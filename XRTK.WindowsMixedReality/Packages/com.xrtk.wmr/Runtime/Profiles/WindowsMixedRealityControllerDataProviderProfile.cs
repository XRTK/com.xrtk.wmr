// Copyright (c) XRTK. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using UnityEngine;
using XRTK.Attributes;
using XRTK.Definitions.Utilities;
using XRTK.Definitions.Controllers;
using XRTK.WindowsMixedReality.Providers.Controllers;
using XRTK.WindowsMixedReality.Definitions;

namespace XRTK.WindowsMixedReality.Profiles
{
    /// <summary>
    /// Configuration profile for the <see cref="WindowsMixedRealityControllerDataProvider"/>. This profile
    /// offers settings for adjusting <see cref="Interfaces.Providers.Controllers.IMixedRealityController"/> behaviour on the
    /// <see cref="XRTK.Definitions.Platforms.UniversalWindowsPlatform"/>.
    /// </summary>
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

        public override ControllerDefinition[] GetDefaultControllerOptions()
        {
            return new[]
               {
                 new ControllerDefinition(typeof(HololensOneController), Handedness.Left),
                 new ControllerDefinition(typeof(HololensOneController), Handedness.Right),
                 new ControllerDefinition(typeof(WindowsMixedRealityMotionController), Handedness.Left),
                 new ControllerDefinition(typeof(WindowsMixedRealityMotionController), Handedness.Right),
            };
        }
    }
}