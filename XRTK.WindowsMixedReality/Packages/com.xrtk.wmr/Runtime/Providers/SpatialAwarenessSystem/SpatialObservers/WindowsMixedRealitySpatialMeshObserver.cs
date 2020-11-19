// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XRTK.Attributes;
using XRTK.Definitions.Platforms;
using XRTK.Interfaces.SpatialAwarenessSystem;
using XRTK.Providers.SpatialObservers;
using XRTK.WindowsMixedReality.Profiles;

#if UNITY_WSA
using System;
using UnityEngine;
using UnityEngine.XR.WSA;
using XRTK.Definitions.SpatialAwarenessSystem;
using XRTK.Extensions;
using XRTK.Services;
using XRTK.Utilities;
#endif // UNITY_WSA

namespace XRTK.WindowsMixedReality.Providers.SpatialAwarenessSystem.SpatialObservers
{
    /// <summary>
    /// The Windows Mixed Reality Spatial Mesh Observer.
    /// </summary>
    [Obsolete]
    [RuntimePlatform(typeof(UniversalWindowsPlatform))]
    [System.Runtime.InteropServices.Guid("0861C801-E20E-4E76-8C4E-711C1CB43DDF")]
    public class WindowsMixedRealitySpatialMeshObserver : BaseMixedRealitySpatialMeshObserver
    {
        /// <inheritdoc />
        public WindowsMixedRealitySpatialMeshObserver(string name, uint priority, WindowsMixedRealitySpatialMeshObserverProfile profile, IMixedRealitySpatialAwarenessSystem parentService)
            : base(name, priority, profile, parentService)
        {
#if UNITY_WSA
#if UNITY_EDITOR 
            if (!UnityEditor.PlayerSettings.WSA.GetCapability(UnityEditor.PlayerSettings.WSACapability.SpatialPerception))
            {
                UnityEditor.PlayerSettings.WSA.SetCapability(UnityEditor.PlayerSettings.WSACapability.SpatialPerception, true);
            }

#endif // UNITY_EDITOR
            if (observer == null)
            {
                observer = new SurfaceObserver();
            }
#endif // UNITY_WSA
        }

#if UNITY_WSA

        #region IMixedRealityService implementation

        /// <inheritdoc />
        public override void Update()
        {
            base.Update();

            // Only update the observer if it is running.
            if (!Application.isPlaying || !IsRunning) { return; }

            // and If enough time has passed since the previous observer update
            if (!(Time.time - lastUpdated >= UpdateInterval)) { return; }

            // Update the observer location if it is not stationary
            if (!IsStationaryObserver)
            {
                ObserverOrigin = MixedRealityToolkit.CameraSystem != null
                    ? MixedRealityToolkit.CameraSystem.MainCameraRig.CameraTransform.localPosition
                    : CameraCache.Main.transform.position;
            }

            // The application can update the observer volume at any time, make sure we are using the latest.
            ConfigureObserverVolume(ObserverOrigin, ObservationExtents);

            observer.Update(SurfaceObserver_OnSurfaceChanged);
            lastUpdated = Time.time;
        }

        /// <inheritdoc />
        protected override void OnDispose(bool finalizing)
        {
            observer.Dispose();

            base.OnDispose(finalizing);
        }

        #endregion IMixedRealityService implementation

        #region IMixedRealitySpatialMeshObserver implementation

        /// <summary>
        /// The surface observer providing the spatial data.
        /// </summary>
        private static SurfaceObserver observer = null;

        /// <summary>
        /// The current location of the surface observer.
        /// </summary>
        private Vector3 currentObserverOrigin = Vector3.zero;

        /// <summary> 
        /// The observation extents that are currently in use by the surface observer. 
        /// </summary> 
        private Vector3 currentObserverExtents = Vector3.zero;

        /// <summary>
        /// The time at which the surface observer was last asked for updated data.
        /// </summary>
        private float lastUpdated = 0;

        /// <inheritdoc/>
        public override void StartObserving()
        {
            if (IsRunning)
            {
                return;
            }

            // We want the first update immediately.
            lastUpdated = 0;

            base.StartObserving();
        }

        /// <summary>
        /// Applies the configured observation extents.
        /// </summary>
        private void ConfigureObserverVolume(Vector3 newOrigin, Vector3 newExtents)
        {
            if (currentObserverExtents.Equals(newExtents) &&
                currentObserverOrigin.Equals(newOrigin))
            {
                return;
            }

            observer.SetVolumeAsAxisAlignedBox(newOrigin, newExtents);

            currentObserverExtents = newExtents;
            currentObserverOrigin = newOrigin;
        }

        /// <summary>
        /// Handles the SurfaceObserver's OnSurfaceChanged event.
        /// </summary>
        /// <param name="surfaceId">The identifier assigned to the surface which has changed.</param>
        /// <param name="changeType">The type of change that occurred on the surface.</param>
        /// <param name="bounds">The bounds of the surface.</param>
        /// <param name="updateTime">The date and time at which the change occurred.</param>
        private async void SurfaceObserver_OnSurfaceChanged(SurfaceId surfaceId, SurfaceChange changeType, Bounds bounds, DateTime updateTime)
        {
            // If we're adding or updating a mesh
            if (changeType != SurfaceChange.Removed)
            {
                var spatialMeshObject = await RequestSpatialMeshObject(surfaceId.handle);
                spatialMeshObject.GameObject.name = $"SpatialMesh_{surfaceId.handle.ToString()}";
                var worldAnchor = spatialMeshObject.GameObject.EnsureComponent<WorldAnchor>();
                var surfaceData = new SurfaceData(surfaceId, spatialMeshObject.Filter, worldAnchor, spatialMeshObject.Collider, MeshTrianglesPerCubicMeter, true);

                if (!observer.RequestMeshAsync(surfaceData, OnDataReady))
                {
                    Debug.LogError($"Mesh request failed for spatial observer with Id {surfaceId.handle.ToString()}");
                    RaiseMeshRemoved(spatialMeshObject);
                }

                void OnDataReady(SurfaceData cookedData, bool outputWritten, float elapsedCookTimeSeconds)
                {
                    if (!outputWritten)
                    {
                        Debug.LogWarning($"No output for {cookedData.id.handle.ToString()}");
                        return;
                    }

                    if (!SpatialMeshObjects.TryGetValue(cookedData.id.handle, out var meshObject))
                    {
                        // Likely it was removed before data could be cooked.
                        return;
                    }

                    // Apply the appropriate material to the mesh.
                    var displayOption = MeshDisplayOption;

                    if (displayOption != SpatialMeshDisplayOptions.None)
                    {
                        meshObject.Collider.enabled = true;
                        meshObject.Renderer.enabled = displayOption == SpatialMeshDisplayOptions.Visible ||
                                                      displayOption == SpatialMeshDisplayOptions.Occlusion;
                        meshObject.Renderer.sharedMaterial = (displayOption == SpatialMeshDisplayOptions.Visible)
                            ? MeshVisibleMaterial
                            : MeshOcclusionMaterial;
                    }
                    else
                    {
                        meshObject.Collider.enabled = false;
                        meshObject.Renderer.enabled = false;
                    }

                    // Recalculate the mesh normals if requested.
                    if (MeshRecalculateNormals)
                    {
                        if (meshObject.Filter.sharedMesh != null)
                        {
                            meshObject.Filter.sharedMesh.RecalculateNormals();
                        }
                        else
                        {
                            meshObject.Filter.mesh.RecalculateNormals();
                        }
                    }

                    meshObject.GameObject.SetActive(true);

                    switch (changeType)
                    {
                        case SurfaceChange.Added:
                            RaiseMeshAdded(meshObject);
                            break;
                        case SurfaceChange.Updated:
                            RaiseMeshUpdated(meshObject);
                            break;
                    }
                }
            }
            else if (SpatialMeshObjects.TryGetValue(surfaceId.handle, out var meshObject))
            {
                RaiseMeshRemoved(meshObject);
            }
        }

        #endregion IMixedRealitySpatialMeshObserver implementation

#endif // UNITY_WSA
    }
}