// Copyright (c) XRTK. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using UnityEngine;
using XRTK.Definitions.Controllers;
using XRTK.Definitions.Devices;
using XRTK.Definitions.Utilities;
using XRTK.Providers.Controllers;
using XRTK.WindowsMixedReality.Controllers;

namespace XRTK.WindowsMixedReality.Profiles
{
    [CreateAssetMenu(menuName = "Mixed Reality Toolkit/Input System/Controller Mappings/Windows Mixed Reality Hand Controller Mapping Profile", fileName = "WindowsMixedRealityHandControllerMappingProfile")]
    public class WindowsMixedRealityHandControllerMappingProfile : BaseMixedRealityControllerMappingProfile
    {
        /// <inheritdoc />
        public override SupportedControllerType ControllerType => SupportedControllerType.Hand;

        /// <inheritdoc />
        public override string TexturePath => $"{base.TexturePath}Hand";

        protected override void Awake()
        {
            if (!HasSetupDefaults)
            {
                ControllerMappings = new[]
                {
                    new MixedRealityControllerMapping("Windows Mixed Reality Hand Controller Left", typeof(WindowsMixedRealityHandController), Handedness.Left),
                    new MixedRealityControllerMapping("Windows Mixed Reality Hand Controller Right", typeof(WindowsMixedRealityHandController), Handedness.Right)
                };
            }

            base.Awake();
        }
    }
}