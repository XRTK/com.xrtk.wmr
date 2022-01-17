// Copyright (c) XRTK. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

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
        private SerializedProperty buildAppx;
        private SerializedProperty rebuildAppx;
        private SerializedProperty platformArchitecture;
        private SerializedProperty verbosity;

        protected override void OnEnable()
        {
            base.OnEnable();

            buildAppx = serializedObject.FindProperty(nameof(buildAppx));
            rebuildAppx = serializedObject.FindProperty(nameof(rebuildAppx));
            platformArchitecture = serializedObject.FindProperty(nameof(platformArchitecture));
            verbosity = serializedObject.FindProperty(nameof(verbosity));
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            serializedObject.Update();

            if (!(buildInfo is UwpBuildInfo uwpBuildInfo))
            {
                Debug.LogError($"{buildInfo.name} is not a {nameof(UwpBuildInfo)}");
                return;
            }

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

            EditorGUILayout.PropertyField(platformArchitecture);
            EditorGUILayout.PropertyField(verbosity);
            EditorGUILayout.EndHorizontal();

            UwpAppxBuildTools.ValidateUwpSdk(true);

            // Build Appx
            if (GUILayout.Button("Build APPX"))
            {
                // Check if solution exists
                var slnFilename = Path.Combine(uwpBuildInfo.OutputDirectory, uwpBuildInfo.SolutionPath);

                if (File.Exists(slnFilename))
                {
                    EditorApplication.delayCall += () => UwpAppxBuildTools.BuildAppx(target as UwpBuildInfo);
                }
                else if (EditorUtility.DisplayDialog("Solution Not Found", "We couldn't find the solution. Would you like to Build it?", "Yes, Build", "No"))
                {
                    EditorApplication.delayCall += () => UnityPlayerBuildTools.BuildUnityPlayer();
                }
            }

            // Open Appx Solution
            if (GUILayout.Button("Open APPX solution in Visual Studio"))
            {
                var slnFilename = Path.Combine(uwpBuildInfo.OutputDirectory, uwpBuildInfo.SolutionPath);

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

            serializedObject.ApplyModifiedProperties();
        }
    }
}
