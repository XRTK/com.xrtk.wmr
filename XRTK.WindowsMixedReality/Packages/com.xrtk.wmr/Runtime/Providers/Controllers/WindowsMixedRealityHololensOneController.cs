// Copyright (c) XRTK. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XRTK.Definitions.Controllers;
using XRTK.Definitions.Devices;
using XRTK.Definitions.Utilities;
using XRTK.Interfaces.Providers.Controllers;

namespace XRTK.WindowsMixedReality.Providers.Controllers
{
    /// <summary>
    /// First generation HoloLens controller. The HoloLens controller is not a physical controller,
    /// it's the user's hand with a limited set of gestures recognized. HoloLens first generation does not support
    /// fully articulated hand tracking.
    /// </summary>
    [System.Runtime.InteropServices.Guid("6CE43357-54E7-4471-B1B7-4BF4912984B1")]
    public class WindowsMixedRealityHololensOneController : WindowsMixedRealityMotionController
    {
        /// <inheritdoc />
        public WindowsMixedRealityHololensOneController(IMixedRealityControllerDataProvider controllerDataProvider, TrackingState trackingState, Handedness controllerHandedness, MixedRealityControllerMappingProfile controllerMappingProfile)
            : base(controllerDataProvider, trackingState, controllerHandedness, controllerMappingProfile) { }

        /// <inheritdoc />
        public override MixedRealityInteractionMapping[] DefaultInteractions => new[]
        {
            new MixedRealityInteractionMapping("Spatial Pointer", AxisType.SixDof, DeviceInputType.SpatialPointer),
            new MixedRealityInteractionMapping("Spatial Grip", AxisType.SixDof, DeviceInputType.SpatialGrip),
            new MixedRealityInteractionMapping("Air Tap (Select)", AxisType.Digital, DeviceInputType.Select),
        };
    }
}
