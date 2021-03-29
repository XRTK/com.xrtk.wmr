// Copyright (c) XRTK. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

#if WINDOWS_UWP

using System;
using Windows.Perception.Spatial.Surfaces;
using Windows.Storage.Streams;
using XRTK.Definitions.SpatialAwarenessSystem;

namespace XRTK.WindowsMixedReality.Definitions
{
    /// <summary>
    /// <see cref="SpatialSurfaceData"/> is a wrapper around the native Windows Universal
    /// <see cref="SpatialSurfaceMesh"/> class. It's main purpose is to convert  native surface
    /// mesh data to a format the Unity Engine can work with.
    /// 
    /// The <see cref="Providers.SpatialAwarenessSystem.SpatialObservers.WindowsMixedRealitySpatialMeshObserver"/>
    /// is using <see cref="SpatialSurfaceData"/> to manage observed surfaces.
    /// </summary>
    internal sealed class SpatialSurfaceData
    {
        /// <summary>
        /// Constructs a new <see cref="SpatialSurfaceData"/> instance.
        /// </summary>
        /// <param name="id">Unique surface identifier.</param>
        /// <param name="spatialSurfaceMesh">The surface mesh data provided by the platform.</param>
        /// <param name="spatialMeshObject">The XRTK mesh object representing this surface.</param>
        public SpatialSurfaceData(Guid id, SpatialSurfaceMesh spatialSurfaceMesh, SpatialMeshObject spatialMeshObject)
        {
            using var reader = DataReader.FromBuffer(spatialSurfaceMesh.VertexPositions.Data);

            // TODO: spatialSurfaceMesh.VertexPositions -> spatialMeshObject.Mesh.vertices
            // TODO: spatialSurfaceMesh.VertexNormals => spatialMeshObject.Mesh.normals
            // etc.
        }
    }
}

#endif // WINDOWS_UWP