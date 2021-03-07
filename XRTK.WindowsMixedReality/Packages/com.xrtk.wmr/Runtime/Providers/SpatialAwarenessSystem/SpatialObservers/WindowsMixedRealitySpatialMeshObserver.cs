// Copyright (c) XRTK. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XRTK.Attributes;
using XRTK.Definitions.Platforms;
using XRTK.Interfaces.SpatialAwarenessSystem;
using XRTK.Providers.SpatialObservers;
using XRTK.WindowsMixedReality.Profiles;

#if WINDOWS_UWP

using System;
using System.Collections.Generic;
using UnityEngine;
using Windows.Perception.Spatial;
using XRTK.WindowsMixedReality.Utilities;
using Windows.Perception.Spatial.Surfaces;
using XRTK.Extensions;
using XRTK.Services;
using XRTK.Utilities;
using XRTK.Interfaces.CameraSystem;
using XRTK.Definitions.SpatialAwarenessSystem;
using XRTK.WindowsMixedReality.Definitions;

#endif // WINDOWS_UWP

namespace XRTK.WindowsMixedReality.Providers.SpatialAwarenessSystem.SpatialObservers
{
    /// <summary>
    /// The <see cref="WindowsMixedRealitySpatialMeshObserver"/> is responsible for providing surface data in the user's surroundings
    /// when running on a <see cref="UniversalWindowsPlatform"/> device with spatial mapping capabilities.
    /// </summary>
    [RuntimePlatform(typeof(UniversalWindowsPlatform))]
    [System.Runtime.InteropServices.Guid("0861C801-E20E-4E76-8C4E-711C1CB43DDF")]
    public class WindowsMixedRealitySpatialMeshObserver : BaseMixedRealitySpatialMeshObserver
    {
        /// <inheritdoc />
        public WindowsMixedRealitySpatialMeshObserver(string name, uint priority, WindowsMixedRealitySpatialMeshObserverProfile profile, IMixedRealitySpatialAwarenessSystem parentService)
            : base(name, priority, profile, parentService) { }

#if WINDOWS_UWP

        private SpatialSurfaceObserver spatialSurfaceObserver;
        private SpatialSurfaceMeshOptions spatialSurfaceMeshOptions;
        private Vector3 currentObserverOrigin = Vector3.zero;
        private Vector3 currentObserverExtents = Vector3.zero;
        private SpatialPerceptionAccessStatus currentAccessStatus = SpatialPerceptionAccessStatus.Unspecified;
        private readonly Dictionary<Guid, SpatialSurfaceMesh> observedMeshesDict = new Dictionary<Guid, SpatialSurfaceMesh>();
        private float lastUpdatedObservedSurfacesTimeStamp = 0;

        /// <summary>
        /// Only update the observer if it is running and if enough time has passed since the previous observer update.
        /// </summary>
        private bool ShouldUpdate => !Application.isPlaying || !IsRunning || !(Time.time - lastUpdatedObservedSurfacesTimeStamp >= UpdateInterval);

        /// <inheritdoc />
        public async override void Enable()
        {
            if (SpatialSurfaceObserver.IsSupported())
            {
                if (spatialSurfaceObserver == null)
                {
                    spatialSurfaceObserver = new SpatialSurfaceObserver();
                    spatialSurfaceMeshOptions = new SpatialSurfaceMeshOptions();
                    var supportedVertexPositionFormats = SpatialSurfaceMeshOptions.SupportedVertexPositionFormats;
                    var supportedVertexNormalFormats = SpatialSurfaceMeshOptions.SupportedVertexNormalFormats;

                    for (int i = 0; i < supportedVertexPositionFormats.Count; i++)
                    {
                        if (supportedVertexPositionFormats[i] == Windows.Graphics.DirectX.DirectXPixelFormat.R16G16B16A16IntNormalized)
                        {
                            spatialSurfaceMeshOptions.VertexPositionFormat = Windows.Graphics.DirectX.DirectXPixelFormat.R16G16B16A16IntNormalized;
                            break;
                        }
                    }

                    for (int i = 0; i < supportedVertexNormalFormats.Count; i++)
                    {
                        if (supportedVertexNormalFormats[i] == Windows.Graphics.DirectX.DirectXPixelFormat.R8G8B8A8IntNormalized)
                        {
                            spatialSurfaceMeshOptions.VertexNormalFormat = Windows.Graphics.DirectX.DirectXPixelFormat.R8G8B8A8IntNormalized;
                            break;
                        }
                    }

                    // If a very high detail setting with spatial mapping is used, it can be beneficial
                    // to use a 32-bit unsigned integer format for indices instead of the default 16-bit. 
                    if (MeshLevelOfDetail == SpatialAwarenessMeshLevelOfDetail.High)
                    {
                        var supportedTriangleIndexFormats = SpatialSurfaceMeshOptions.SupportedTriangleIndexFormats;
                        for (int i = 0; i < supportedTriangleIndexFormats.Count; i++)
                        {
                            if (supportedTriangleIndexFormats[i] == Windows.Graphics.DirectX.DirectXPixelFormat.R8G8B8A8IntNormalized)
                            {
                                spatialSurfaceMeshOptions.TriangleIndexFormat = Windows.Graphics.DirectX.DirectXPixelFormat.R32UInt;
                            }
                        }
                    }
                }

                if (currentAccessStatus == SpatialPerceptionAccessStatus.Unspecified)
                {
                    currentAccessStatus = await SpatialSurfaceObserver.RequestAccessAsync();
                }

                if (currentAccessStatus == SpatialPerceptionAccessStatus.Allowed)
                {
                    UpdateObservedSurfaces(spatialSurfaceObserver);
                    spatialSurfaceObserver.ObservedSurfacesChanged += SpatialSurfaceObserver_ObservedSurfacesChanged;
                }
            }

            base.Enable();
        }

        /// <inheritdoc />
        public override void Disable()
        {
            if (spatialSurfaceObserver != null)
            {
                spatialSurfaceObserver.ObservedSurfacesChanged -= SpatialSurfaceObserver_ObservedSurfacesChanged;
            }

            observedMeshesDict.Clear();
            currentAccessStatus = SpatialPerceptionAccessStatus.Unspecified;

            base.Disable();
        }

        /// <inheritdoc />
        public override void Update()
        {
            base.Update();

            if (!ShouldUpdate)
            {
                return;
            }

            // Update the observer location if it is not stationary.
            if (!IsStationaryObserver)
            {
                ObserverOrigin = MixedRealityToolkit.TryGetSystem<IMixedRealityCameraSystem>(out var cameraSystem)
                    ? cameraSystem.MainCameraRig.CameraTransform.localPosition
                    : CameraCache.Main.transform.position;
            }

            // The application can update the observer volume at any time, make sure we are using the latest.
            ConfigureObserverVolume(ObserverOrigin, ObservationExtents);
        }

        /// <inheritdoc/>
        public override void StartObserving()
        {
            if (IsRunning || spatialSurfaceObserver == null)
            {
                return;
            }

            // We want the first update immediately.
            lastUpdatedObservedSurfacesTimeStamp = 0;

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

            spatialSurfaceObserver.SetBoundingVolume(SpatialBoundingVolume.FromBox(WindowsMixedRealityUtilities.SpatialCoordinateSystem, new SpatialBoundingBox()
            {
                Center = newOrigin.ToVector3(),
                Extents = newExtents.ToVector3()
            }));

            currentObserverExtents = newExtents;
            currentObserverOrigin = newOrigin;
        }

        private void SpatialSurfaceObserver_ObservedSurfacesChanged(SpatialSurfaceObserver sender, object args) => UpdateObservedSurfaces(sender);

        private async void UpdateObservedSurfaces(SpatialSurfaceObserver spatialSurfaceObserver)
        {
            if (!ShouldUpdate)
            {
                return;
            }

            var observedSurfaces = spatialSurfaceObserver.GetObservedSurfaces();
            foreach (var spatialSurfaceInfo in observedSurfaces.Values)
            {
                var surfaceBounds = spatialSurfaceInfo.TryGetBounds(WindowsMixedRealityUtilities.SpatialCoordinateSystem);
                if (surfaceBounds.HasValue)
                {
                    var surfaceMesh = await spatialSurfaceInfo.TryComputeLatestMeshAsync(WindowsMixedRealityUtilities.GetMaxTrianglesPerCubicMeter(MeshLevelOfDetail), spatialSurfaceMeshOptions);

                }
            }

            lastUpdatedObservedSurfacesTimeStamp = Time.time;
        }

        private async void ProcessSurfaceChange(Guid surfaceId, SpatialSurfaceChange changeType, SpatialSurfaceMesh spatialSurfaceMesh)
        {
            if (changeType == SpatialSurfaceChange.Removed &&
                SpatialMeshObjects.TryGetValue(surfaceId, out var meshObject))
            {
                RaiseMeshRemoved(meshObject);
                return;
            }

            var spatialMeshObject = await RequestSpatialMeshObject(surfaceId);
            spatialMeshObject.GameObject.name = $"SpatialMesh_{surfaceId}";

            var surfaceData = new SurfaceData(surfaceId, spatialMeshObject.Filter, worldAnchor, spatialMeshObject.Collider, 1000 * (int)MeshLevelOfDetail, true);

            if (!SpatialMeshObjects.TryGetValue(cookedData.id.handle, out var meshObject))
            {
                // Likely it was removed before data could be cooked.
                return;
            }

            var mesh = spatialMeshObject.Mesh;
            using var vertexPositions = spatialSurfaceMesh.VertexPositions;

            // Apply the appropriate material to the mesh.
            var displayOption = MeshDisplayOption;
            switch (displayOption)
            {
                case SpatialMeshDisplayOptions.None:
                    meshObject.Collider.enabled = false;
                    meshObject.Renderer.enabled = false;
                    break;
                case SpatialMeshDisplayOptions.Visible:
                    meshObject.Collider.enabled = true;
                    meshObject.Renderer.enabled = true;
                    meshObject.Renderer.sharedMaterial = MeshVisibleMaterial;
                    break;
                case SpatialMeshDisplayOptions.Occlusion:
                    meshObject.Collider.enabled = true;
                    meshObject.Renderer.enabled = true;
                    meshObject.Renderer.sharedMaterial = MeshOcclusionMaterial;
                    break;
                case SpatialMeshDisplayOptions.Collision:
                    meshObject.Collider.enabled = true;
                    meshObject.Renderer.enabled = false;
                    break;
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
                case SpatialSurfaceChange.Added:
                    RaiseMeshAdded(meshObject);
                    break;
                case SpatialSurfaceChange.Updated:
                    RaiseMeshUpdated(meshObject);
                    break;
                default:
                    throw new ArgumentOutOfRangeException($"{nameof(SpatialSurfaceChange)}.{changeType} is not handled by {nameof(WindowsMixedRealitySpatialMeshObserver)}.{nameof(ProcessSurfaceChange)}.");
            }
        }

#endif // WINDOWS_UWP
    }
}