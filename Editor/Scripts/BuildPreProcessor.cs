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
                if (plugin.isNativePlugin)
                {
                    switch (platformGroup)
                    {
                        case BuildTargetGroup.iOS:
                        case BuildTargetGroup.tvOS:
                            if (plugin.IsAppleArmPlatformLibrary())
                            {
                                plugin.SetIncludeInBuildDelegate(
                                    plugin.IsSimulatorLibrary() == isSimulatorBuild
                                    ? IncludeLibraryInBuild
                                    : (PluginImporter.IncludeInBuildDelegate)ExcludeLibraryInBuild
                                    );
                            }
                            break;
                    }
                }
            }
        }

        static bool IsSimulatorBuild(BuildTargetGroup platformGroup)
        {
            switch (platformGroup)
            {
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
        /// <summary>
        /// Tells if the library targets Apple non-macOS platforms.
        /// </summary>
        /// <param name="plugin">Native library importer.</param>
        /// <returns>True if the target platform is among iOS, tvOS or visionOS. False otherwise.</returns>
        public static bool IsAppleArmPlatformLibrary(this PluginImporter plugin)
        {
            var extension = Path.GetExtension(plugin.assetPath);
            return extension == ".a";
        }

        public static bool IsSimulatorLibrary(this PluginImporter plugin)
        {
            var parent = new DirectoryInfo(plugin.assetPath).Parent;
            return parent != null && parent.Name == "Simulator";
        }
    }
}
