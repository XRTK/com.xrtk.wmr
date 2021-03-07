// Copyright (c) XRTK. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

#if WINDOWS_UWP

using System;
using Windows.Perception.Spatial.Surfaces;

namespace XRTK.WindowsMixedReality.Definitions
{
    /// <summary>
    /// <see cref="SpatialSurfaceData"/> is a wrapper around the native Windows Universal
    /// <see cref="SpatialSurfaceMesh"/> class. It's main purpose is to convert  native surface
    /// mesh data to a format the Unity Engine can work with.
    /// 
    /// The <see cref="Providers.SpatialAwarenessSystem.SpatialObservers.WindowsMixedRealitySpatialMeshObserver"/>
    /// is using it to manage observed surfaces.
    /// </summary>
    internal sealed class SpatialSurfaceData
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="id"></param>
        /// <param name="spatialSurfaceMesh"></param>
        public SpatialSurfaceData(Guid id, SpatialSurfaceMesh spatialSurfaceMesh)
        {

        }
    }
}

#endif // WINDOWS_UWP