// Copyright (c) XRTK. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Collections.Generic;
using UnityEngine;
using XRTK.Attributes;
using XRTK.Definitions;
using XRTK.Definitions.Platforms;
using XRTK.Interfaces.BoundarySystem;
using XRTK.Services;
using XRTK.Definitions.BoundarySystem;

#if WINDOWS_UWP
using System.Linq;
using Windows.Perception.Spatial;
using XRTK.Extensions;
#endif

namespace XRTK.WindowsMixedReality.Providers.BoundarySystem
{
    [RuntimePlatform(typeof(UniversalWindowsPlatform))]
    [System.Runtime.InteropServices.Guid("e61b047a-56ac-421a-b5f7-683fd44dd33c")]
    public class WindowsMixedRealityBoundaryDataProvider : BaseDataProvider, IMixedRealityBoundaryDataProvider
    {
        /// <inheritdoc />
        public WindowsMixedRealityBoundaryDataProvider(string name, uint priority, BaseMixedRealityProfile profile, IMixedRealityBoundarySystem parentService)
            : base(name, priority, profile, parentService)
        {
            boundarySystem = parentService;
        }

        private readonly IMixedRealityBoundarySystem boundarySystem;

        /// <inheritdoc />
        public override void Enable()
        {
            base.Enable();

            boundarySystem.SetupBoundary(this);
        }

        #region IMixedRealityBoundaryDataProvider Implementation

        /// <inheritdoc />
        public BoundaryVisibility Visibility => BoundaryVisibility.Unknown;

        /// <inheritdoc />
        public bool IsPlatformConfigured
        {
            get
            {
#if WINDOWS_UWP
                var currentStage = SpatialStageFrameOfReference.Current;
                return currentStage != null && currentStage.MovementRange == SpatialMovementRange.Bounded;
#else
                return false;
#endif
            }
        }

        /// <inheritdoc />
        public bool TryGetBoundaryGeometry(ref List<Vector3> geometry)
        {
#if WINDOWS_UWP
            geometry.Clear();

            var currentStage = SpatialStageFrameOfReference.Current;
            var platformGeometry = currentStage?.TryGetMovementBounds(currentStage.CoordinateSystem);

            if (platformGeometry == null)
            {
                return false;
            }

            geometry.AddRange(platformGeometry.Select(point => point.ToUnity()));
            return true;
#else
            return false;
#endif
        }

        #endregion  IMixedRealityBoundaryDataProvider Implementation
    }
}
