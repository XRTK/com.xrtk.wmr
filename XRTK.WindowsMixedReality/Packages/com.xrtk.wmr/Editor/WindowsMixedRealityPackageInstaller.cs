// Copyright (c) XRTK. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.IO;
using UnityEditor;
using XRTK.Editor;
using XRTK.Extensions;
using XRTK.Utilities.Editor;

namespace XRTK.WindowsMixedReality.Editor
{
    [InitializeOnLoad]
    internal static class WindowsMixedRealityPackageInstaller
    {
        private static readonly string DefaultPath = $"{MixedRealityPreferences.ProfileGenerationPath}WindowsMixedReality\\";
        private static readonly string HiddenPath = Path.GetFullPath($"{PathFinderUtility.ResolvePath<IPathFinder>(typeof(WindowsMixedRealityPathFinder)).ToForwardSlashes()}\\{MixedRealityPreferences.HIDDEN_PROFILES_PATH}");

        static WindowsMixedRealityPackageInstaller()
        {
            if (!EditorPreferences.Get($"{nameof(WindowsMixedRealityPackageInstaller)}", false))
            {
                EditorPreferences.Set($"{nameof(WindowsMixedRealityPackageInstaller)}", PackageInstaller.TryInstallProfiles(HiddenPath, DefaultPath));
            }
        }
    }
}
