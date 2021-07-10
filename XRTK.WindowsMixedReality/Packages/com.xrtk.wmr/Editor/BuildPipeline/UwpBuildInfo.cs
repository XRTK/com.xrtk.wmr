// Copyright (c) XRTK. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;
using XRTK.Attributes;
using XRTK.Definitions.Platforms;
using XRTK.Services;

namespace XRTK.Editor.BuildPipeline
{
    [RuntimePlatform(typeof(UniversalWindowsPlatform))]
    public class UwpBuildInfo : BuildInfo
    {
        public enum Platform
        {
            x86 = 0,
            x64 = 1,
            ARM = 2,
            ARM64 = 3,
        }

        public enum VerbosityLevel
        {
            Quiet = 0,
            Minimal,
            Normal,
            Detailed,
            Diagnostic
        }

        /// <inheritdoc />
        protected override void OnEnable()
        {
            base.OnEnable();

            if (string.IsNullOrWhiteSpace(solutionName))
            {
                solutionName = $"{PlayerSettings.productName}\\{PlayerSettings.productName}.sln";
            }
        }

        /// <inheritdoc />
        public override BuildTarget BuildTarget => BuildTarget.WSAPlayer;

        private Version version;

        /// <inheritdoc />
        public override Version Version
        {
            get
            {
                if (version == null)
                {
                    version = PlayerSettings.WSA.packageVersion;
                }

                return version;
            }
            set => version = value;
        }

        [SerializeField]
        private string solutionName;

        /// <summary>
        /// The name of the Visual Studio .sln file generated.
        /// </summary>
        public string SolutionName => solutionName;

        [SerializeField]
        private bool buildAppx;

        /// <summary>
        /// Build the appx bundle after building Unity Player?
        /// </summary>
        public bool BuildAppx
        {
            get => buildAppx;
            set => buildAppx = value;
        }

        [SerializeField]
        private bool rebuildAppx = false;

        /// <summary>
        /// Force rebuilding the appx bundle?
        /// </summary>
        public bool RebuildAppx
        {
            get => rebuildAppx;
            set => rebuildAppx = value;
        }

        [SerializeField]
        private Platform platformArchitecture = Platform.ARM64;

        /// <summary>
        /// The build platform architecture (i.e. x86, x64, ARM, ARM64)
        /// </summary>
        public Platform PlatformArchitecture
        {
            get => platformArchitecture;
            set => platformArchitecture = value;
        }

        [SerializeField]
        private VerbosityLevel verbosity = VerbosityLevel.Quiet;

        public VerbosityLevel Verbosity
        {
            get => verbosity;
            set => verbosity = value;
        }

        public string UwpSdk => EditorUserBuildSettings.wsaUWPSDK;

        public string MinSdk => EditorUserBuildSettings.wsaMinUWPSDK;

        private PlayerSettings.WSATargetFamily[] buildTargetFamilies;

        public PlayerSettings.WSATargetFamily[] BuildTargetFamilies => buildTargetFamilies ?? (buildTargetFamilies = GetFamilies());

        private static PlayerSettings.WSATargetFamily[] GetFamilies()
        {
            var values = (PlayerSettings.WSATargetFamily[])Enum.GetValues(typeof(PlayerSettings.WSATargetFamily));
            return values.Where(PlayerSettings.WSA.GetTargetDeviceFamily).ToArray();
        }

        /// <inheritdoc />
        public override void OnPreProcessBuild(BuildReport report)
        {
            if (!MixedRealityToolkit.ActivePlatforms.Contains(BuildPlatform) ||
                EditorUserBuildSettings.activeBuildTarget != BuildTarget)
            {
                return;
            }

            EditorUserBuildSettings.wsaArchitecture = platformArchitecture.ToString();

            if (!UwpAppxBuildTools.ValidateUwpSdk())
            {
                throw new ArgumentException("Invalid Windows SDK");
            }
        }

        /// <inheritdoc />
        public override void OnPostProcessBuild(BuildReport buildReport)
        {
            if (!MixedRealityToolkit.ActivePlatforms.Contains(BuildPlatform) ||
                EditorUserBuildSettings.activeBuildTarget != BuildTarget)
            {
                return;
            }

            if (buildReport.summary.result == BuildResult.Failed)
            {
                if (!Application.isBatchMode)
                {
                    EditorUtility.DisplayDialog($"{PlayerSettings.productName} Build {buildReport.summary.result}!", "See console for details", "OK");
                }
            }
            else
            {
                if (BuildAppx ||
                    !Application.isBatchMode &&
                    !EditorUtility.DisplayDialog(PlayerSettings.productName, "Build Complete", "OK", "Build AppX"))
                {
                    UwpAppxBuildTools.BuildAppx(this);
                }
            }
        }

        /// <inheritdoc />
        public override void ParseCommandLineArgs()
        {
            base.ParseCommandLineArgs();

            string[] arguments = Environment.GetCommandLineArgs();

            for (int i = 0; i < arguments.Length; ++i)
            {
                switch (arguments[i])
                {
                    case "-verbosity":
                        var verb = arguments[++i].Substring(1);

                        switch (verb)
                        {
                            case "q":
                            case "quiet":
                                Verbosity = VerbosityLevel.Quiet;
                                break;
                            case "m":
                            case "minimal":
                                Verbosity = VerbosityLevel.Minimal;
                                break;
                            case "n":
                            case "normal":
                                Verbosity = VerbosityLevel.Normal;
                                break;
                            case "d":
                            case "detailed":
                                Verbosity = VerbosityLevel.Detailed;
                                break;
                            case "diag":
                            case "diagnostic":
                                Verbosity = VerbosityLevel.Diagnostic;
                                break;
                            default:
                                Debug.LogError($"Failed to parse -{nameof(verbosity)}: \"{verb}\"");
                                break;
                        }

                        break;
                    case "-buildAppx":
                        BuildAppx = true;
                        break;
                    case "-rebuildAppx":
                        RebuildAppx = true;
                        break;
                    case "-buildArchitecture":
                        var architecture = arguments[++i].Substring(1);

                        switch (architecture)
                        {
                            case "x86":
                                PlatformArchitecture = Platform.x86;
                                break;
                            case "x64":
                                PlatformArchitecture = Platform.x64;
                                break;
                            case "ARM":
                                PlatformArchitecture = Platform.ARM;
                                break;
                            case "ARM64":
                                PlatformArchitecture = Platform.ARM64;
                                break;
                            default:
                                Debug.LogError($"Failed to parse -buildArchitecture: \"{architecture}\"");
                                break;
                        }

                        break;
                }
            }
        }
    }
}
