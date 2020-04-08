// Copyright (c) XRTK. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using UnityEditor;
using XRTK.Inspectors.Profiles.InputSystem.Controllers;
using XRTK.WindowsMixedReality.Profiles;

namespace XRTK.WindowsMixedReality.Inspectors
{
    [CustomEditor(typeof(WindowsMixedRealityHandControllerDataProviderProfile))]
    public class WindowsMixedRealityHandControllerDataProviderProfileInspector : BaseMixedRealityHandControllerDataProviderProfileInspector
    {
        public override void OnInspectorGUI()
        {
            RenderHeader();

            base.OnInspectorGUI();
        }
    }
}