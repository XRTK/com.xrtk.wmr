// Copyright (c) XRTK. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using UnityEngine;
using XRTK.Definitions.CameraSystem;
using XRTK.Interfaces.CameraSystem;
using XRTK.Providers.CameraSystem;

namespace XRTK.WindowsMixedReality.Providers.CameraSystem
{
    public class WindowsMixedRealityCameraDataProvider : BaseCameraDataProvider
    {
        /// <inheritdoc />
        public WindowsMixedRealityCameraDataProvider(string name, uint priority, BaseMixedRealityCameraDataProviderProfile profile, IMixedRealityCameraSystem parentService)
            : base(name, priority, profile, parentService)
        {
        }

        /// <inheritdoc />
        public override bool IsOpaque
        {
            get
            {
#if UNITY_WSA
                return UnityEngine.XR.WSA.HolographicSettings.IsDisplayOpaque;
#else
                return base.IsOpaque;
#endif
            }
        }
    }
}