// Copyright (c) XRTK. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace XRTK.WindowsMixedReality.Definitions
{
    /// <summary>
    /// Enumerates possible changes that may happen to a spatial surface observed
    /// by a <see cref="Interfaces.Providers.SpatialObservers.IMixedRealitySpatialMeshObserver"/>.
    /// </summary>
    public enum SpatialSurfaceChange
    {
        /// <summary>
        /// The spatial surface was removed. This may happen with moving objects or
        /// in general when an environment changes in betwen spatial mapping updates.
        /// It may also happen when the spatial mapping was refined.
        /// </summary>
        Removed = 0,
        /// <summary>
        /// The spatial surface was initially recognized and added to the spatial mesh.
        /// </summary>
        Added,
        /// <summary>
        /// The spatial surface was already being observed but got updated.
        /// </summary>
        Updated
    }
}