// Copyright (c) XRTK. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

#if WINDOWS_UWP

using Windows.Perception.Spatial;

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
    }
}

#endif // WINDOWS_UWP