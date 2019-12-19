// Copyright (c) XRTK. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using UnityEngine;
using XRTK.Definitions.Controllers;
using XRTK.Definitions.Utilities;

namespace XRTK.WindowsMixedReality.Profiles
{
    [CreateAssetMenu(menuName = "Mixed Reality Toolkit/Input System/Controller Data Providers/Hands/Windows Mixed Reality Hand Controller Data Provider Profile", fileName = "WindowsMixedRealityHandControllerDataProviderProfile", order = (int)CreateProfileMenuItemIndices.Input)]
    public class WindowsMixedRealityHandControllerDataProviderProfile : BaseMixedRealityControllerDataProviderProfile
    {
        [Header("Hand Tracking")]
        [SerializeField]
        [Tooltip("Enable hand tracking")]
        private bool handTrackingEnabled = true;

        /// <summary>
        /// Is hand tracking enabled?
        /// </summary>
        public bool HandTrackingEnabled => handTrackingEnabled;
    }
}