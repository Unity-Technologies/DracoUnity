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
        public const string packagePath = "Packages/com.unity.cloud.draco/";

        const string k_PreCompiledLibraryName = "libdraco_unity.";

        public const string wasm2020Guid = "8c582db225b9e4bd4865264fece2da8b";
        public const string wasm2021Guid = "9846a73c344db4fa49e600594da610eb";
        public const string wasm2022Guid = "300cc74d74bc64ca78d3fe7d50cb5439";
        public const string wasm2023Guid = "9ab284c4ad5904cf09339d3522f7b10d";

        public int callbackOrder => 0;

        void IPreprocessBuildWithReport.OnPreprocessBuild(BuildReport report)
        {
            SetRuntimePluginCopyDelegate(report.summary.platform);
        }

        static void SetRuntimePluginCopyDelegate(BuildTarget platform)
        {
            var allPlugins = PluginImporter.GetImporters(platform);
            var isSimulatorBuild = IsSimulatorBuild(platform);
            foreach (var plugin in allPlugins)
            {
                if (plugin.isNativePlugin
                    && plugin.assetPath.StartsWith(packagePath)
                    && plugin.assetPath.Contains(k_PreCompiledLibraryName)
                   )
                {
                    switch (platform)
                    {
                        case BuildTarget.iOS:
                        case BuildTarget.tvOS:
#if UNITY_2022_3_OR_NEWER
                        case BuildTarget.VisionOS:
#endif
                            plugin.SetIncludeInBuildDelegate(
                                IsAppleSimulatorLibrary(plugin) == isSimulatorBuild
                                ? IncludeLibraryInBuild
                                : (PluginImporter.IncludeInBuildDelegate)ExcludeLibraryInBuild
                                );
                            break;
                        case BuildTarget.WebGL:
                            plugin.SetIncludeInBuildDelegate(
                                    IsWebAssemblyCompatible(plugin)
                                    ? IncludeLibraryInBuild
                                    : (PluginImporter.IncludeInBuildDelegate)ExcludeLibraryInBuild
                            );
                            break;
                    }
                }
            }
        }

        static bool IsSimulatorBuild(BuildTarget platformGroup)
        {
            switch (platformGroup)
            {
                case BuildTarget.iOS:
                    return PlayerSettings.iOS.sdkVersion == iOSSdkVersion.SimulatorSDK;
                case BuildTarget.tvOS:
                    return PlayerSettings.tvOS.sdkVersion == tvOSSdkVersion.Simulator;
#if UNITY_2022_3_OR_NEWER
                case BuildTarget.VisionOS:
                    return PlayerSettings.VisionOS.sdkVersion == VisionOSSdkVersion.Simulator;
#endif
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

        public static bool IsAppleSimulatorLibrary(PluginImporter plugin)
        {
            var parent = new DirectoryInfo(plugin.assetPath).Parent;

            switch (parent?.Name)
            {
                case "Simulator":
                    return true;
                case "Device":
                    return false;
                default:
                    throw new InvalidDataException(
                        $@"Could not determine SDK type of library ""{plugin.assetPath}"". " +
                        @"Apple iOS/tvOS/visionOS native libraries have to be placed in a folder named ""Device"" " +
                        @"or ""Simulator"" for implicit SDK type detection."
                    );
            }
        }

        static bool IsWebAssemblyCompatible(PluginImporter plugin)
        {
            var unityVersion = new UnityVersion(Application.unityVersion);

            var pluginGuid = AssetDatabase.GUIDFromAssetPath(plugin.assetPath);

            return IsWebAssemblyCompatible(pluginGuid, unityVersion);
        }

        public static bool IsWebAssemblyCompatible(GUID pluginGuid, UnityVersion unityVersion)
        {
            var wasm2021 = new UnityVersion("2021.2");
            var wasm2022 = new UnityVersion("2022.2");
            var wasm2023 = new UnityVersion("2023.2.0a17");

            if (pluginGuid == new GUID(wasm2020Guid))
            {
                return unityVersion < wasm2021;
            }

            if (pluginGuid == new GUID(wasm2021Guid))
            {
                return unityVersion >= wasm2021 && unityVersion < wasm2022;
            }

            if (pluginGuid == new GUID(wasm2022Guid))
            {
                return unityVersion >= wasm2022 && unityVersion < wasm2023;
            }

            if (pluginGuid == new GUID(wasm2023Guid))
            {
                return unityVersion >= wasm2023;
            }

            throw new InvalidDataException($"Unknown WebAssembly library at {AssetDatabase.GUIDToAssetPath(pluginGuid)}.");
        }
    }
}
