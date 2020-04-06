// Copyright (c) XRTK. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using UnityEngine;
using XRTK.Attributes;
using XRTK.Definitions.InputSystem;
using XRTK.Definitions.Utilities;
using XRTK.Definitions.Controllers;

namespace XRTK.WindowsMixedReality.Profiles
{
    [CreateAssetMenu(menuName = "Mixed Reality Toolkit/Input System/Controller Data Providers/Windows Mixed Reality", fileName = "WindowsMixedRealityControllerDataProviderProfile", order = (int)CreateProfileMenuItemIndices.Input)]
    public class WindowsMixedRealityControllerDataProviderProfile : BaseMixedRealityControllerDataProviderProfile
    {
        [EnumFlags]
        [SerializeField]
        [Tooltip("The recognizable Manipulation Gestures.")]
        private WindowsGestureSettings manipulationGestures = 0;

        /// <summary>
        /// The recognizable Manipulation Gestures.
        /// </summary>
        public WindowsGestureSettings ManipulationGestures => manipulationGestures;

        [EnumFlags]
        [SerializeField]
        [Tooltip("The recognizable Navigation Gestures.")]
        private WindowsGestureSettings navigationGestures = 0;

        /// <summary>
        /// The recognizable Navigation Gestures.
        /// </summary>
        public WindowsGestureSettings NavigationGestures => navigationGestures;

        [SerializeField]
        [Tooltip("Should the Navigation use Rails on start?\nNote: This can be changed at runtime to switch between the two Navigation settings.")]
        private bool useRailsNavigation = false;

        public bool UseRailsNavigation => useRailsNavigation;

        [EnumFlags]
        [SerializeField]
        [Tooltip("The recognizable Rails Navigation Gestures.")]
        private WindowsGestureSettings railsNavigationGestures = 0;

        /// <summary>
        /// The recognizable Navigation Gestures.
        /// </summary>
        public WindowsGestureSettings RailsNavigationGestures => railsNavigationGestures;

        [SerializeField]
        private AutoStartBehavior windowsGestureAutoStart = AutoStartBehavior.AutoStart;

        public AutoStartBehavior WindowsGestureAutoStart => windowsGestureAutoStart;

        public override ControllerDefinition[] GetControllerDefinitions()
        {
            // new MixedRealityControllerMapping("Windows Mixed Reality HoloLens Hand Input", typeof(WindowsMixedRealityController)),
            // new MixedRealityControllerMapping("Windows Mixed Reality Motion Controller Left", typeof(WindowsMixedRealityController), Handedness.Left),
            // new MixedRealityControllerMapping("Windows Mixed Reality Motion Controller Right", typeof(WindowsMixedRealityController), Handedness.Right),
            // new MixedRealityControllerMapping("Open VR Motion Controller Left", typeof(WindowsMixedRealityOpenVRMotionController), Handedness.Left),
            // new MixedRealityControllerMapping("Open VR Motion Controller Right", typeof(WindowsMixedRealityOpenVRMotionController), Handedness.Right),
            throw new System.NotImplementedException();
        }
    }
}
