// Copyright (c) XRTK. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.IO;
using UnityEditor;
using XRTK.Editor;
using XRTK.Extensions;
using XRTK.Editor.Utilities;

namespace XRTK.WindowsMixedReality.Editor
{
    [InitializeOnLoad]
    internal static class WindowsMixedRealityPackageInstaller
    {
        private static readonly string DefaultPath = $"{MixedRealityPreferences.ProfileGenerationPath}WindowsMixedReality";
        private static readonly string HiddenPath = Path.GetFullPath($"{PathFinderUtility.ResolvePath<IPathFinder>(typeof(WindowsMixedRealityPathFinder)).BackSlashes()}{Path.DirectorySeparatorChar}{MixedRealityPreferences.HIDDEN_PROFILES_PATH}");

        static WindowsMixedRealityPackageInstaller()
        {
            EditorApplication.delayCall += CheckPackage;
        }

        [MenuItem("Mixed Reality Toolkit/Packages/Install Windows Mixed Realty Package Assets...", true)]
        private static bool ImportPackageAssetsValidation()
        {
            return !Directory.Exists($"{DefaultPath}{Path.DirectorySeparatorChar}Profiles");
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
                EditorPreferences.Set($"{nameof(WindowsMixedRealityPackageInstaller)}.Profiles", PackageInstaller.TryInstallAssets(HiddenPath, $"{DefaultPath}{Path.DirectorySeparatorChar}Profiles"));
            }
        }
    }
}