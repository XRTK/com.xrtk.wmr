// Copyright (c) XRTK. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Collections.Generic;
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
        private static readonly string DefaultPath = $"{MixedRealityPreferences.ProfileGenerationPath}WindowsMixedReality";
        private static readonly string HiddenProfilePath = Path.GetFullPath($"{PathFinderUtility.ResolvePath<IPathFinder>(typeof(WindowsMixedRealityPathFinder)).ToForwardSlashes()}\\{MixedRealityPreferences.HIDDEN_PROFILES_PATH}");
        private static readonly string HiddenPrefabPath = Path.GetFullPath($"{PathFinderUtility.ResolvePath<IPathFinder>(typeof(WindowsMixedRealityPathFinder)).ToForwardSlashes()}\\{MixedRealityPreferences.HIDDEN_PREFABS_PATH}");
        private static readonly Dictionary<string, string> DefaultWMRAssets = new Dictionary<string, string>
        {
            {HiddenProfilePath,  $"{DefaultPath}\\Profiles"},
            {HiddenPrefabPath, $"{DefaultPath}\\Prefabs"}
        };

        static WindowsMixedRealityPackageInstaller()
        {
            EditorApplication.delayCall += CheckPackage;
        }

        [MenuItem("Mixed Reality Toolkit/Packages/Install Windows Mixed Realty Package Assets...", true)]
        private static bool ImportPackageAssetsValidation()
        {
            return !Directory.Exists($"{DefaultPath}\\Profiles") || !Directory.Exists($"{DefaultPath}\\Prefabs");
        }

        [MenuItem("Mixed Reality Toolkit/Packages/Install Windows Mixed Realty Package Assets...")]
        private static void ImportPackageAssets()
        {
            EditorPreferences.Set($"{nameof(WindowsMixedRealityPackageInstaller)}.Profiles", false);
            EditorApplication.delayCall += CheckPackage;
        }

        private static void CheckPackage()
        {
            if (!EditorPreferences.Get($"{nameof(WindowsMixedRealityPackageInstaller)}.Profiles", false))
            {
                EditorPreferences.Set($"{nameof(WindowsMixedRealityPackageInstaller)}.Profiles", PackageInstaller.TryInstallAssets(DefaultWMRAssets));
            }
        }
    }
}
