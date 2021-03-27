// Copyright (c) XRTK. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

#if WINDOWS_UWP

using System;
using Windows.Perception.Spatial;
using Windows.UI.Input.Spatial;
using XRTK.Definitions.SpatialAwarenessSystem;

namespace XRTK.WindowsMixedReality.Utilities
{
    public static class WindowsMixedRealityUtilities
    {
        private static SpatialCoordinateSystem spatialCoordinateSystem = null;

        /// <summary>
        /// Gets a cached reference to the native <see cref="Windows.Perception.Spatial.SpatialCoordinateSystem"/>.
        /// </summary>
        public static SpatialCoordinateSystem SpatialCoordinateSystem
        {
            get
            {
                if (spatialCoordinateSystem != null)
                {
                    return spatialCoordinateSystem;
                }

                var spatialLocator = SpatialLocator.GetDefault();
                var stationaryFrameOfReference = spatialLocator.CreateStationaryFrameOfReferenceAtCurrentLocation();
                spatialCoordinateSystem = stationaryFrameOfReference.CoordinateSystem;

                return spatialCoordinateSystem;
            }
        }

        private static SpatialInteractionManager spatialInteractionManager = null;

        /// <summary>
        /// Gets the native <see cref="Windows.UI.Input.Spatial.SpatialInteractionManager"/> instance for the current application
        /// state.
        /// </summary>
        public static SpatialInteractionManager SpatialInteractionManager
        {
            get
            {
                if (spatialInteractionManager == null)
                {
                    UnityEngine.WSA.Application.InvokeOnUIThread(() =>
                    {
                        spatialInteractionManager = SpatialInteractionManager.GetForCurrentView();
                    }, true);
                }

                return spatialInteractionManager;
            }
        }

        public static double GetMaxTrianglesPerCubicMeter(SpatialAwarenessMeshLevelOfDetail spatialAwarenessMeshLevelOfDetail) => spatialAwarenessMeshLevelOfDetail switch
        {
            SpatialAwarenessMeshLevelOfDetail.Low => 1000,
            SpatialAwarenessMeshLevelOfDetail.Medium => 2000,
            SpatialAwarenessMeshLevelOfDetail.High => 3000,
            _ => throw new ArgumentOutOfRangeException($"{nameof(SpatialAwarenessMeshLevelOfDetail)}.{spatialAwarenessMeshLevelOfDetail} could not be mapped to maximum triangles per cubic meter.")
        };
    }
}

#endif // WINDOWS_UWP