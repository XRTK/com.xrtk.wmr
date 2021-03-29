// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using XRTK.Attributes;
using XRTK.Definitions.Platforms;
using XRTK.Interfaces.InputSystem;
using XRTK.Providers.Controllers;
using XRTK.WindowsMixedReality.Profiles;

#if UNITY_WSA
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.WSA.Input;
using XRTK.Definitions.Devices;
using XRTK.Definitions.InputSystem;
using XRTK.Definitions.Utilities;
using XRTK.Extensions;
using XRTK.Interfaces.Providers.Controllers;
using XRTK.Services;
using WsaGestureSettings = UnityEngine.XR.WSA.Input.GestureSettings;
#endif // UNITY_WSA

#if WINDOWS_UWP
using Windows.ApplicationModel.Core;
using Windows.Perception;
using Windows.Storage.Streams;
using Windows.UI.Input.Spatial;
using XRTK.WindowsMixedReality.Utilities;
#endif // WINDOWS_UWP

namespace XRTK.WindowsMixedReality.Providers.Controllers
{
    /// <summary>
    /// The device manager for Windows Mixed Reality controllers.
    /// </summary>
    [Obsolete]
    [RuntimePlatform(typeof(UniversalWindowsPlatform))]
    [System.Runtime.InteropServices.Guid("12E02EF8-4177-46AB-BC50-19AF7148BD4A")]
    public class WindowsMixedRealityControllerDataProvider : BaseControllerDataProvider
    {
        /// <inheritdoc />
        public WindowsMixedRealityControllerDataProvider(string name, uint priority, WindowsMixedRealityControllerDataProviderProfile profile, IMixedRealityInputSystem parentService)
            : base(name, priority, profile, parentService)
        {
#if UNITY_WSA
            if (MixedRealityToolkit.TryGetSystemProfile<IMixedRealityInputSystem, MixedRealityInputSystemProfile>(out var inputSystemProfile))
            {
                gestures = inputSystemProfile.GesturesProfile.Gestures;
            }

            gestureAutoStartBehavior = profile.WindowsGestureAutoStart;
            GestureSettings = profile.ManipulationGestures;
            NavigationSettings = profile.NavigationGestures;
            RailsNavigationSettings = profile.RailsNavigationGestures;
            UseRailsNavigation = profile.UseRailsNavigation;
            gestureRecognizer = new GestureRecognizer();
            navigationGestureRecognizer = new GestureRecognizer();
#endif // UNITY_WSA
        }

#if UNITY_WSA

        private readonly AutoStartBehavior gestureAutoStartBehavior;

        private readonly MixedRealityGestureMapping[] gestures;

        /// <summary>
        /// The max expected sources is two - two controllers and/or two hands.
        /// We'll set it to 20 just to be certain we can't run out of sources.
        /// </summary>
        public const int MaxInteractionSourceStates = 20;

        /// <summary>
        /// Dictionary to capture all active controllers detected
        /// </summary>
        private readonly Dictionary<uint, IMixedRealityController> activeControllers = new Dictionary<uint, IMixedRealityController>();

        /// <summary>
        /// Cache of the states captured from the Unity InteractionManager for UWP
        /// </summary>
        private readonly InteractionSourceState[] interactionManagerStates = new InteractionSourceState[MaxInteractionSourceStates];

        /// <summary>
        /// The number of states captured most recently
        /// </summary>
        private int numInteractionManagerStates;

        /// <summary>
        /// The current source state reading for the Unity InteractionManager for UWP
        /// </summary>
        public InteractionSourceState[] LastInteractionManagerStateReading { get; protected set; }

        private static bool gestureRecognizerEnabled;

        /// <summary>
        /// Enables or disables the gesture recognizer.
        /// </summary>
        /// <remarks>
        /// Automatically disabled navigation recognizer if enabled.
        /// </remarks>
        public static bool GestureRecognizerEnabled
        {
            get => gestureRecognizerEnabled;
            set
            {
                gestureRecognizerEnabled = value;
                if (!Application.isPlaying) { return; }

                if (!gestureRecognizer.IsCapturingGestures() && gestureRecognizerEnabled)
                {
                    NavigationRecognizerEnabled = false;
                    gestureRecognizer.StartCapturingGestures();
                }

                if (gestureRecognizer.IsCapturingGestures() && !gestureRecognizerEnabled)
                {
                    gestureRecognizer.CancelGestures();
                }
            }
        }

        private static bool navigationRecognizerEnabled;

        /// <summary>
        /// Enables or disables the navigation recognizer.
        /// </summary>
        /// <remarks>
        /// Automatically disables the gesture recognizer if enabled.
        /// </remarks>
        public static bool NavigationRecognizerEnabled
        {
            get => navigationRecognizerEnabled;
            set
            {
                navigationRecognizerEnabled = value;

                if (!Application.isPlaying) { return; }

                if (!navigationGestureRecognizer.IsCapturingGestures() && navigationRecognizerEnabled)
                {
                    GestureRecognizerEnabled = false;
                    navigationGestureRecognizer.StartCapturingGestures();
                }

                if (navigationGestureRecognizer.IsCapturingGestures() && !navigationRecognizerEnabled)
                {
                    navigationGestureRecognizer.CancelGestures();
                }
            }
        }

        private static WindowsGestureSettings gestureSettings = WindowsGestureSettings.Hold | WindowsGestureSettings.ManipulationTranslate;

        /// <summary>
        /// Current Gesture Settings for the GestureRecognizer
        /// </summary>
        public static WindowsGestureSettings GestureSettings
        {
            get => gestureSettings;
            set
            {
                gestureSettings = value;

                if (Application.isPlaying)
                {
                    gestureRecognizer.UpdateAndResetGestures(WSAGestureSettings);
                }
            }
        }

        private static WindowsGestureSettings navigationSettings = WindowsGestureSettings.NavigationX | WindowsGestureSettings.NavigationY | WindowsGestureSettings.NavigationZ;

        /// <summary>
        /// Current Navigation Gesture Recognizer Settings.
        /// </summary>
        public static WindowsGestureSettings NavigationSettings
        {
            get => navigationSettings;
            set
            {
                navigationSettings = value;

                if (Application.isPlaying)
                {
                    navigationGestureRecognizer.UpdateAndResetGestures(WSANavigationSettings);
                }
            }
        }

        private static WindowsGestureSettings railsNavigationSettings = WindowsGestureSettings.NavigationRailsX | WindowsGestureSettings.NavigationRailsY | WindowsGestureSettings.NavigationRailsZ;

        /// <summary>
        /// Current Navigation Gesture Recognizer Rails Settings.
        /// </summary>
        public static WindowsGestureSettings RailsNavigationSettings
        {
            get => railsNavigationSettings;
            set
            {
                railsNavigationSettings = value;

                if (Application.isPlaying)
                {
                    navigationGestureRecognizer.UpdateAndResetGestures(WSARailsNavigationSettings);
                }
            }
        }

        private static bool useRailsNavigation = true;

        /// <summary>
        /// Should the Navigation Gesture Recognizer use Rails?
        /// </summary>
        public static bool UseRailsNavigation
        {
            get => useRailsNavigation;
            set
            {
                useRailsNavigation = value;

                if (Application.isPlaying)
                {
                    navigationGestureRecognizer.UpdateAndResetGestures(useRailsNavigation ? WSANavigationSettings : WSARailsNavigationSettings);
                }
            }
        }

        private MixedRealityInputAction tapAction = MixedRealityInputAction.None;
        private MixedRealityInputAction doubleTapAction = MixedRealityInputAction.None;
        private MixedRealityInputAction holdAction = MixedRealityInputAction.None;
        private MixedRealityInputAction navigationAction = MixedRealityInputAction.None;
        private MixedRealityInputAction manipulationAction = MixedRealityInputAction.None;

        private static GestureRecognizer gestureRecognizer;
        private static WsaGestureSettings WSAGestureSettings => (WsaGestureSettings)gestureSettings;

        private static GestureRecognizer navigationGestureRecognizer;
        private static WsaGestureSettings WSANavigationSettings => (WsaGestureSettings)navigationSettings;
        private static WsaGestureSettings WSARailsNavigationSettings => (WsaGestureSettings)railsNavigationSettings;

        #region IMixedRealityService Interface

        /// <inheritdoc/>
        public override void Enable()
        {
            if (!Application.isPlaying) { return; }

            gestureRecognizer.Tapped += GestureRecognizer_Tapped;
            gestureRecognizer.HoldStarted += GestureRecognizer_HoldStarted;
            gestureRecognizer.HoldCompleted += GestureRecognizer_HoldCompleted;
            gestureRecognizer.HoldCanceled += GestureRecognizer_HoldCanceled;

            gestureRecognizer.ManipulationStarted += GestureRecognizer_ManipulationStarted;
            gestureRecognizer.ManipulationUpdated += GestureRecognizer_ManipulationUpdated;
            gestureRecognizer.ManipulationCompleted += GestureRecognizer_ManipulationCompleted;
            gestureRecognizer.ManipulationCanceled += GestureRecognizer_ManipulationCanceled;

            navigationGestureRecognizer.NavigationStarted += NavigationGestureRecognizer_NavigationStarted;
            navigationGestureRecognizer.NavigationUpdated += NavigationGestureRecognizer_NavigationUpdated;
            navigationGestureRecognizer.NavigationCompleted += NavigationGestureRecognizer_NavigationCompleted;
            navigationGestureRecognizer.NavigationCanceled += NavigationGestureRecognizer_NavigationCanceled;

            for (int i = 0; i < gestures?.Length; i++)
            {
                var gesture = gestures[i];

                switch (gesture.GestureType)
                {
                    case GestureInputType.Hold:
                        holdAction = gesture.Action;
                        break;
                    case GestureInputType.Manipulation:
                        manipulationAction = gesture.Action;
                        break;
                    case GestureInputType.Navigation:
                        navigationAction = gesture.Action;
                        break;
                    case GestureInputType.Tap:
                        tapAction = gesture.Action;
                        break;
                    case GestureInputType.DoubleTap:
                        doubleTapAction = gesture.Action;
                        break;
                }
            }

            InteractionManager.InteractionSourceDetected += InteractionManager_InteractionSourceDetected;
            InteractionManager.InteractionSourceLost += InteractionManager_InteractionSourceLost;

            numInteractionManagerStates = InteractionManager.GetCurrentReading(interactionManagerStates);

            // NOTE: We update the source state data, in case an app wants to query it on source detected.
            for (var i = 0; i < numInteractionManagerStates; i++)
            {
                var state = interactionManagerStates[i];

                if (state.sourcePose.positionAccuracy == InteractionSourcePositionAccuracy.None) { continue; }

                var controller = GetController(state.source, true);

                if (controller != null)
                {
                    InputSystem?.RaiseSourceDetected(controller.InputSource, controller);
                    controller.UpdateController(state);
                }
            }

            if (gestures != null &&
                gestureAutoStartBehavior == AutoStartBehavior.AutoStart)
            {
                GestureRecognizerEnabled = true;
            }
        }

        /// <inheritdoc/>
        public override void Update()
        {
            base.Update();

            numInteractionManagerStates = InteractionManager.GetCurrentReading(interactionManagerStates);

            for (var i = 0; i < numInteractionManagerStates; i++)
            {
                var state = interactionManagerStates[i];
                var isTracked = state.sourcePose.positionAccuracy != InteractionSourcePositionAccuracy.None;
                var raiseSourceDetected = !activeControllers.ContainsKey(state.source.id);
                var controller = GetController(state.source, raiseSourceDetected && isTracked);

                if (controller != null && raiseSourceDetected)
                {
                    InputSystem?.RaiseSourceDetected(controller.InputSource, controller);
                }

                controller?.UpdateController(state);

                if (!isTracked)
                {
                    RemoveController(state);
                }
            }

            LastInteractionManagerStateReading = interactionManagerStates;
        }

        /// <inheritdoc/>
        public override void Disable()
        {
            base.Disable();

            gestureRecognizer.Tapped -= GestureRecognizer_Tapped;
            gestureRecognizer.HoldStarted -= GestureRecognizer_HoldStarted;
            gestureRecognizer.HoldCompleted -= GestureRecognizer_HoldCompleted;
            gestureRecognizer.HoldCanceled -= GestureRecognizer_HoldCanceled;

            gestureRecognizer.ManipulationStarted -= GestureRecognizer_ManipulationStarted;
            gestureRecognizer.ManipulationUpdated -= GestureRecognizer_ManipulationUpdated;
            gestureRecognizer.ManipulationCompleted -= GestureRecognizer_ManipulationCompleted;
            gestureRecognizer.ManipulationCanceled -= GestureRecognizer_ManipulationCanceled;

            navigationGestureRecognizer.NavigationStarted -= NavigationGestureRecognizer_NavigationStarted;
            navigationGestureRecognizer.NavigationUpdated -= NavigationGestureRecognizer_NavigationUpdated;
            navigationGestureRecognizer.NavigationCompleted -= NavigationGestureRecognizer_NavigationCompleted;
            navigationGestureRecognizer.NavigationCanceled -= NavigationGestureRecognizer_NavigationCanceled;

            InteractionManager.InteractionSourceDetected -= InteractionManager_InteractionSourceDetected;
            InteractionManager.InteractionSourceLost -= InteractionManager_InteractionSourceLost;

            InteractionSourceState[] states = InteractionManager.GetCurrentReading();

            for (var i = 0; i < states.Length; i++)
            {
                RemoveController(states[i], false);
            }
        }

        protected override void OnDispose(bool finalizing)
        {
            navigationGestureRecognizer.Dispose();
            gestureRecognizer.Dispose();

            base.OnDispose(finalizing);
        }

        #endregion IMixedRealityService Interface

        #region Controller Utilities

        /// <summary>
        /// Retrieve the source controller from the Active Store, or create a new device and register it
        /// </summary>
        /// <param name="interactionSource">Source State provided by the SDK</param>
        /// <param name="addController">Should the Source be added as a controller if it isn't found?</param>
        /// <returns>New or Existing Controller Input Source</returns>
        private WindowsMixedRealityMotionController GetController(InteractionSource interactionSource, bool addController = false)
        {
            //If a device is already registered with the ID provided, just return it.
            if (activeControllers.ContainsKey(interactionSource.id))
            {
                var controller = activeControllers[interactionSource.id] as WindowsMixedRealityMotionController;
                Debug.Assert(controller != null);
                return controller;
            }

            if (!addController) { return null; }

            Handedness handedness;

            switch (interactionSource.handedness)
            {
                default:
                    return null;
                case InteractionSourceHandedness.Left:
                    handedness = Handedness.Left;
                    break;
                case InteractionSourceHandedness.Right:
                    handedness = Handedness.Right;
                    break;
            }


            WindowsMixedRealityMotionController detectedController;
            var controllerType = interactionSource.kind == InteractionSourceKind.Hand
                ? typeof(HololensOneController)
                : typeof(WindowsMixedRealityMotionController);

            try
            {
                detectedController = new WindowsMixedRealityMotionController(this, TrackingState.NotApplicable, handedness, GetControllerMappingProfile(controllerType, handedness));
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to create {nameof(WindowsMixedRealityMotionController)}!\n{e}");
                return null;
            }

            detectedController.TryRenderControllerModel(interactionSource.kind == InteractionSourceKind.Hand);

            activeControllers.Add(interactionSource.id, detectedController);
            AddController(detectedController);
            return detectedController;
        }

        /// <summary>
        /// Remove the selected controller from the Active Store
        /// </summary>
        /// <param name="interactionSourceState">Source State provided by the SDK to remove</param>
        /// <param name="clearFromRegistry">Should the controller be removed from the registry?</param>
        private void RemoveController(InteractionSourceState interactionSourceState, bool clearFromRegistry = true)
        {
            var controller = GetController(interactionSourceState.source);

            if (controller != null)
            {
                InputSystem?.RaiseSourceLost(controller.InputSource, controller);
            }

            if (clearFromRegistry)
            {
                RemoveController(controller);
                activeControllers.Remove(interactionSourceState.source.id);
            }
        }

        #endregion Controller Utilities

        #region Unity InteractionManager Events

        /// <summary>
        /// SDK Interaction Source Detected Event handler
        /// </summary>
        /// <param name="args">SDK source detected event arguments</param>
        private void InteractionManager_InteractionSourceDetected(InteractionSourceDetectedEventArgs args)
        {
            if (args.state.sourcePose.positionAccuracy == InteractionSourcePositionAccuracy.None)
            {
                return;
            }

            var raiseSourceDetected = !activeControllers.ContainsKey(args.state.source.id);
            var controller = GetController(args.state.source, true);

            if (controller != null && raiseSourceDetected)
            {
                InputSystem?.RaiseSourceDetected(controller.InputSource, controller);
            }

            controller?.UpdateController(args.state);
        }

        /// <summary>
        /// SDK Interaction Source Lost Event handler
        /// </summary>
        /// <param name="args">SDK source updated event arguments</param>
        private void InteractionManager_InteractionSourceLost(InteractionSourceLostEventArgs args)
        {
            RemoveController(args.state);
        }

        #endregion Unity InteractionManager Events

        #region Gesture Recognizer Events

        private void GestureRecognizer_Tapped(TappedEventArgs args)
        {
            var controller = GetController(args.source);

            if (controller != null)
            {
                if (args.tapCount == 1)
                {
                    InputSystem?.RaiseGestureStarted(controller, tapAction);
                    InputSystem?.RaiseGestureCompleted(controller, tapAction);
                }
                else if (args.tapCount == 2)
                {
                    InputSystem?.RaiseGestureStarted(controller, doubleTapAction);
                    InputSystem?.RaiseGestureCompleted(controller, doubleTapAction);
                }
            }
        }

        private void GestureRecognizer_HoldStarted(HoldStartedEventArgs args)
        {
            var controller = GetController(args.source);
            if (controller != null)
            {
                InputSystem?.RaiseGestureStarted(controller, holdAction);
            }
        }

        private void GestureRecognizer_HoldCompleted(HoldCompletedEventArgs args)
        {
            var controller = GetController(args.source);
            if (controller != null)
            {
                InputSystem.RaiseGestureCompleted(controller, holdAction);
            }
        }

        private void GestureRecognizer_HoldCanceled(HoldCanceledEventArgs args)
        {
            var controller = GetController(args.source);
            if (controller != null)
            {
                InputSystem.RaiseGestureCanceled(controller, holdAction);
            }
        }

        private void GestureRecognizer_ManipulationStarted(ManipulationStartedEventArgs args)
        {
            var controller = GetController(args.source);
            if (controller != null)
            {
                InputSystem.RaiseGestureStarted(controller, manipulationAction);
            }
        }

        private void GestureRecognizer_ManipulationUpdated(ManipulationUpdatedEventArgs args)
        {
            var controller = GetController(args.source);
            if (controller != null)
            {
                InputSystem.RaiseGestureUpdated(controller, manipulationAction, args.cumulativeDelta);
            }
        }

        private void GestureRecognizer_ManipulationCompleted(ManipulationCompletedEventArgs args)
        {
            var controller = GetController(args.source);
            if (controller != null)
            {
                InputSystem.RaiseGestureCompleted(controller, manipulationAction, args.cumulativeDelta);
            }
        }

        private void GestureRecognizer_ManipulationCanceled(ManipulationCanceledEventArgs args)
        {
            var controller = GetController(args.source);
            if (controller != null)
            {
                InputSystem.RaiseGestureCanceled(controller, manipulationAction);
            }
        }

        #endregion Gesture Recognizer Events

        #region Navigation Recognizer Events

        private void NavigationGestureRecognizer_NavigationStarted(NavigationStartedEventArgs args)
        {
            var controller = GetController(args.source);
            if (controller != null)
            {
                InputSystem.RaiseGestureStarted(controller, navigationAction);
            }
        }

        private void NavigationGestureRecognizer_NavigationUpdated(NavigationUpdatedEventArgs args)
        {
            var controller = GetController(args.source);
            if (controller != null)
            {
                InputSystem.RaiseGestureUpdated(controller, navigationAction, args.normalizedOffset);
            }
        }

        private void NavigationGestureRecognizer_NavigationCompleted(NavigationCompletedEventArgs args)
        {
            var controller = GetController(args.source);
            if (controller != null)
            {
                InputSystem.RaiseGestureCompleted(controller, navigationAction, args.normalizedOffset);
            }
        }

        private void NavigationGestureRecognizer_NavigationCanceled(NavigationCanceledEventArgs args)
        {
            var controller = GetController(args.source);
            if (controller != null)
            {
                InputSystem.RaiseGestureCanceled(controller, navigationAction);
            }
        }

        #endregion Navigation Recognizer Events

#endif // UNITY_WSA

    }
}
