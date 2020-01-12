// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

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
using System;
using Windows.ApplicationModel.Core;
using Windows.Perception;
using Windows.Storage.Streams;
using Windows.UI.Input.Spatial;
using XRTK.Utilities;
#endif // WINDOWS_UWP

namespace XRTK.WindowsMixedReality.Controllers
{
    /// <summary>
    /// The device manager for Windows Mixed Reality hand controllers.
    /// </summary>
    public class WindowsMixedRealityHandControllerDataProvider : BaseControllerDataProvider
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="priority"></param>
        /// <param name="profile"></param>
        public WindowsMixedRealityHandControllerDataProvider(string name, uint priority, WindowsMixedRealityHandControllerDataProviderProfile profile)
            : base(name, priority, profile)
        {
            this.profile = profile;
        }

        private readonly WindowsMixedRealityHandControllerDataProviderProfile profile;
    }
}