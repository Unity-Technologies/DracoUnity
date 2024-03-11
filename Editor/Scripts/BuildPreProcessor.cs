// SPDX-FileCopyrightText: 2023 Unity Technologies and the Draco for Unity authors
// SPDX-License-Identifier: Apache-2.0

using System;
using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;

namespace Draco.Editor
{
    class BuildPreProcessor : IPreprocessBuildWithReport
    {
        internal const string packageName = "Packages/com.unity.cloud.draco/";

        const string k_PreCompiledLibraryName = "libdraco_unity.";

        public int callbackOrder => 0;

        void IPreprocessBuildWithReport.OnPreprocessBuild(BuildReport report)
        {
            SetRuntimePluginCopyDelegate(report.summary.platformGroup);
        }

        static void SetRuntimePluginCopyDelegate(BuildTargetGroup platformGroup)
        {
            var allPlugins = PluginImporter.GetAllImporters();
            var isSimulatorBuild = IsSimulatorBuild(platformGroup);
            foreach (var plugin in allPlugins)
            {
                if (plugin.isNativePlugin
                    && plugin.GetBuildTargetGroup() == platformGroup
                    && plugin.assetPath.StartsWith(packageName)
                    && plugin.assetPath.Contains(k_PreCompiledLibraryName)
                   )
                {
                    switch (platformGroup)
                    {
                        case BuildTargetGroup.iOS:
                        case BuildTargetGroup.tvOS:
#if UNITY_2022_3_OR_NEWER
                        case BuildTargetGroup.VisionOS:
#endif
                            plugin.SetIncludeInBuildDelegate(
                                plugin.IsSimulatorLibrary() == isSimulatorBuild
                                ? IncludeLibraryInBuild
                                : (PluginImporter.IncludeInBuildDelegate)ExcludeLibraryInBuild
                                );
                            break;
                    }
                }
            }
        }

        static bool IsSimulatorBuild(BuildTargetGroup platformGroup)
        {
            switch (platformGroup)
            {
#if UNITY_2022_3_OR_NEWER
                // Needs to be at the top,
                // because platformGroup(BuildTargetGroup.VisionOS) == BuildTargetGroup.iOS evaluates to true!
                case BuildTargetGroup.VisionOS:
                    return PlayerSettings.VisionOS.sdkVersion == VisionOSSdkVersion.Simulator;
#endif
                case BuildTargetGroup.iOS:
                    return PlayerSettings.iOS.sdkVersion == iOSSdkVersion.SimulatorSDK;
                case BuildTargetGroup.tvOS:
                    return PlayerSettings.tvOS.sdkVersion == tvOSSdkVersion.Simulator;
            }

            return false;
        }

        static bool ExcludeLibraryInBuild(string path)
        {
            return false;
        }

        static bool IncludeLibraryInBuild(string path)
        {
            return true;
        }
    }

    static class PluginImporterExtension
    {
        public static BuildTargetGroup GetBuildTargetGroup(this PluginImporter plugin)
        {
            var pluginsPath = $"{BuildPreProcessor.packageName}Runtime/Plugins/";
            var lastSlashIndex = plugin.assetPath.IndexOf('/', pluginsPath.Length);
            var relativePath = plugin.assetPath.Substring(pluginsPath.Length, lastSlashIndex - pluginsPath.Length);
            switch (relativePath)
            {
                case "Android":
                    return BuildTargetGroup.Android;
                case "iOS":
                    return BuildTargetGroup.iOS;
                case "tvOS":
                    return BuildTargetGroup.tvOS;
#if UNITY_2022_3_OR_NEWER
                case "visionOS":
                    return BuildTargetGroup.VisionOS;
#endif
                case "WSA":
                    return BuildTargetGroup.WSA;
                case "Windows":
                case "x86":
                case "x86_64":
                    return BuildTargetGroup.Standalone;
            }

            return BuildTargetGroup.Unknown;
        }

        public static bool IsSimulatorLibrary(this PluginImporter plugin)
        {
            var parent = new DirectoryInfo(plugin.assetPath).Parent;
            return parent != null && parent.Name == "Simulator";
        }
    }
}
