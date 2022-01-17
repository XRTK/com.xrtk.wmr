﻿// Copyright (c) XRTK. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XRTK.Attributes;
using XRTK.Definitions.CameraSystem;
using XRTK.Definitions.Platforms;
using XRTK.Interfaces.CameraSystem;
using XRTK.Providers.CameraSystem;

namespace XRTK.WindowsMixedReality.Providers.CameraSystem
{
    [RuntimePlatform(typeof(UniversalWindowsPlatform))]
    [System.Runtime.InteropServices.Guid("0F33B864-E4B9-4697-AF40-F7772F3BC596")]
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
#if WINDOWS_UWP
                return Windows.Graphics.Holographic.HolographicDisplay.GetDefault().IsOpaque;
#else
                return base.IsOpaque;
#endif // WINDOWS_UWP
            }
        }
    }
}
