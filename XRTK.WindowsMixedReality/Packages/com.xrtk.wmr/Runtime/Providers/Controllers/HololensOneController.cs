// Copyright (c) XRTK. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XRTK.Definitions.Devices;
using XRTK.Definitions.Utilities;

namespace XRTK.WindowsMixedReality.Providers.Controllers
{
    /// <summary>
    /// First generation HoloLens controller.
    /// </summary>
    [System.Runtime.InteropServices.Guid("6CE43357-54E7-4471-B1B7-4BF4912984B1")]
    public class HololensOneController : WindowsMixedRealityMotionController
    {
        /// <inheritdoc />
        public override MixedRealityInteractionMapping[] DefaultInteractions => new[]
        {
            new MixedRealityInteractionMapping("Spatial Pointer", AxisType.SixDof, DeviceInputType.SpatialPointer),
            new MixedRealityInteractionMapping("Spatial Grip", AxisType.SixDof, DeviceInputType.SpatialGrip),
            new MixedRealityInteractionMapping("Air Tap (Select)", AxisType.Digital, DeviceInputType.Select),
        };
    }
}