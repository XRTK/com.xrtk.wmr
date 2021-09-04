﻿// Copyright (c) XRTK. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using UnityEditor;
using UnityEngine;
using XRTK.Editor.Utilities;
using XRTK.Extensions;
using Debug = UnityEngine.Debug;

namespace XRTK.Editor.BuildPipeline
{
    public static class UwpAppxBuildTools
    {
        private static readonly XNamespace UapNameSpace = "http://schemas.microsoft.com/appx/manifest/uap/windows10";
        private static readonly XNamespace Uap5NameSpace = "http://schemas.microsoft.com/appx/manifest/uap/windows10/5";
        private static readonly List<Version> UWPSdkVersions = new List<Version>();

        private static float progress;
        private static CancellationTokenSource cancellationTokenSource;

        /// <summary>
        /// Query the build process to see if we're already building.
        /// </summary>
        public static bool IsBuilding { get; private set; } = false;

        public static bool ValidateUwpSdk(bool showInfo = false)
        {
            if (UWPSdkVersions.Count == 0)
            {
                var windowsSdkPaths = Directory.GetDirectories(@"C:\Program Files (x86)\Windows Kits\10\Lib");

                foreach (var path in windowsSdkPaths)
                {
                    UWPSdkVersions.Add(new Version(path.Substring(path.LastIndexOf(@"\", StringComparison.Ordinal) + 1)));
                }

                // There is no well-defined enumeration of Directory.GetDirectories, so the list
                // is sorted prior to use later in this class.
                UWPSdkVersions.Sort();
            }

            // SDK and MS Build Version (and save setting, if it's changed)
            var currentSDKVersion = EditorUserBuildSettings.wsaMinUWPSDK;

            Version chosenSDKVersion = null;

            for (var i = 0; i < UWPSdkVersions.Count; i++)
            {
                // windowsSdkVersions is sorted in ascending order, so we always take
                // the highest SDK version that is above our minimum.
                if (UWPSdkVersions[i] >= UwpBuildDeployPreferences.MIN_SDK_VERSION)
                {
                    chosenSDKVersion = UWPSdkVersions[i];
                }
            }

            if (showInfo)
            {
                EditorGUILayout.HelpBox($"Minimum Required SDK Version: {currentSDKVersion}", MessageType.Info);
            }

            // Throw exception if user has no Windows 10 SDK installed
            if (chosenSDKVersion == null)
            {
                Debug.LogError($"Unable to find the required Windows 10 SDK Target!\nPlease be sure to install the {UwpBuildDeployPreferences.MIN_SDK_VERSION} SDK from Visual Studio Installer.");

                if (showInfo)
                {
                    EditorGUILayout.HelpBox($"Unable to find the required Windows 10 SDK Target!\nPlease be sure to install the {UwpBuildDeployPreferences.MIN_SDK_VERSION} SDK from Visual Studio Installer.", MessageType.Error);
                }

                return false;
            }

            var newSdkVersion = chosenSDKVersion.ToString();

            if (!newSdkVersion.Equals(currentSDKVersion))
            {
                EditorUserBuildSettings.wsaMinUWPSDK = newSdkVersion;
            }

            return true;
        }

        /// <summary>
        /// Build the UWP appx bundle for this project.
        /// </summary>
        /// <param name="buildInfo"></param>
        /// <returns>True, if the appx build was successful.</returns>
        public static void BuildAppx(UwpBuildInfo buildInfo)
        {
            if (IsBuilding)
            {
                Debug.LogError("Build already in progress!");
                return;
            }

            IsBuilding = true;
            EditorAssemblyReloadManager.LockReloadAssemblies = true;

            cancellationTokenSource = new CancellationTokenSource();

            if (buildInfo.IsCommandLine)
            {
                // We don't need stack traces on all our logs. Makes things a lot easier to read.
                Application.SetStackTraceLogType(LogType.Log, StackTraceLogType.None);
            }

            progress = 0f;

            // Ensure that the generated .appx version increments by modifying Package.appxmanifest
            try
            {
                if (!UpdateAppxManifest(buildInfo))
                {
                    throw new Exception();
                }
            }
            catch (Exception)
            {
                Debug.LogError("Failed to update appxmanifest!");

                if (buildInfo.IsCommandLine)
                {
                    throw;
                }

                return;
            }

            Task.Run(async () =>
            {
                try
                {
                    await BuildAppxAsync(buildInfo);
                }
                catch (Exception e)
                {
                    Debug.LogError(e);
                }

                IsBuilding = false;
            });

            while (!EditorUtility.DisplayCancelableProgressBar("XRTK Appx Build", string.Empty, progress))
            {
                if (!IsBuilding)
                {
                    break;
                }

                EditorApplication.Step();
            }

            cancellationTokenSource.Cancel();
            cancellationTokenSource.Dispose();
            cancellationTokenSource = null;
            IsBuilding = false;
            EditorApplication.delayCall += EditorUtility.ClearProgressBar;
            EditorAssemblyReloadManager.LockReloadAssemblies = false;
            AssetDatabase.SaveAssets();

            if (!buildInfo.IsCommandLine && buildInfo.Install)
            {
                //string fullBuildLocation = BuildDeployWindowOld.CalcMostRecentBuild();

                //if (UwpBuildDeployPreferences.TargetAllConnections)
                //{
                //    await BuildDeployWindowOld.InstallAppOnDevicesListAsync(fullBuildLocation, BuildDeployWindowOld.DevicePortalConnections);
                //}
                //else
                //{
                //    await BuildDeployWindowOld.InstallOnTargetDeviceAsync(fullBuildLocation, BuildDeployWindowOld.CurrentConnection);
                //}
            }
        }

        private static async Task BuildAppxAsync(UwpBuildInfo buildInfo)
        {
            progress = 0f;

            string slnOutputPath = Path.Combine(buildInfo.OutputDirectory, buildInfo.SolutionName);

            if (!File.Exists(slnOutputPath))
            {
                Debug.LogError("Unable to find Solution to build from!");
                return;
            }

            // Get and validate the msBuild path...
            var msBuildPath = await FindMsBuildPathAsync();

            if (!File.Exists(msBuildPath))
            {
                Debug.LogError($"MSBuild.exe is missing or invalid!\n{msBuildPath}");
                return;
            }

            var storagePath = Path.GetFullPath(Path.Combine(Path.Combine(BuildDeployPreferences.ApplicationDataPath, ".."), buildInfo.OutputDirectory));
            var solutionProjectPath = Path.GetFullPath(Path.Combine(storagePath, buildInfo.SolutionName));
            var appxBuildArgs = $"\"{solutionProjectPath}\" /t:{(buildInfo.RebuildAppx ? "Rebuild" : "Build")} /p:Configuration={buildInfo.Configuration} /p:Platform={buildInfo.PlatformArchitecture} /verbosity:{buildInfo.Verbosity}";
            Debug.Log(appxBuildArgs);
            progress = 0.5f;
            var processResult = await new Process().RunAsync(appxBuildArgs, msBuildPath, true, cancellationTokenSource.Token, false);

            switch (processResult.ExitCode)
            {
                case 0:
                    if (buildInfo.IsCommandLine)
                    {
                        Debug.Log(string.Join("\n", processResult.Output));
                    }
                    break;
                case -1073741510:
                    Debug.LogWarning("The build was terminated either by user's keyboard input CTRL+C or CTRL+Break or closing command prompt window.");
                    break;
                default:
                    if (processResult.ExitCode != 0)
                    {
                        Debug.LogError($"{buildInfo.BundleIdentifier} appx build Failed! ErrorCode:{processResult.ExitCode}:{string.Join("\n", processResult.Errors)}");

                        if (buildInfo.IsCommandLine)
                        {
                            var buildOutput = new StringBuilder();

                            if (processResult.Output?.Length > 0)
                            {
                                buildOutput.Append("Appx Build Output:");

                                foreach (var message in processResult.Output)
                                {
                                    buildOutput.Append($"\n{message}");
                                }
                            }

                            if (processResult.Errors?.Length > 0)
                            {
                                buildOutput.Append("Appx Build Errors:");

                                foreach (var error in processResult.Errors)
                                {
                                    buildOutput.Append($"\n{error}");
                                }
                            }

                            Debug.LogError(buildOutput);
                        }
                    }

                    break;
            }

            progress = 1f;
        }

        private static async Task<string> FindMsBuildPathAsync()
        {
            progress = 0.25f;

            var processResult = await new Process().RunAsync(
                new ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    CreateNoWindow = true,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    WorkingDirectory = @"C:\Program Files (x86)\Microsoft Visual Studio\Installer",
                    Arguments = "/c vswhere -all -products * -requires Microsoft.Component.MSBuild -property installationPath"
                }, false, cancellationTokenSource.Token);

            foreach (var path in processResult.Output)
            {
                if (string.IsNullOrEmpty(path)) { continue; }

                var paths = path.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);

                if (paths.Length > 0)
                {
                    // if there are multiple visual studio installs,
                    // prefer enterprise, then pro, then community
                    var bestPath = paths.OrderBy(p => p.ToLower().Contains("enterprise"))
                        .ThenBy(p => p.ToLower().Contains("professional"))
                        .ThenBy(p => p.ToLower().Contains("community")).First();

                    return bestPath.Contains("2019")
                        ? $@"{bestPath}\MSBuild\Current\Bin\MSBuild.exe"
                        : $@"{bestPath}\MSBuild\15.0\Bin\MSBuild.exe";
                }
            }

            return string.Empty;
        }

        private static bool UpdateAppxManifest(UwpBuildInfo buildInfo)
        {
            progress = 0.25f;

            // Find the manifest, assume the one we want is the first one
            string[] manifests = Directory.GetFiles(buildInfo.AbsoluteOutputDirectory, "Package.appxmanifest", SearchOption.AllDirectories);

            if (manifests.Length == 0)
            {
                Debug.LogError($"Unable to find Package.appxmanifest file for build (in path - {buildInfo.AbsoluteOutputDirectory})");
                return false;
            }

            if (manifests.Length > 1)
            {
                Debug.LogWarning("Found more than one appxmanifest in the target build folder!");
            }

            const string uap5 = "uap5";

            var rootNode = XElement.Load(manifests[0]);
            var identityNode = rootNode.Element(rootNode.GetDefaultNamespace() + "Identity");

            if (identityNode == null)
            {
                Debug.LogError($"Package.appxmanifest for build (in path - {buildInfo.AbsoluteOutputDirectory}) is missing an <Identity /> node");
                return false;
            }

            var dependencies = rootNode.Element(rootNode.GetDefaultNamespace() + "Dependencies");

            if (dependencies == null)
            {
                Debug.LogError($"Package.appxmanifest for build (in path - {buildInfo.AbsoluteOutputDirectory}) is missing <Dependencies /> node.");
                return false;
            }

            UpdateDependenciesElement(buildInfo, dependencies, rootNode.GetDefaultNamespace());

            // Setup the 3d app icon.
            if (!string.IsNullOrWhiteSpace(UwpBuildDeployPreferences.MixedRealityAppIconPath))
            {
                // Add the uap5 namespace if it doesn't exist.
                if (rootNode.GetNamespaceOfPrefix(uap5) == null)
                {
                    rootNode.Add(new XAttribute(XNamespace.Xmlns + uap5, Uap5NameSpace));
                }

                var ignorable = rootNode.Attribute(XName.Get("IgnorableNamespaces"));

                if (ignorable != null)
                {
                    if (!ignorable.Value.Contains(uap5))
                    {
                        ignorable.Value = $"{ignorable.Value} {uap5}";
                    }
                }

                if (!string.IsNullOrEmpty(UwpBuildDeployPreferences.MixedRealityAppIconPath))
                {
                    string modelPath;

                    // find mixed reality model container
                    var modelContainer = rootNode.Descendants(Uap5NameSpace + "MixedRealityModel").ToArray();

                    try
                    {
                        var modelFullPath = Path.GetFullPath(UwpBuildDeployPreferences.MixedRealityAppIconPath);
                        var absoluteBuildDirectory = Path.GetFullPath(BuildDeployPreferences.BuildDirectory);

                        modelPath = $"{absoluteBuildDirectory}/{buildInfo.BundleIdentifier}/Assets/{Path.GetFileName(modelFullPath)}";

                        if (File.Exists(modelPath))
                        {
                            File.Delete(modelPath);
                        }

                        File.Copy(modelFullPath, modelPath);
                        modelPath = modelPath.Replace($"{absoluteBuildDirectory}/{buildInfo.BundleIdentifier}/", string.Empty).Replace("/", "\\");
                    }
                    catch (Exception e)
                    {
                        Debug.LogError(e.Message);
                        return false;
                    }

                    if (modelContainer.Any())
                    {
                        var element = modelContainer.First();
                        var path = element.Attribute(XName.Get("Path"));

                        if (path != null)
                        {
                            path.Value = modelPath;
                        }
                        else
                        {
                            element.Add(new XAttribute("Path", modelPath));
                        }
                    }
                    else
                    {
                        var modelElement = new XElement(Uap5NameSpace + "MixedRealityModel");
                        var defaultTile = rootNode.Descendants(UapNameSpace + "DefaultTile").First();
                        defaultTile.Add(modelElement);
                        modelElement.Add(new XAttribute("Path", modelPath));
                    }
                }
            }

            // We use XName.Get instead of string -> XName implicit conversion because
            // when we pass in the string "Version", the program doesn't find the attribute.
            // Best guess as to why this happens is that implicit string conversion doesn't set the namespace to empty
            var versionAttr = identityNode.Attribute(XName.Get("Version"));

            if (versionAttr == null)
            {
                Debug.LogError($"Package.appxmanifest for build (in path - {buildInfo.AbsoluteOutputDirectory}) is missing a Version attribute in the <Identity /> node.");
                return false;
            }

            versionAttr.Value = PlayerSettings.WSA.packageVersion.ToString();
            rootNode.Save(manifests[0]);
            return true;
        }

        private static void UpdateDependenciesElement(UwpBuildInfo buildInfo, XElement dependencies, XNamespace defaultNamespace)
        {
            if (string.IsNullOrWhiteSpace(buildInfo.UwpSdk))
            {
                var windowsSdkPaths = Directory.GetDirectories(@"C:\Program Files (x86)\Windows Kits\10\Lib");

                for (int i = 0; i < windowsSdkPaths.Length; i++)
                {
                    windowsSdkPaths[i] = windowsSdkPaths[i].Substring(windowsSdkPaths[i].LastIndexOf(@"\", StringComparison.Ordinal) + 1);
                }

                EditorUserBuildSettings.wsaUWPSDK = windowsSdkPaths[windowsSdkPaths.Length - 1];
            }

            string maxVersionTested = buildInfo.UwpSdk;
            string minVersion = buildInfo.MinSdk;

            if (string.IsNullOrWhiteSpace(buildInfo.MinSdk))
            {
                minVersion = UwpBuildDeployPreferences.MIN_SDK_VERSION.ToString();
            }

            // Clear any we had before.
            dependencies.RemoveAll();

            foreach (var family in buildInfo.BuildTargetFamilies)
            {
                dependencies.Add(
                    new XElement(defaultNamespace + "TargetDeviceFamily",
                    new XAttribute("Name", $"Windows.{family}"),
                    new XAttribute("MinVersion", minVersion),
                    new XAttribute("MaxVersionTested", maxVersionTested)));
            }

            if (!dependencies.HasElements)
            {
                dependencies.Add(
                    new XElement(defaultNamespace + "TargetDeviceFamily",
                    new XAttribute("Name", "Windows.Universal"),
                    new XAttribute("MinVersion", minVersion),
                    new XAttribute("MaxVersionTested", maxVersionTested)));
            }
        }
    }
}
