// Copyright (c) XRTK. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using UnityEngine;
using XRTK.Attributes;
using XRTK.Definitions.InputSystem;
using XRTK.Definitions.Utilities;
using XRTK.Definitions.Controllers;
using XRTK.Providers.Controllers.Hands;
using XRTK.Interfaces.InputSystem.Controllers.Hands;

namespace XRTK.WindowsMixedReality.Profiles
{
    [CreateAssetMenu(menuName = "Mixed Reality Toolkit/Input System/Controller Data Providers/Windows Mixed Reality Controller Data Provider Profile", fileName = "WindowsMixedRealityControllerDataProviderProfile", order = (int)CreateProfileMenuItemIndices.Input)]
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

        #region Global Hand Settings Overrides

        [Header("General Settings")]

        [SerializeField]
        [Tooltip("If set, hand mesh data will be read and available for visualization. Disable for optimized performance.")]
        private bool handMeshingEnabled = false;

        /// <summary>
        /// If set, hand mesh data will be read and available for visualization. Disable for optimized performance.
        /// </summary>
        public bool HandMeshingEnabled => handMeshingEnabled;

        [SerializeField]
        [Tooltip("The hand ray concrete type to use when raycasting for hand interaction.")]
        [Implements(typeof(IMixedRealityHandRay), TypeGrouping.ByNamespaceFlat)]
        private SystemType handRayType = null;

        /// <summary>
        /// The hand ray concrete type to use when raycasting for hand interaction.
        /// </summary>
        public SystemType HandRayType => handRayType;

        [Header("Hand Physics")]

        [SerializeField]
        [Tooltip("If set, hands will be setup with colliders and a rigidbody to work with Unity's physics system.")]
        private bool handPhysicsEnabled = false;

        /// <summary>
        /// If set, hands will be setup with colliders and a rigidbody to work with Unity's physics system.
        /// </summary>
        public bool HandPhysicsEnabled => handPhysicsEnabled;

        [SerializeField]
        [Tooltip("If set, hand colliders will be setup as triggers.")]
        private bool useTriggers = false;

        /// <summary>
        /// If set, hand colliders will be setup as triggers.
        /// </summary>
        public bool UseTriggers => useTriggers;

        [SerializeField]
        [Tooltip("Set the bounds mode to use for calculating hand bounds.")]
        private HandBoundsMode boundsMode = HandBoundsMode.Hand;

        /// <summary>
        /// Set the bounds mode to use for calculating hand bounds.
        /// </summary>
        public HandBoundsMode BoundsMode => boundsMode;

        #endregion Global Hand Settings Overrides
    }
}
