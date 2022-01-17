//// Copyright (c) XRTK. All rights reserved.
//// Licensed under the MIT License. See LICENSE in the project root for license information.

//#if WINDOWS_UWP

//using UnityEngine;
//using Windows.UI.Input;
//using Windows.UI.Input.Spatial;
//using XRTK.Definitions.Devices;
//using XRTK.Definitions.InputSystem;
//using XRTK.Definitions.Utilities;
//using XRTK.Interfaces.InputSystem;
//using XRTK.Services;
//using XRTK.WindowsMixedReality.Definitions;
//using XRTK.WindowsMixedReality.Profiles;

//namespace XRTK.WindowsMixedReality.Providers.Controllers
//{
//    public class WindowsMixedRealityGestureDataProvider
//    {
//        public WindowsMixedRealityGestureDataProvider(WindowsMixedRealityControllerDataProviderProfile profile)
//        {
//            if (MixedRealityToolkit.TryGetSystemProfile<IMixedRealityInputSystem, MixedRealityInputSystemProfile>(out var inputSystemProfile))
//            {
//                gestures = inputSystemProfile.GesturesProfile.Gestures;
//            }

//            gestureAutoStartBehavior = profile.WindowsGestureAutoStart;
//            GestureSettings = profile.ManipulationGestures;
//            NavigationSettings = profile.NavigationGestures;
//            RailsNavigationSettings = profile.RailsNavigationGestures;
//            UseRailsNavigation = profile.UseRailsNavigation;
//            gestureRecognizer = new GestureRecognizer();
//            navigationGestureRecognizer = new GestureRecognizer();
//        }

//        private readonly AutoStartBehavior gestureAutoStartBehavior;
//        private readonly MixedRealityGestureMapping[] gestures;

//        private MixedRealityInputAction tapAction = MixedRealityInputAction.None;
//        private MixedRealityInputAction doubleTapAction = MixedRealityInputAction.None;
//        private MixedRealityInputAction holdAction = MixedRealityInputAction.None;
//        private MixedRealityInputAction navigationAction = MixedRealityInputAction.None;
//        private MixedRealityInputAction manipulationAction = MixedRealityInputAction.None;

//        private static bool gestureRecognizerEnabled;

//        /// <summary>
//        /// Enables or disables the gesture recognizer.
//        /// </summary>
//        /// <remarks>
//        /// Automatically disabled navigation recognizer if enabled.
//        /// </remarks>
//        public static bool GestureRecognizerEnabled
//        {
//            get => gestureRecognizerEnabled;
//            set
//            {
//                gestureRecognizerEnabled = value;
//                if (!Application.isPlaying) { return; }

//                if (!gestureRecognizer.IsCapturingGestures() && gestureRecognizerEnabled)
//                {
//                    NavigationRecognizerEnabled = false;
//                    gestureRecognizer.StartCapturingGestures();
//                }

//                if (gestureRecognizer.IsCapturingGestures() && !gestureRecognizerEnabled)
//                {
//                    gestureRecognizer.CancelGestures();
//                }
//            }
//        }

//        private static bool navigationRecognizerEnabled;

//        /// <summary>
//        /// Enables or disables the navigation recognizer.
//        /// </summary>
//        /// <remarks>
//        /// Automatically disables the gesture recognizer if enabled.
//        /// </remarks>
//        public static bool NavigationRecognizerEnabled
//        {
//            get => navigationRecognizerEnabled;
//            set
//            {
//                navigationRecognizerEnabled = value;

//                if (!Application.isPlaying) { return; }

//                if (!navigationGestureRecognizer.IsCapturingGestures() && navigationRecognizerEnabled)
//                {
//                    GestureRecognizerEnabled = false;
//                    navigationGestureRecognizer.StartCapturingGestures();
//                }

//                if (navigationGestureRecognizer.IsCapturingGestures() && !navigationRecognizerEnabled)
//                {
//                    navigationGestureRecognizer.CancelGestures();
//                }
//            }
//        }

//        private static WindowsGestureSettings gestureSettings = WindowsGestureSettings.Hold | WindowsGestureSettings.ManipulationTranslate;

//        /// <summary>
//        /// Current Gesture Settings for the GestureRecognizer
//        /// </summary>
//        public static WindowsGestureSettings GestureSettings
//        {
//            get => gestureSettings;
//            set
//            {
//                gestureSettings = value;

//                if (Application.isPlaying)
//                {
//                    gestureRecognizer.UpdateAndResetGestures(WSAGestureSettings);
//                }
//            }
//        }

//        private static WindowsGestureSettings navigationSettings = WindowsGestureSettings.NavigationX | WindowsGestureSettings.NavigationY | WindowsGestureSettings.NavigationZ;

//        /// <summary>
//        /// Current Navigation Gesture Recognizer Settings.
//        /// </summary>
//        public static WindowsGestureSettings NavigationSettings
//        {
//            get => navigationSettings;
//            set
//            {
//                navigationSettings = value;

//                if (Application.isPlaying)
//                {
//                    navigationGestureRecognizer.UpdateAndResetGestures(WSANavigationSettings);
//                }
//            }
//        }

//        private static WindowsGestureSettings railsNavigationSettings = WindowsGestureSettings.NavigationRailsX | WindowsGestureSettings.NavigationRailsY | WindowsGestureSettings.NavigationRailsZ;

//        /// <summary>
//        /// Current Navigation Gesture Recognizer Rails Settings.
//        /// </summary>
//        public static WindowsGestureSettings RailsNavigationSettings
//        {
//            get => railsNavigationSettings;
//            set
//            {
//                railsNavigationSettings = value;

//                if (Application.isPlaying)
//                {
//                    navigationGestureRecognizer.UpdateAndResetGestures(WSARailsNavigationSettings);
//                }
//            }
//        }

//        private static bool useRailsNavigation = true;

//        /// <summary>
//        /// Should the Navigation Gesture Recognizer use Rails?
//        /// </summary>
//        public static bool UseRailsNavigation
//        {
//            get => useRailsNavigation;
//            set
//            {
//                useRailsNavigation = value;

//                if (Application.isPlaying)
//                {
//                    navigationGestureRecognizer.UpdateAndResetGestures(useRailsNavigation ? WSANavigationSettings : WSARailsNavigationSettings);
//                }
//            }
//        }

//        private static GestureRecognizer gestureRecognizer;
//        private static WsaGestureSettings WSAGestureSettings => (WsaGestureSettings)gestureSettings;

//        private static GestureRecognizer navigationGestureRecognizer;
//        private static WsaGestureSettings WSANavigationSettings => (WsaGestureSettings)navigationSettings;
//        private static WsaGestureSettings WSARailsNavigationSettings => (WsaGestureSettings)railsNavigationSettings;

//        public void Enable()
//        {
//            gestureRecognizer.Tapped += GestureRecognizer_Tapped;
//            gestureRecognizer.HoldStarted += GestureRecognizer_HoldStarted;
//            gestureRecognizer.HoldCompleted += GestureRecognizer_HoldCompleted;
//            gestureRecognizer.HoldCanceled += GestureRecognizer_HoldCanceled;

//            gestureRecognizer.ManipulationStarted += GestureRecognizer_ManipulationStarted;
//            gestureRecognizer.ManipulationUpdated += GestureRecognizer_ManipulationUpdated;
//            gestureRecognizer.ManipulationCompleted += GestureRecognizer_ManipulationCompleted;
//            gestureRecognizer.ManipulationCanceled += GestureRecognizer_ManipulationCanceled;

//            navigationGestureRecognizer.NavigationStarted += NavigationGestureRecognizer_NavigationStarted;
//            navigationGestureRecognizer.NavigationUpdated += NavigationGestureRecognizer_NavigationUpdated;
//            navigationGestureRecognizer.NavigationCompleted += NavigationGestureRecognizer_NavigationCompleted;
//            navigationGestureRecognizer.NavigationCanceled += NavigationGestureRecognizer_NavigationCanceled;

//            for (int i = 0; i < gestures?.Length; i++)
//            {
//                var gesture = gestures[i];

//                switch (gesture.GestureType)
//                {
//                    case GestureInputType.Hold:
//                        holdAction = gesture.Action;
//                        break;
//                    case GestureInputType.Manipulation:
//                        manipulationAction = gesture.Action;
//                        break;
//                    case GestureInputType.Navigation:
//                        navigationAction = gesture.Action;
//                        break;
//                    case GestureInputType.Tap:
//                        tapAction = gesture.Action;
//                        break;
//                    case GestureInputType.DoubleTap:
//                        doubleTapAction = gesture.Action;
//                        break;
//                }
//            }

//            if (gestures != null &&
//                gestureAutoStartBehavior == AutoStartBehavior.AutoStart)
//            {
//                GestureRecognizerEnabled = true;
//            }
//        }

//        public void Disable()
//        {
//            gestureRecognizer.Tapped -= GestureRecognizer_Tapped;
//            gestureRecognizer.HoldStarted -= GestureRecognizer_HoldStarted;
//            gestureRecognizer.HoldCompleted -= GestureRecognizer_HoldCompleted;
//            gestureRecognizer.HoldCanceled -= GestureRecognizer_HoldCanceled;

//            gestureRecognizer.ManipulationStarted -= GestureRecognizer_ManipulationStarted;
//            gestureRecognizer.ManipulationUpdated -= GestureRecognizer_ManipulationUpdated;
//            gestureRecognizer.ManipulationCompleted -= GestureRecognizer_ManipulationCompleted;
//            gestureRecognizer.ManipulationCanceled -= GestureRecognizer_ManipulationCanceled;

//            navigationGestureRecognizer.NavigationStarted -= NavigationGestureRecognizer_NavigationStarted;
//            navigationGestureRecognizer.NavigationUpdated -= NavigationGestureRecognizer_NavigationUpdated;
//            navigationGestureRecognizer.NavigationCompleted -= NavigationGestureRecognizer_NavigationCompleted;
//            navigationGestureRecognizer.NavigationCanceled -= NavigationGestureRecognizer_NavigationCanceled;
//        }

//        public void OnDispose(bool finalizing)
//        {
//            navigationGestureRecognizer.Dispose();
//            gestureRecognizer.Dispose();
//        }

//        private void GestureRecognizer_Tapped(TappedEventArgs args)
//        {
//            var controller = GetController(args.source);

//            if (controller != null)
//            {
//                if (args.tapCount == 1)
//                {
//                    InputSystem?.RaiseGestureStarted(controller, tapAction);
//                    InputSystem?.RaiseGestureCompleted(controller, tapAction);
//                }
//                else if (args.tapCount == 2)
//                {
//                    InputSystem?.RaiseGestureStarted(controller, doubleTapAction);
//                    InputSystem?.RaiseGestureCompleted(controller, doubleTapAction);
//                }
//            }
//        }

//        private void GestureRecognizer_HoldStarted(HoldStartedEventArgs args)
//        {
//            var controller = GetController(args.source);
//            if (controller != null)
//            {
//                InputSystem?.RaiseGestureStarted(controller, holdAction);
//            }
//        }

//        private void GestureRecognizer_HoldCompleted(HoldCompletedEventArgs args)
//        {
//            var controller = GetController(args.source);
//            if (controller != null)
//            {
//                InputSystem.RaiseGestureCompleted(controller, holdAction);
//            }
//        }

//        private void GestureRecognizer_HoldCanceled(HoldCanceledEventArgs args)
//        {
//            var controller = GetController(args.source);
//            if (controller != null)
//            {
//                InputSystem.RaiseGestureCanceled(controller, holdAction);
//            }
//        }

//        private void GestureRecognizer_ManipulationStarted(SpatialManipulationStartedEventArgs args)
//        {
//            var controller = GetController(args.source);
//            if (controller != null)
//            {
//                InputSystem.RaiseGestureStarted(controller, manipulationAction);
//            }
//        }

//        private void GestureRecognizer_ManipulationUpdated(SpatialManipulationUpdatedEventArgs args)
//        {
//            var controller = GetController(args.source);
//            if (controller != null)
//            {
//                InputSystem.RaiseGestureUpdated(controller, manipulationAction, args.cumulativeDelta);
//            }
//        }

//        private void GestureRecognizer_ManipulationCompleted(SpatialManipulationCompletedEventArgs args)
//        {
//            var controller = GetController(args.source);
//            if (controller != null)
//            {
//                InputSystem.RaiseGestureCompleted(controller, manipulationAction, args.cumulativeDelta);
//            }
//        }

//        private void GestureRecognizer_ManipulationCanceled(SpatialManipulationCanceledEventArgs args)
//        {
//            var controller = GetController(args.source);
//            if (controller != null)
//            {
//                InputSystem.RaiseGestureCanceled(controller, manipulationAction);
//            }
//        }

//        private void NavigationGestureRecognizer_NavigationStarted(SpatialNavigationStartedEventArgs args)
//        {
//            var controller = GetController(args.source);
//            if (controller != null)
//            {
//                InputSystem.RaiseGestureStarted(controller, navigationAction);
//            }
//        }

//        private void NavigationGestureRecognizer_NavigationUpdated(SpatialNavigationUpdatedEventArgs args)
//        {
//            var controller = GetController(args.source);
//            if (controller != null)
//            {
//                InputSystem.RaiseGestureUpdated(controller, navigationAction, args.normalizedOffset);
//            }
//        }

//        private void NavigationGestureRecognizer_NavigationCompleted(SpatialNavigationCompletedEventArgs args)
//        {
//            var controller = GetController(args.source);
//            if (controller != null)
//            {
//                InputSystem.RaiseGestureCompleted(controller, navigationAction, args.normalizedOffset);
//            }
//        }

//        private void NavigationGestureRecognizer_NavigationCanceled(SpatialNavigationCanceledEventArgs args)
//        {
//            var controller = GetController(args);
//            if (controller != null)
//            {
//                InputSystem.RaiseGestureCanceled(controller, navigationAction);
//            }
//        }
//    }
//}

//#endif // WINDOWS_UWP