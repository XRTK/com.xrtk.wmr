// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XRTK.Definitions.Devices;
using XRTK.Definitions.InputSystem;
using XRTK.Definitions.Utilities;
using XRTK.Interfaces.InputSystem;
using XRTK.Interfaces.Providers.Controllers;
using XRTK.Providers.Controllers;

#if UNITY_WSA
using UnityEngine;
using UnityEngine.XR.WSA.Input;
using XRTK.Extensions;
using XRTK.Services;
#endif

namespace XRTK.WindowsMixedReality.Controllers
{
    /// <summary>
    /// A Windows Mixed Reality Controller Instance.
    /// </summary>
    public class WindowsMixedRealityController : BaseController
    {
        /// <inheritdoc />
        public WindowsMixedRealityController(IMixedRealityControllerDataProvider controllerDataProvider, TrackingState trackingState, Handedness controllerHandedness, IMixedRealityInputSource inputSource = null, MixedRealityInteractionMapping[] interactions = null)
                : base(controllerDataProvider, trackingState, controllerHandedness, inputSource, interactions)
        {
        }

        /// <summary>
        /// The Windows Mixed Reality Controller default interactions.
        /// </summary>
        /// <remarks>A single interaction mapping works for both left and right controllers.</remarks>
        public override MixedRealityInteractionMapping[] DefaultInteractions => new[]
        {
            new MixedRealityInteractionMapping(0, "Spatial Pointer", AxisType.SixDof, DeviceInputType.SpatialPointer, MixedRealityInputAction.None),
            new MixedRealityInteractionMapping(1, "Spatial Grip", AxisType.SixDof, DeviceInputType.SpatialGrip, MixedRealityInputAction.None),
            new MixedRealityInteractionMapping(2, "Grip Press", AxisType.SingleAxis, DeviceInputType.TriggerPress, MixedRealityInputAction.None),
            new MixedRealityInteractionMapping(3, "Trigger Position", AxisType.SingleAxis, DeviceInputType.Trigger, MixedRealityInputAction.None),
            new MixedRealityInteractionMapping(4, "Trigger Touched", AxisType.Digital, DeviceInputType.TriggerTouch, MixedRealityInputAction.None),
            new MixedRealityInteractionMapping(5, "Trigger Press (Select)", AxisType.Digital, DeviceInputType.Select, MixedRealityInputAction.None),
            new MixedRealityInteractionMapping(6, "Touchpad Position", AxisType.DualAxis, DeviceInputType.Touchpad, MixedRealityInputAction.None),
            new MixedRealityInteractionMapping(7, "Touchpad Touch", AxisType.Digital, DeviceInputType.TouchpadTouch, MixedRealityInputAction.None),
            new MixedRealityInteractionMapping(8, "Touchpad Press", AxisType.Digital, DeviceInputType.TouchpadPress, MixedRealityInputAction.None),
            new MixedRealityInteractionMapping(9, "Menu Press", AxisType.Digital, DeviceInputType.Menu, MixedRealityInputAction.None),
            new MixedRealityInteractionMapping(10, "Thumbstick Position", AxisType.DualAxis, DeviceInputType.ThumbStick, MixedRealityInputAction.None),
            new MixedRealityInteractionMapping(11, "Thumbstick Press", AxisType.Digital, DeviceInputType.ThumbStickPress, MixedRealityInputAction.None),
        };

        /// <inheritdoc />
        public override MixedRealityInteractionMapping[] DefaultLeftHandedInteractions => DefaultInteractions;

        /// <inheritdoc />
        public override MixedRealityInteractionMapping[] DefaultRightHandedInteractions => DefaultInteractions;

        /// <inheritdoc />
        public override void SetupDefaultInteractions(Handedness controllerHandedness)
        {
            AssignControllerMappings(DefaultInteractions);
        }

#if UNITY_WSA

        /// <summary>
        /// The last updated source state reading for this Windows Mixed Reality Controller.
        /// </summary>
        public InteractionSourceState LastSourceStateReading { get; private set; }

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

        #region Update data functions

        /// <summary>
        /// Update the controller data from the provided platform state
        /// </summary>
        /// <param name="interactionSourceState">The InteractionSourceState retrieved from the platform</param>
        public void UpdateController(InteractionSourceState interactionSourceState)
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
                        Debug.LogError($"Input [{interactionMapping.InputType}] is not handled for this controller [{GetType().Name}]");
                        Enabled = false;
                        break;
                }

                interactionMapping.RaiseInputAction(InputSource, ControllerHandedness);
            }

            LastSourceStateReading = interactionSourceState;
        }

        /// <summary>
        /// Update the "Controller" input from the device
        /// </summary>
        /// <param name="interactionSourceState">The InteractionSourceState retrieved from the platform</param>
        private void UpdateControllerData(InteractionSourceState interactionSourceState)
        {
            var lastState = TrackingState;
            var sourceKind = interactionSourceState.source.kind;

            lastControllerPose = currentControllerPose;

            if (sourceKind == InteractionSourceKind.Hand ||
               (sourceKind == InteractionSourceKind.Controller && interactionSourceState.source.supportsPointing))
            {
                // The source is either a hand or a controller that supports pointing.
                // We can now check for position and rotation.
                IsPositionAvailable = interactionSourceState.sourcePose.TryGetPosition(out currentControllerPosition);

                if (IsPositionAvailable)
                {
                    IsPositionApproximate = (interactionSourceState.sourcePose.positionAccuracy == InteractionSourcePositionAccuracy.Approximate);
                }
                else
                {
                    IsPositionApproximate = false;
                }

                IsRotationAvailable = interactionSourceState.sourcePose.TryGetRotation(out currentControllerRotation);

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
                MixedRealityToolkit.InputSystem?.RaiseSourceTrackingStateChanged(InputSource, this, TrackingState);
            }

            if (TrackingState == TrackingState.Tracked && lastControllerPose != currentControllerPose)
            {
                if (IsPositionAvailable && IsRotationAvailable)
                {
                    MixedRealityToolkit.InputSystem?.RaiseSourcePoseChanged(InputSource, this, currentControllerPose);
                }
                else if (IsPositionAvailable && !IsRotationAvailable)
                {
                    MixedRealityToolkit.InputSystem?.RaiseSourcePositionChanged(InputSource, this, currentControllerPosition);
                }
                else if (!IsPositionAvailable && IsRotationAvailable)
                {
                    MixedRealityToolkit.InputSystem?.RaiseSourceRotationChanged(InputSource, this, currentControllerRotation);
                }
            }
        }

        /// <summary>
        /// Update the "Spatial Pointer" input from the device
        /// </summary>
        /// <param name="interactionSourceState">The InteractionSourceState retrieved from the platform</param>
        /// <param name="interactionMapping"></param>
        private void UpdatePointerData(InteractionSourceState interactionSourceState, MixedRealityInteractionMapping interactionMapping)
        {
            interactionSourceState.sourcePose.TryGetPosition(out currentPointerPosition, InteractionSourceNode.Pointer);
            interactionSourceState.sourcePose.TryGetRotation(out currentPointerRotation, InteractionSourceNode.Pointer);

            currentPointerPose.Position = currentPointerPosition;
            currentPointerPose.Rotation = currentPointerRotation;

            interactionMapping.PoseData = currentPointerPose;
        }

        /// <summary>
        /// Update the "Spatial Grip" input from the device
        /// </summary>
        /// <param name="interactionSourceState">The InteractionSourceState retrieved from the platform</param>
        /// <param name="interactionMapping"></param>
        private void UpdateGripData(InteractionSourceState interactionSourceState, MixedRealityInteractionMapping interactionMapping)
        {
            switch (interactionMapping.AxisType)
            {
                case AxisType.SixDof:
                    {
                        interactionSourceState.sourcePose.TryGetPosition(out currentGripPosition, InteractionSourceNode.Grip);
                        interactionSourceState.sourcePose.TryGetRotation(out currentGripRotation, InteractionSourceNode.Grip);

                        var cameraRig = MixedRealityToolkit.CameraSystem?.MainCameraRig;

                        if (cameraRig != null &&
                            cameraRig.PlayspaceTransform != null)
                        {
                            currentGripPose.Position = cameraRig.PlayspaceTransform.TransformPoint(currentGripPosition);
                            currentGripPose.Rotation = Quaternion.Euler(cameraRig.PlayspaceTransform.TransformDirection(currentGripRotation.eulerAngles));
                        }
                        else
                        {
                            currentGripPose.Position = currentGripPosition;
                            currentGripPose.Rotation = currentGripRotation;
                        }

                        interactionMapping.PoseData = currentGripPose;
                    }
                    break;
            }
        }

        /// <summary>
        /// Update the Touchpad input from the device
        /// </summary>
        /// <param name="interactionSourceState">The InteractionSourceState retrieved from the platform</param>
        /// <param name="interactionMapping"></param>
        private void UpdateTouchPadData(InteractionSourceState interactionSourceState, MixedRealityInteractionMapping interactionMapping)
        {
            switch (interactionMapping.InputType)
            {
                case DeviceInputType.TouchpadTouch:
                    {
                        interactionMapping.BoolData = interactionSourceState.touchpadTouched;
                        break;
                    }
                case DeviceInputType.TouchpadPress:
                    {
                        interactionMapping.BoolData = interactionSourceState.touchpadPressed;
                        break;
                    }
                case DeviceInputType.Touchpad:
                    {
                        interactionMapping.Vector2Data = interactionSourceState.touchpadPosition;
                        break;
                    }
            }
        }

        /// <summary>
        /// Update the Thumbstick input from the device
        /// </summary>
        /// <param name="interactionSourceState">The InteractionSourceState retrieved from the platform</param>
        /// <param name="interactionMapping"></param>
        private void UpdateThumbStickData(InteractionSourceState interactionSourceState, MixedRealityInteractionMapping interactionMapping)
        {
            switch (interactionMapping.InputType)
            {
                case DeviceInputType.ThumbStickPress:
                    {
                        interactionMapping.BoolData = interactionSourceState.thumbstickPressed;
                        break;
                    }
                case DeviceInputType.ThumbStick:
                    {
                        interactionMapping.Vector2Data = interactionSourceState.thumbstickPosition;
                        break;
                    }
            }
        }

        /// <summary>
        /// Update the Trigger input from the device
        /// </summary>
        /// <param name="interactionSourceState">The InteractionSourceState retrieved from the platform</param>
        /// <param name="interactionMapping"></param>
        private void UpdateTriggerData(InteractionSourceState interactionSourceState, MixedRealityInteractionMapping interactionMapping)
        {
            switch (interactionMapping.InputType)
            {
                case DeviceInputType.TriggerPress:
                    interactionMapping.BoolData = interactionSourceState.grasped;
                    break;
                case DeviceInputType.Select:
                    {
                        bool selectPressed = interactionSourceState.selectPressed;

                        // BEGIN WORKAROUND: Unity issue #1033526
                        // See https://issuetracker.unity3d.com/issues/hololens-interactionsourcestate-dot-selectpressed-is-false-when-air-tap-and-hold
                        // Bug was discovered May 2018 and still exists as of today Feb 2019 in version 2018.3.4f1, timeline for fix is unknown
                        // The bug only affects the development workflow via Holographic Remoting or Simulation
                        if (interactionSourceState.source.kind == InteractionSourceKind.Hand)
                        {
                            Debug.Assert(!(UnityEngine.XR.WSA.HolographicRemoting.ConnectionState == UnityEngine.XR.WSA.HolographicStreamerConnectionState.Connected
                                           && interactionSourceState.selectPressed),
                                         "Unity issue #1033526 seems to have been resolved. Please remove this ugly workaround!");

                            // This workaround is safe as long as all these assumptions hold:
                            Debug.Assert(!interactionSourceState.source.supportsGrasp);
                            Debug.Assert(!interactionSourceState.source.supportsMenu);
                            Debug.Assert(!interactionSourceState.source.supportsPointing);
                            Debug.Assert(!interactionSourceState.source.supportsThumbstick);
                            Debug.Assert(!interactionSourceState.source.supportsTouchpad);

                            selectPressed = interactionSourceState.anyPressed;
                        }
                        // END WORKAROUND: Unity issue #1033526

                        interactionMapping.BoolData = selectPressed;
                        break;
                    }
                case DeviceInputType.Trigger:
                    {
                        interactionMapping.FloatData = interactionSourceState.selectPressedAmount;
                        break;
                    }
                case DeviceInputType.TriggerTouch:
                    {
                        interactionMapping.BoolData = interactionSourceState.selectPressedAmount > 0;
                        break;
                    }
            }
        }

        /// <summary>
        /// Update the Menu button state.
        /// </summary>
        /// <param name="interactionSourceState"></param>
        /// <param name="interactionMapping"></param>
        private void UpdateMenuData(InteractionSourceState interactionSourceState, MixedRealityInteractionMapping interactionMapping)
        {
            interactionMapping.BoolData = interactionSourceState.menuPressed;
        }

        #endregion Update data functions

#endif // UNITY_WSA
    }
}