// Copyright (c) XRTK. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace XRTK.Editor.BuildPipeline
{
    [CustomEditor(typeof(UwpBuildInfo))]
    public class UwpBuildInfoInspector : BuildInfoInspector
    {
        private enum Architecture
        {
            x86 = 0,
            x64 = 1,
            ARM = 2,
            ARM64 = 3,
        }

        private SerializedProperty buildAppx;
        private SerializedProperty rebuildAppx;
        private List<Version> uwpSdkVersions = new List<Version>();

        private static bool IsValidSdkInstalled { get; set; } = true;

        protected override void OnEnable()
        {
            base.OnEnable();

#if UNITY_EDITOR_WIN
            LoadUwpSdkPaths();
#endif

            buildAppx = serializedObject.FindProperty(nameof(buildAppx));
            rebuildAppx = serializedObject.FindProperty(nameof(rebuildAppx));
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            serializedObject.Update();

            EditorGUILayout.PropertyField(buildAppx);
            GUI.enabled = buildAppx.boolValue;
            EditorGUILayout.PropertyField(rebuildAppx);
            GUI.enabled = true;

            EditorGUILayout.BeginHorizontal();

            // Build config (and save setting, if it's changed)
            var curBuildConfigString = buildInfo.Configuration;

            WSABuildType buildConfigOption;

            if (curBuildConfigString.ToLower().Equals("master"))
            {
                buildConfigOption = WSABuildType.Master;
            }
            else if (curBuildConfigString.ToLower().Equals("release"))
            {
                buildConfigOption = WSABuildType.Release;
            }
            else
            {
                buildConfigOption = WSABuildType.Debug;
            }

            buildConfigOption = (WSABuildType)EditorGUILayout.EnumPopup("Build Configuration", buildConfigOption);

            var buildConfigString = buildConfigOption.ToString().ToLower();

            if (buildConfigString != curBuildConfigString)
            {
                buildInfo.Configuration = buildConfigString;
            }

            // Build Platform (and save setting, if it's changed)
            var currentArchitectureString = EditorUserBuildSettings.wsaArchitecture;

            var buildArchitecture = Architecture.x86;

            if (currentArchitectureString.ToLower().Equals("x86"))
            {
                buildArchitecture = Architecture.x86;
            }
            else if (currentArchitectureString.ToLower().Equals("x64"))
            {
                buildArchitecture = Architecture.x64;
            }
            else if (currentArchitectureString.ToLower().Equals("arm"))
            {
                buildArchitecture = Architecture.ARM;
            }
            else if (currentArchitectureString.ToLower().Equals("arm64"))
            {
                buildArchitecture = Architecture.ARM64;
            }

            buildArchitecture = (Architecture)EditorGUILayout.EnumPopup("Build Platform", buildArchitecture);

            string newBuildArchitectureString = buildArchitecture.ToString();

            if (newBuildArchitectureString != currentArchitectureString)
            {
                EditorUserBuildSettings.wsaArchitecture = newBuildArchitectureString;
            }

            EditorGUILayout.EndHorizontal();

            ValidateUwpSdk();

            if (GUILayout.Button("Open in Visual Studio"))
            {
                // Open SLN
                var slnFilename = Path.Combine(BuildDeployPreferences.BuildDirectory, $"{PlayerSettings.productName}\\{PlayerSettings.productName}.sln");

                if (File.Exists(slnFilename))
                {
                    EditorApplication.delayCall += () => Process.Start(new FileInfo(slnFilename).FullName);
                }
                else if (EditorUtility.DisplayDialog(
                    "Solution Not Found",
                    "We couldn't find the Project's Solution. Would you like to Build the project now?",
                    "Yes, Build", "No"))
                {
                    EditorApplication.delayCall += () => UnityPlayerBuildTools.BuildUnityPlayer();
                }
            }

            // Open AppX packages location
            var appxDirectory = $"\\{PlayerSettings.productName}\\AppPackages";
            var appxBuildPath = Path.GetFullPath($"{BuildDeployPreferences.BuildDirectory}{appxDirectory}");
            GUI.enabled = !string.IsNullOrEmpty(appxBuildPath) && Directory.Exists(appxBuildPath);

            if (GUILayout.Button("Open APPX Packages Location"))
            {
                EditorApplication.delayCall += () => Process.Start("explorer.exe", $"/f /open,{appxBuildPath}");
            }

            GUI.enabled = true;

            if (GUILayout.Button("Build APPX"))
            {
                // Check if solution exists
                var slnFilename = Path.Combine(BuildDeployPreferences.BuildDirectory, $"{PlayerSettings.productName}\\{PlayerSettings.productName}.sln");

                if (File.Exists(slnFilename))
                {
                    EditorApplication.delayCall += () => UwpAppxBuildTools.BuildAppx(target as UwpBuildInfo);
                }
                else if (EditorUtility.DisplayDialog("Solution Not Found", "We couldn't find the solution. Would you like to Build it?", "Yes, Build", "No"))
                {
                    EditorApplication.delayCall += () => UnityPlayerBuildTools.BuildUnityPlayer();
                }
            }

            serializedObject.ApplyModifiedProperties();
        }

        private void LoadUwpSdkPaths()
        {
            var windowsSdkPaths = Directory.GetDirectories(@"C:\Program Files (x86)\Windows Kits\10\Lib");

            foreach (var path in windowsSdkPaths)
            {
                uwpSdkVersions.Add(new Version(path.Substring(path.LastIndexOf(@"\", StringComparison.Ordinal) + 1)));
            }

            // There is no well-defined enumeration of Directory.GetDirectories, so the list
            // is sorted prior to use later in this class.
            uwpSdkVersions.Sort();
        }

        private void ValidateUwpSdk()
        {
            // SDK and MS Build Version (and save setting, if it's changed)
            string currentSDKVersion = EditorUserBuildSettings.wsaMinUWPSDK;

            Version chosenSDKVersion = null;

            for (var i = 0; i < uwpSdkVersions.Count; i++)
            {
                // windowsSdkVersions is sorted in ascending order, so we always take
                // the highest SDK version that is above our minimum.
                if (uwpSdkVersions[i] >= UwpBuildDeployPreferences.MIN_SDK_VERSION)
                {
                    chosenSDKVersion = uwpSdkVersions[i];
                }
            }

            EditorGUILayout.HelpBox($"Minimum Required SDK Version: {currentSDKVersion}", MessageType.Info);

            // Throw exception if user has no Windows 10 SDK installed
            if (chosenSDKVersion == null)
            {
                if (IsValidSdkInstalled)
                {
                    Debug.LogError($"Unable to find the required Windows 10 SDK Target!\nPlease be sure to install the {UwpBuildDeployPreferences.MIN_SDK_VERSION} SDK from Visual Studio Installer.");
                }

                EditorGUILayout.HelpBox($"Unable to find the required Windows 10 SDK Target!\nPlease be sure to install the {UwpBuildDeployPreferences.MIN_SDK_VERSION} SDK from Visual Studio Installer.", MessageType.Error);
                GUILayout.EndVertical();
                IsValidSdkInstalled = false;
                return;
            }

            IsValidSdkInstalled = true;
            var newSdkVersion = chosenSDKVersion.ToString();

            if (!newSdkVersion.Equals(currentSDKVersion))
            {
                EditorUserBuildSettings.wsaMinUWPSDK = newSdkVersion;
            }
        }
    }
}
