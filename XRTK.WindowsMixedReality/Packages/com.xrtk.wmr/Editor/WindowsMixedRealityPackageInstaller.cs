// Copyright (c) XRTK. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.IO;
using UnityEditor;
using XRTK.Editor;
using XRTK.Extensions;
using XRTK.Editor.Utilities;
using UnityEditor.PackageManager.Requests;
using UnityEditor.PackageManager;
using UnityEngine;

namespace XRTK.WindowsMixedReality.Editor
{
    [InitializeOnLoad]
    internal static class WindowsMixedRealityPackageInstaller
    {
        private static readonly string defaultPath = $"{MixedRealityPreferences.ProfileGenerationPath}WindowsMixedReality";
        private static readonly string hiddenPath = Path.GetFullPath($"{PathFinderUtility.ResolvePath<IPathFinder>(typeof(WindowsMixedRealityPathFinder)).ToForwardSlashes()}\\{MixedRealityPreferences.HIDDEN_PROFILES_PATH}");
        private static readonly string wmrDependencyCheckSettingsKey = $"{nameof(WindowsMixedRealityPackageInstaller)}.LastDependenciesCheckSessionId";
        private static readonly string sessionId = GUID.Generate().ToString();

#if UNITY_2020_1_OR_NEWER
        private static readonly string wmrDependencyPackageName = "com.unity.xr.windowsmr";
        private static readonly string wmrDependencyPackageVersion = "4.4.1";
#else
        private static readonly string wmrDependencyPackageName = "com.unity.xr.windowsmr.metro";
        private static readonly string wmrDependencyPackageVersion = "4.2.3";
#endif

        private static ListRequest listProjectPackagesRequest;

        static WindowsMixedRealityPackageInstaller()
        {
            EditorApplication.delayCall += CheckPackage;
            EditorApplication.update += CheckDependencies;
        }

        [MenuItem("Mixed Reality Toolkit/Packages/Install Windows Mixed Realty Package Assets...", true)]
        private static bool ImportPackageAssetsValidation()
        {
            return !Directory.Exists($"{defaultPath}\\Profiles");
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
                EditorPreferences.Set($"{nameof(WindowsMixedRealityPackageInstaller)}.Profiles", PackageInstaller.TryInstallAssets(hiddenPath, $"{defaultPath}\\Profiles"));
            }
        }

        /// <summary>
        /// Checks whether dependency packages are intalled depending on the project's Unity version.
        /// </summary>
        private static void CheckDependencies()
        {
            // If we haven't checked whether dependencies are set up correctly during this Unity session, check now.
            if (!string.Equals(EditorPreferences.Get(wmrDependencyCheckSettingsKey, string.Empty), sessionId))
            {
                if (listProjectPackagesRequest == null)
                {
                    listProjectPackagesRequest = Client.List(true, true);
                }
                else if (listProjectPackagesRequest != null && !listProjectPackagesRequest.IsCompleted)
                {
                    return;
                }
                else if (listProjectPackagesRequest.Status == StatusCode.Success)
                {
                    var dependenciesInstalled = false;
                    foreach (var packageInfo in listProjectPackagesRequest.Result)
                    {
                        switch (packageInfo.name)
                        {
                            case string _ when packageInfo.name.Equals(wmrDependencyPackageName):
                                dependenciesInstalled = true;
                                break;
                        }

                        if (dependenciesInstalled)
                        {
                            break;
                        }
                    }

                    if (!dependenciesInstalled)
                    {
                        Debug.Log($"{nameof(WindowsMixedRealityPackageInstaller)} added dependency package {wmrDependencyPackageName}@{wmrDependencyPackageVersion} to project.");
                        Client.Add($"{wmrDependencyPackageName}@{wmrDependencyPackageVersion}");
                    }

                    EditorPreferences.Set(wmrDependencyCheckSettingsKey, sessionId);
                    EditorApplication.update -= CheckDependencies;
                }
                else
                {
                    Debug.LogError($"{nameof(WindowsMixedRealityPackageInstaller)} couldn't read project packages to update XRTK dependencies.");
                    listProjectPackagesRequest = null;
                }
            }
        }
    }
}
