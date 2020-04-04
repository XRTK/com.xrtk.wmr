// Copyright (c) XRTK. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using UnityEngine;
using XRTK.Attributes;
using XRTK.Definitions.Utilities;
using XRTK.Definitions.Controllers;
using XRTK.Providers.Controllers.Hands;
using XRTK.Interfaces.InputSystem.Controllers.Hands;

namespace XRTK.WindowsMixedReality.Profiles
{
    [CreateAssetMenu(menuName = "Mixed Reality Toolkit/Input System/Controller Data Providers/Windows Mixed Reality Hand", fileName = "WindowsMixedRealityHandControllerDataProviderProfile", order = (int)CreateProfileMenuItemIndices.Input)]
    public class WindowsMixedRealityHandControllerDataProviderProfile : BaseMixedRealityControllerDataProviderProfile
    {
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
