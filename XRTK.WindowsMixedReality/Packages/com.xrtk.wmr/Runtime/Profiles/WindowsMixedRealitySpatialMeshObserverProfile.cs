// Copyright (c) XRTK. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using UnityEngine;
using XRTK.Definitions.Platforms;
using XRTK.Definitions.SpatialAwarenessSystem;
using XRTK.Providers.SpatialObservers;
using XRTK.WindowsMixedReality.Providers.SpatialAwarenessSystem.SpatialObservers;

namespace XRTK.WindowsMixedReality.Profiles
{
    /// <summary>
    /// Configuration profile for the <see cref="WindowsMixedRealitySpatialMeshObserver"/>.
    /// This profile offers settings for adjusting spatial awareness behaviour on the <see cref="UniversalWindowsPlatform"/>.
    /// </summary>
    public class WindowsMixedRealitySpatialMeshObserverProfile : BaseMixedRealitySpatialMeshObserverProfile
    {
        [SerializeField]
        [Tooltip("The triangles per cubic meter to use when the mesh level of detail is set to custom.")]
        private double trianglesPerCubicMeter;

        /// <summary>
        /// The triangles per cubic meter to use when <see cref="BaseMixedRealitySpatialMeshObserverProfile.MeshLevelOfDetail"/>
        /// is set to <see cref="SpatialAwarenessMeshLevelOfDetail.Custom"/>
        /// </summary>
        public double TrianglesPerCubicMeter => trianglesPerCubicMeter;
    }
}
