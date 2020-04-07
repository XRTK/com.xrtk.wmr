// Copyright (c) XRTK. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using UnityEngine;
using XRTK.Definitions.Controllers;
using XRTK.Definitions.Utilities;
using XRTK.Definitions.Controllers.Hands;

namespace XRTK.WindowsMixedReality.Profiles
{
    [CreateAssetMenu(menuName = "Mixed Reality Toolkit/Input System/Controller Data Providers/Windows Mixed Reality Hand", fileName = "WindowsMixedRealityHandControllerDataProviderProfile", order = (int)CreateProfileMenuItemIndices.Input)]
    public class WindowsMixedRealityHandControllerDataProviderProfile : BaseHandControllerDataProviderProfile
    {
        public override ControllerDefinition[] GetControllerDefinitions()
        {
            throw new System.NotImplementedException();
        }
    }
}