// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

#if WINDOWS_UWP
using System.Runtime.InteropServices;
using UnityEngine.XR.WSA;
using Windows.Perception.Spatial;
#endif // WINDOWS_UWP

namespace XRTK.WindowsMixedReality.Utilities
{
    public static class WindowsMixedRealityUtilities
    {
#if WINDOWS_UWP
        private static SpatialCoordinateSystem spatialCoordinateSystem = null;

        public static SpatialCoordinateSystem SpatialCoordinateSystem => spatialCoordinateSystem ?? (spatialCoordinateSystem = Marshal.GetObjectForIUnknown(WorldManager.GetNativeISpatialCoordinateSystemPtr()) as SpatialCoordinateSystem);
#endif // WINDOWS_UWP
    }
}