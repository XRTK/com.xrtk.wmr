// Copyright (c) XRTK. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XRTK.Attributes;
using XRTK.Definitions.Platforms;
using XRTK.Interfaces.SpatialAwarenessSystem;
using XRTK.Providers.SpatialObservers;
using XRTK.WindowsMixedReality.Profiles;

#if WINDOWS_UWP

using System;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Rendering;
using Windows.Perception.Spatial;
using Windows.Perception.Spatial.Surfaces;
using Windows.Storage.Streams;
using XRTK.Definitions.SpatialAwarenessSystem;
using XRTK.Extensions;
using XRTK.Interfaces.CameraSystem;
using XRTK.Services;
using XRTK.Utilities;
using XRTK.Utilities.Async;
using XRTK.WindowsMixedReality.Utilities;

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
            : base(name, priority, profile, parentService)
        {
#if WINDOWS_UWP
            if (profile.MeshLevelOfDetail == SpatialAwarenessMeshLevelOfDetail.Custom)
            {
                trianglesPerCubicMeter = profile.TrianglesPerCubicMeter;
            }

            if (SpatialSurfaceObserver.IsSupported())
            {
                spatialSurfaceObserver = new SpatialSurfaceObserver();
                spatialSurfaceMeshOptions = new SpatialSurfaceMeshOptions();
                spatialSurfaceMeshOptions.IncludeVertexNormals = true;

                // TODO Determine which formats are the correct ones to use.
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
        }

        private readonly SpatialSurfaceObserver spatialSurfaceObserver;
        private readonly SpatialSurfaceMeshOptions spatialSurfaceMeshOptions;

        private float lastUpdated = 0;
        private Vector3 currentObserverOrigin = Vector3.zero;
        private Vector3 currentObserverExtents = Vector3.zero;
        private SpatialPerceptionAccessStatus currentAccessStatus = SpatialPerceptionAccessStatus.Unspecified;

        private double trianglesPerCubicMeter;

        /// <summary>
        /// The mesh detail to use in triangles per cubic meter.
        /// </summary>
        public double TrianglesPerCubicMeter
        {
            get
            {
                switch (MeshLevelOfDetail)
                {
                    case SpatialAwarenessMeshLevelOfDetail.Low:
                        trianglesPerCubicMeter = 1000d;
                        break;
                    case SpatialAwarenessMeshLevelOfDetail.Medium:
                        trianglesPerCubicMeter = 2000d;
                        break;
                    case SpatialAwarenessMeshLevelOfDetail.High:
                        trianglesPerCubicMeter = 3000d;
                        break;
                }

                return trianglesPerCubicMeter;
            }
            set
            {
                MeshLevelOfDetail = SpatialAwarenessMeshLevelOfDetail.Custom;
                trianglesPerCubicMeter = value;
            }
        }

        /// <inheritdoc />
        public override void Update()
        {
            base.Update();

            // Only update the observer if it is running.
            if (!IsRunning ||
                !Application.isPlaying ||
                spatialSurfaceObserver != null)
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

            // and If enough time has passed since the previous observer update
            if (Time.time - lastUpdated >= UpdateInterval)
            {
                UpdateObservedSurfaces();
                lastUpdated = Time.time;
            }
        }

        /// <inheritdoc/>
        public override async void StartObserving()
        {
            if (IsRunning)
            {
                return;
            }

            if (spatialSurfaceObserver == null)
            {
                Debug.LogError($"Failed to start {Name}! {nameof(spatialSurfaceObserver)} is null!");
                return;
            }

            if (currentAccessStatus != SpatialPerceptionAccessStatus.Allowed)
            {
                currentAccessStatus = await SpatialSurfaceObserver.RequestAccessAsync();
            }

            if (currentAccessStatus != SpatialPerceptionAccessStatus.Allowed)
            {
                Debug.LogWarning($"Failed to start {Name}! Access is {currentAccessStatus}.");
                return;
            }

            base.StartObserving();

            // We want the first update immediately.
            lastUpdated = 0;
        }

        public override void StopObserving()
        {
            if (!IsRunning) { return; }

            currentAccessStatus = SpatialPerceptionAccessStatus.Unspecified;

            base.StopObserving();
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

            spatialSurfaceObserver.SetBoundingVolume(
                SpatialBoundingVolume.FromBox(
                    WindowsMixedRealityUtilities.SpatialCoordinateSystem, new SpatialBoundingBox
                    {
                        Center = newOrigin.ToNumericsVector3(),
                        Extents = newExtents.ToNumericsVector3()
                    }
                )
            );

            currentObserverExtents = newExtents;
            currentObserverOrigin = newOrigin;
        }

        private void UpdateObservedSurfaces()
        {
            var surfaces = spatialSurfaceObserver.GetObservedSurfaces();

            foreach (var spatialMeshObject in SpatialMeshObjects)
            {
                if (!surfaces.TryGetValue(spatialMeshObject.Key, out _))
                {
                    RaiseMeshRemoved(spatialMeshObject.Value);
                }
            }

            foreach (var surface in surfaces)
            {
                var surfaceBounds = surface.Value.TryGetBounds(WindowsMixedRealityUtilities.SpatialCoordinateSystem);
                var surfaceChangeStatus = SpatialMeshObjects.TryGetValue(surface.Key, out var spatialMeshObject)
                    ? SpatialObserverStatus.Updated
                    : SpatialObserverStatus.Added;

                if (surfaceBounds.HasValue)
                {
                    MeshInfo_Update(surface.Value, surfaceChangeStatus);
                }
                else
                {
                    if (surfaceChangeStatus == SpatialObserverStatus.Updated)
                    {
                        // Only remove if we've already created a mesh for it.
                        RaiseMeshRemoved(spatialMeshObject);
                    }
                }
            }
        }

        private async void MeshInfo_Update(SpatialSurfaceInfo meshInfo, SpatialObserverStatus statusType)
        {
            if (statusType == SpatialObserverStatus.Removed &&
                SpatialMeshObjects.TryGetValue(meshInfo.Id, out var removedMeshObject))
            {
                RaiseMeshRemoved(removedMeshObject);
                return;
            }

            var spatialMeshObject = await RequestSpatialMeshObject(meshInfo.Id);
            spatialMeshObject.GameObject.name = $"SpatialMesh_{meshInfo.Id}";

            try
            {
                await GenerateMeshAsync(meshInfo, spatialMeshObject);
            }
            catch (Exception e)
            {
                Debug.LogError(e);
                RaiseMeshRemoved(spatialMeshObject);
                return;
            }

            if (!SpatialMeshObjects.TryGetValue(meshInfo.Id, out var meshObject))
            {
                Debug.LogWarning($"Failed to find a spatial mesh object for {meshInfo.Id}!");
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
                meshObject.Renderer.sharedMaterial = displayOption == SpatialMeshDisplayOptions.Visible
                    ? MeshVisibleMaterial
                    : MeshOcclusionMaterial;
            }
            else
            {
                meshObject.Renderer.enabled = false;
                meshObject.Collider.enabled = false;
            }

            if (!meshObject.GameObject.activeInHierarchy)
            {
                meshObject.GameObject.SetActive(true);
            }

            switch (statusType)
            {
                case SpatialObserverStatus.Added:
                    RaiseMeshAdded(meshObject);
                    break;
                case SpatialObserverStatus.Updated:
                    RaiseMeshUpdated(meshObject);
                    break;
                default:
                    throw new ArgumentOutOfRangeException($"{nameof(SpatialObserverStatus)}.{statusType} was not handled!");
            }
        }

        private async Task GenerateMeshAsync(SpatialSurfaceInfo meshInfo, SpatialMeshObject spatialMeshObject)
        {
            // TODO Check if the spatialSurfaceMeshOptions are correct for what we need.
            var spatialSurfaceMesh = await meshInfo.TryComputeLatestMeshAsync(TrianglesPerCubicMeter, spatialSurfaceMeshOptions);

            await Awaiters.UnityMainThread;

            var mesh = spatialMeshObject.Mesh == null ? new Mesh() : spatialMeshObject.Mesh;

            mesh.name = $"Mesh_{meshInfo.Id}";

            await Awaiters.BackgroundThread;

            if (MeshRecalculateNormals)
            {
                var normalCount = (int)spatialSurfaceMesh.VertexNormals.ElementCount;
                var normals = new NativeArray<VertexData>(normalCount, Allocator.None);
                var vertexBuffer = DataReader.FromBuffer(spatialSurfaceMesh.VertexPositions.Data);
                var normalBuffer = DataReader.FromBuffer(spatialSurfaceMesh.VertexNormals.Data);

                for (int i = 0; i < normalCount; i++)
                {
                    normals[i] = new VertexData
                    {
                        // TODO Check if spatialSurfaceMesh.VertexPositionScale needs to be accounted for.
                        Position = new Vector3(vertexBuffer.ReadSingle(), vertexBuffer.ReadSingle(), -vertexBuffer.ReadSingle()),
                        Normal = new Vector3(normalBuffer.ReadSingle(), normalBuffer.ReadSingle(), -normalBuffer.ReadSingle())
                    };
                }

                mesh.SetVertexBufferParams(normalCount, NormalsLayout);
                mesh.SetVertexBufferData(normals, 0, 0, normalCount);

                vertexBuffer.Dispose();
                normalBuffer.Dispose();
                normals.Dispose();
            }
            else
            {
                var vertexCount = (int)spatialSurfaceMesh.VertexPositions.ElementCount;
                var vertices = new NativeArray<Vector3>(vertexCount, Allocator.None);
                var vertexBuffer = DataReader.FromBuffer(spatialSurfaceMesh.VertexPositions.Data);

                for (int i = 0; i < vertexCount; i++)
                {
                    // TODO Check if spatialSurfaceMesh.VertexPositionScale needs to be accounted for.
                    vertices[i] = new Vector3(vertexBuffer.ReadSingle(), vertexBuffer.ReadSingle(), -vertexBuffer.ReadSingle());
                }

                mesh.SetVertexBufferParams(vertexCount, VertexLayout);
                mesh.SetVertexBufferData(vertices, 0, 0, vertexCount);

                vertexBuffer.Dispose();
                vertices.Dispose();
            }

            var indicesCount = (int)spatialSurfaceMesh.TriangleIndices.ElementCount;
            var indices = new NativeArray<short>(indicesCount, Allocator.None);
            var indicesBuffer = DataReader.FromBuffer(spatialSurfaceMesh.TriangleIndices.Data);

            for (int i = 0; i < indicesCount; i++)
            {
                indices[i] = indicesBuffer.ReadInt16();
            }

            mesh.SetIndexBufferParams(indicesCount, IndexFormat.UInt16);
            mesh.SetIndexBufferData(indices, 0, 0, indicesCount);

            indicesBuffer.Dispose();
            indices.Dispose();

            mesh.SetSubMesh(0, new SubMeshDescriptor(0, indicesCount));
            mesh.Optimize();
            mesh.RecalculateBounds();

            if (MeshRecalculateNormals)
            {
                mesh.RecalculateNormals();
            }

            spatialMeshObject.Mesh = mesh;

            await Awaiters.UnityMainThread;
        }

        private static readonly VertexAttributeDescriptor[] VertexLayout =
        {
            new VertexAttributeDescriptor(VertexAttribute.Position)
        };

        private static readonly VertexAttributeDescriptor[] NormalsLayout =
        {
            new VertexAttributeDescriptor(VertexAttribute.Position),
            new VertexAttributeDescriptor(VertexAttribute.Normal)
        };

        /// <summary>
        /// Helper struct used as layout when normals are requested.
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        private struct VertexData
        {
            /// <summary>
            /// Position data of vertex.
            /// </summary>
            public Vector3 Position;

            /// <summary>
            /// Normal data of vertex.
            /// </summary>
            public Vector3 Normal;
        }
#else
        }
#endif // WINDOWS_UWP
    }
}
