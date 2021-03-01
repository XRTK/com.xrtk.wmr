// Copyright (c) XRTK. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XRTK.Definitions.Controllers;
using XRTK.Definitions.Devices;
using XRTK.Definitions.Utilities;
using XRTK.Interfaces.CameraSystem;
using XRTK.Interfaces.Providers.Controllers;
using XRTK.Providers.Controllers;

#if WINDOWS_UWP
using UnityEngine;
using XRTK.Extensions;
using XRTK.Services;
using Windows.UI.Input.Spatial;
#endif // WINDOWS_UWP

namespace XRTK.WindowsMixedReality.Providers.Controllers
{
    /// <summary>
    /// A Windows Mixed Reality Controller Instance.
    /// </summary>6
    [System.Runtime.InteropServices.Guid("37AA1554-3D46-4C72-AAC4-31023775F62B")]
    public class WindowsMixedRealityMotionController : BaseController
    {
        /// <inheritdoc />
        public WindowsMixedRealityMotionController() { }

        /// <inheritdoc />
        public WindowsMixedRealityMotionController(IMixedRealityControllerDataProvider controllerDataProvider, TrackingState trackingState, Handedness controllerHandedness, MixedRealityControllerMappingProfile controllerMappingProfile)
                : base(controllerDataProvider, trackingState, controllerHandedness, controllerMappingProfile)
        {
        }

        /// <inheritdoc />
        public override MixedRealityInteractionMapping[] DefaultInteractions => new[]
        {
            new MixedRealityInteractionMapping("Spatial Pointer", AxisType.SixDof, DeviceInputType.SpatialPointer),
            new MixedRealityInteractionMapping("Spatial Grip", AxisType.SixDof, DeviceInputType.SpatialGrip),
            new MixedRealityInteractionMapping("Grip Press", AxisType.SingleAxis, DeviceInputType.TriggerPress),
            new MixedRealityInteractionMapping("Trigger Position", AxisType.SingleAxis, DeviceInputType.Trigger),
            new MixedRealityInteractionMapping("Trigger Touched", AxisType.Digital, DeviceInputType.TriggerTouch),
            new MixedRealityInteractionMapping("Trigger Press (Select)", AxisType.Digital, DeviceInputType.Select),
            new MixedRealityInteractionMapping("Touchpad Position", AxisType.DualAxis, DeviceInputType.Touchpad),
            new MixedRealityInteractionMapping("Touchpad Touch", AxisType.Digital, DeviceInputType.TouchpadTouch),
            new MixedRealityInteractionMapping("Touchpad Press", AxisType.Digital, DeviceInputType.TouchpadPress),
            new MixedRealityInteractionMapping("Menu Press", AxisType.Digital, DeviceInputType.Menu),
            new MixedRealityInteractionMapping("Thumbstick Position", AxisType.DualAxis, DeviceInputType.ThumbStick),
            new MixedRealityInteractionMapping("Thumbstick Press", AxisType.Digital, DeviceInputType.ThumbStickPress),
        };

        /// <inheritdoc />
        public override MixedRealityInteractionMapping[] DefaultLeftHandedInteractions => DefaultInteractions;

        /// <inheritdoc />
        public override MixedRealityInteractionMapping[] DefaultRightHandedInteractions => DefaultInteractions;

#if UNITY_WSA

        /// <summary>
        /// The last updated source state reading for this Windows Mixed Reality Controller.
        /// </summary>
        public SpatialInteractionSourceState LastSourceStateReading { get; private set; }

        private Vector3 currentControllerPosition = Vector3.zero;
        private Quaternion currentControllerRotation = Quaternion.identity;
        private MixedRealityPose lastControllerPose = MixedRealityPose.ZeroIdentity;
        private MixedRealityPose currentControllerPose = MixedRealityPose.ZeroIdentity;

        private Vector3 currentPointerPosition = Vector3.zero;
        private Quaternion currentPointerRotation = Quaternion.identity;
        private MixedRealityPose currentPointerPose = MixedRealityPose.ZeroIdentity;

        private Vector3 currentGripPosition = Vector3.zero;
        private Quaternion currentGripRotation = Quaternion.identity;
        private MixedRealityPose currentGripPose = MixedRealityPose.ZeroIdentity;

        /// <summary>
        /// Update the controller data from the provided platform state
        /// </summary>
        /// <param name="interactionSourceState">The InteractionSourceState retrieved from the platform</param>
        public void UpdateController(SpatialInteractionSourceState interactionSourceState)
        {
            if (!Enabled) { return; }

            base.UpdateController();

            UpdateControllerData(interactionSourceState);

            if (Interactions == null)
            {
                Debug.LogError($"No interaction configuration for {GetType().Name} {ControllerHandedness}");
                Enabled = false;
            }

            for (int i = 0; i < Interactions?.Length; i++)
            {
                var interactionMapping = Interactions[i];

                switch (interactionMapping.InputType)
                {
                    case DeviceInputType.None:
                        break;
                    case DeviceInputType.SpatialPointer:
                        UpdatePointerData(interactionSourceState, interactionMapping);
                        break;
                    case DeviceInputType.Select:
                    case DeviceInputType.Trigger:
                    case DeviceInputType.TriggerTouch:
                    case DeviceInputType.TriggerPress:
                        UpdateTriggerData(interactionSourceState, interactionMapping);
                        break;
                    case DeviceInputType.SpatialGrip:
                        UpdateGripData(interactionSourceState, interactionMapping);
                        break;
                    case DeviceInputType.ThumbStick:
                    case DeviceInputType.ThumbStickPress:
                        UpdateThumbStickData(interactionSourceState, interactionMapping);
                        break;
                    case DeviceInputType.Touchpad:
                    case DeviceInputType.TouchpadTouch:
                    case DeviceInputType.TouchpadPress:
                        UpdateTouchPadData(interactionSourceState, interactionMapping);
                        break;
                    case DeviceInputType.Menu:
                        UpdateMenuData(interactionSourceState, interactionMapping);
                        break;
                    default:
                        Debug.LogError($"Input [{interactionMapping.Description}.{interactionMapping.InputType}] is not handled for this controller [{GetType().Name}]");
                        Enabled = false;
                        break;
                }

                interactionMapping.RaiseInputAction(InputSource, ControllerHandedness);
            }

            LastSourceStateReading = interactionSourceState;
        }

        private void UpdateControllerData(SpatialInteractionSourceState spatialInteractionSourceState)
        {
            var lastState = TrackingState;
            var sourceKind = spatialInteractionSourceState.Source.Kind;

            lastControllerPose = currentControllerPose;

            if (sourceKind == SpatialInteractionSourceKind.Hand ||
               (sourceKind == SpatialInteractionSourceKind.Controller && spatialInteractionSourceState.Source.IsPointingSupported))
            {
                // The source is either a hand or a controller that supports pointing.
                // We can now check for position and rotation.
                IsPositionAvailable = spatialInteractionSourceState.sourcePose.TryGetPosition(out currentControllerPosition);

                if (IsPositionAvailable)
                {
                    IsPositionApproximate = (spatialInteractionSourceState.sourcePose.positionAccuracy == SpatialInteractionSourcePositionAccuracy.Approximate);
                }
                else
                {
                    IsPositionApproximate = false;
                }

                IsRotationAvailable = spatialInteractionSourceState.sourcePose.TryGetRotation(out currentControllerRotation);

                // Devices are considered tracked if we receive position OR rotation data from the sensors.
                TrackingState = (IsPositionAvailable || IsRotationAvailable) ? TrackingState.Tracked : TrackingState.NotTracked;
            }
            else
            {
                // The input source does not support tracking.
                TrackingState = TrackingState.NotApplicable;
            }

            currentControllerPose.Position = currentControllerPosition;
            currentControllerPose.Rotation = currentControllerRotation;

            // Raise input system events if it is enabled.
            if (lastState != TrackingState)
            {
                InputSystem?.RaiseSourceTrackingStateChanged(InputSource, this, TrackingState);
            }

            if (TrackingState == TrackingState.Tracked && lastControllerPose != currentControllerPose)
            {
                if (IsPositionAvailable && IsRotationAvailable)
                {
                    InputSystem?.RaiseSourcePoseChanged(InputSource, this, currentControllerPose);
                }
                else if (IsPositionAvailable && !IsRotationAvailable)
                {
                    InputSystem?.RaiseSourcePositionChanged(InputSource, this, currentControllerPosition);
                }
                else if (!IsPositionAvailable && IsRotationAvailable)
                {
                    InputSystem?.RaiseSourceRotationChanged(InputSource, this, currentControllerRotation);
                }
            }
        }

        private void UpdatePointerData(SpatialInteractionSourceState spatialInteractionSourceState, MixedRealityInteractionMapping interactionMapping)
        {
            spatialInteractionSourceState.sourcePose.TryGetPosition(out currentPointerPosition, InteractionSourceNode.Pointer);
            spatialInteractionSourceState.sourcePose.TryGetRotation(out currentPointerRotation, InteractionSourceNode.Pointer);

            currentPointerPose.Position = currentPointerPosition;
            currentPointerPose.Rotation = currentPointerRotation;

            interactionMapping.PoseData = currentPointerPose;
        }

        private void UpdateGripData(SpatialInteractionSourceState spatialInteractionSourceState, MixedRealityInteractionMapping interactionMapping)
        {
            switch (interactionMapping.AxisType)
            {
                case AxisType.SixDof:
                    spatialInteractionSourceState.sourcePose.TryGetPosition(out currentGripPosition, InteractionSourceNode.Grip);
                    spatialInteractionSourceState.sourcePose.TryGetRotation(out currentGripRotation);

                    if (MixedRealityToolkit.TryGetSystem<IMixedRealityCameraSystem>(out var cameraSystem))
                    {
                        currentGripPose.Position = cameraSystem.MainCameraRig.PlayspaceTransform.TransformPoint(currentGripPosition);
                        currentGripPose.Rotation = Quaternion.Euler(cameraSystem.MainCameraRig.PlayspaceTransform.TransformDirection(currentGripRotation.eulerAngles));
                    }
                    else
                    {
                        currentGripPose.Position = currentGripPosition;
                        currentGripPose.Rotation = currentGripRotation;
                    }

                    interactionMapping.PoseData = currentGripPose;
                    break;
            }
        }

        private void UpdateTouchPadData(SpatialInteractionSourceState spatialInteractionSourceState, MixedRealityInteractionMapping interactionMapping)
        {
            switch (interactionMapping.InputType)
            {
                case DeviceInputType.TouchpadTouch:
                    interactionMapping.BoolData = spatialInteractionSourceState.touchpadTouched;
                    break;
                case DeviceInputType.TouchpadPress:
                    interactionMapping.BoolData = spatialInteractionSourceState.touchpadPressed;
                    break;
                case DeviceInputType.Touchpad:
                    interactionMapping.Vector2Data = spatialInteractionSourceState.touchpadPosition;
                    break;
            }
        }

        private void UpdateThumbStickData(SpatialInteractionSourceState spatialInteractionSourceState, MixedRealityInteractionMapping interactionMapping)
        {
            switch (interactionMapping.InputType)
            {
                case DeviceInputType.ThumbStickPress:
                    interactionMapping.BoolData = spatialInteractionSourceState.thumbstickPressed;
                    break;
                case DeviceInputType.ThumbStick:
                    interactionMapping.Vector2Data = spatialInteractionSourceState.thumbstickPosition;
                    break;
            }
        }

        private void UpdateTriggerData(SpatialInteractionSourceState spatialInteractionSourceState, MixedRealityInteractionMapping interactionMapping)
        {
            switch (interactionMapping.InputType)
            {
                case DeviceInputType.TriggerPress:
                    interactionMapping.BoolData = spatialInteractionSourceState.IsGrasped;
                    break;
                case DeviceInputType.Select:
                    bool selectPressed = spatialInteractionSourceState.IsSelectPressed;

                    // BEGIN WORKAROUND: Unity issue #1033526
                    // See https://issuetracker.unity3d.com/issues/hololens-interactionsourcestate-dot-selectpressed-is-false-when-air-tap-and-hold
                    // Bug was discovered May 2018 and still exists as of today Feb 2019 in version 2018.3.4f1, timeline for fix is unknown
                    // The bug only affects the development workflow via Holographic Remoting or Simulation
                    if (spatialInteractionSourceState.source.kind == InteractionSourceKind.Hand)
                    {
                        Debug.Assert(!(UnityEngine.XR.WSA.HolographicRemoting.ConnectionState == UnityEngine.XR.WSA.HolographicStreamerConnectionState.Connected
                                       && spatialInteractionSourceState.selectPressed),
                            "Unity issue #1033526 seems to have been resolved. Please remove this ugly workaround!");

                        // This workaround is safe as long as all these assumptions hold:
                        Debug.Assert(!spatialInteractionSourceState.source.supportsGrasp);
                        Debug.Assert(!spatialInteractionSourceState.source.supportsMenu);
                        Debug.Assert(!spatialInteractionSourceState.source.supportsPointing);
                        Debug.Assert(!spatialInteractionSourceState.source.supportsThumbstick);
                        Debug.Assert(!spatialInteractionSourceState.source.supportsTouchpad);

                        selectPressed = spatialInteractionSourceState.anyPressed;
                    }
                    // END WORKAROUND: Unity issue #1033526

                    interactionMapping.BoolData = selectPressed;
                    break;
                case DeviceInputType.Trigger:
                    interactionMapping.FloatData = (float)spatialInteractionSourceState.SelectPressedValue;
                    break;
                case DeviceInputType.TriggerTouch:
                    interactionMapping.BoolData = spatialInteractionSourceState.SelectPressedValue > 0;
                    break;
            }
        }

        private void UpdateMenuData(SpatialInteractionSourceState spatialInteractionSourceState, MixedRealityInteractionMapping interactionMapping) => interactionMapping.BoolData = spatialInteractionSourceState.IsMenuPressed;

#endif // UNITY_WSA
    }
}
