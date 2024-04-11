// SPDX-FileCopyrightText: 2024 Unity Technologies and the Draco for Unity authors
// SPDX-License-Identifier: Apache-2.0

#if UNITY_2023_3_OR_NEWER || UNITY_2022_3
#define VISION_OS_SUPPORTED
#endif

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;

namespace Draco.Editor
{
    class BuildPreProcessor : IPreprocessBuildWithReport
    {
        public const string packagePath = "Packages/com.unity.cloud.draco/Runtime/Plugins/";

        internal static readonly Dictionary<GUID, int> webAssemblyLibraries = new Dictionary<GUID, int>()
        {
            // Database of WebAssembly library files within folder `Runtime/Plugins/WebGL`
            [new GUID("8c582db225b9e4bd4865264fece2da8b")] = 2020, // 2020/libdraco_unity.bc
            [new GUID("9846a73c344db4fa49e600594da610eb")] = 2021, // 2021/libdraco_unity.a
            [new GUID("300cc74d74bc64ca78d3fe7d50cb5439")] = 2022, // 2022/libdraco_unity.a
            [new GUID("9ab284c4ad5904cf09339d3522f7b10d")] = 2023, // 2023/libdraco_unity.a
        };

        public int callbackOrder => 0;

        void IPreprocessBuildWithReport.OnPreprocessBuild(BuildReport report)
        {
            SetRuntimePluginCopyDelegate(report.summary.platform);
        }

        static void SetRuntimePluginCopyDelegate(BuildTarget platform)
        {
            var allPlugins = PluginImporter.GetImporters(platform);
            foreach (var plugin in allPlugins)
            {
                if (plugin.isNativePlugin
                    && plugin.assetPath.StartsWith(packagePath)
                   )
                {
                    switch (platform)
                    {
                        case BuildTarget.iOS:
                        case BuildTarget.tvOS:
#if VISION_OS_SUPPORTED
                        case BuildTarget.VisionOS:
#endif
                            plugin.SetIncludeInBuildDelegate(IncludeAppleLibraryInBuild);
                            break;
                        case BuildTarget.WebGL:
                            if (webAssemblyLibraries.Keys.Any(libGuid => libGuid == AssetDatabase.GUIDFromAssetPath(plugin.assetPath)))
                            {
                                plugin.SetIncludeInBuildDelegate(IncludeWebLibraryInBuild);
                            }
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
#if VISION_OS_SUPPORTED
                case BuildTarget.VisionOS:
                    return PlayerSettings.VisionOS.sdkVersion == VisionOSSdkVersion.Simulator;
#endif
            }

            return false;
        }

        static bool IncludeAppleLibraryInBuild(string path)
        {
            var isSimulatorLibrary = IsAppleSimulatorLibrary(path);
            var isSimulatorBuild = IsSimulatorBuild(EditorUserBuildSettings.activeBuildTarget);
            return isSimulatorLibrary == isSimulatorBuild;
        }

        static bool IncludeWebLibraryInBuild(string path)
        {
            return IsWebAssemblyCompatible(path);
        }

        public static bool IsAppleSimulatorLibrary(string assetPath)
        {
            var parent = new DirectoryInfo(assetPath).Parent;

            switch (parent?.Name)
            {
                case "Simulator":
                    return true;
                case "Device":
                    return false;
                default:
                    throw new InvalidDataException(
                        $@"Could not determine SDK type of library ""{assetPath}"". " +
                        @"Apple iOS/tvOS/visionOS native libraries have to be placed in a folder named ""Device"" " +
                        @"or ""Simulator"" for implicit SDK type detection."
                    );
            }
        }

        static bool IsWebAssemblyCompatible(string assetPath)
        {
            var unityVersion = new UnityVersion(Application.unityVersion);

            var pluginGuid = AssetDatabase.GUIDFromAssetPath(assetPath);

            return IsWebAssemblyCompatible(pluginGuid, unityVersion);
        }

        public static bool IsWebAssemblyCompatible(GUID pluginGuid, UnityVersion unityVersion)
        {
            var wasm2021 = new UnityVersion("2021.2");
            var wasm2022 = new UnityVersion("2022.2");
            var wasm2023 = new UnityVersion("2023.2.0a17");

            if (webAssemblyLibraries.TryGetValue(pluginGuid, out var majorVersion))
            {
                switch (majorVersion)
                {
                    case 2020:
                        return unityVersion < wasm2021;
                    case 2021:
                        return unityVersion >= wasm2021 && unityVersion < wasm2022;
                    case 2022:
                        return unityVersion >= wasm2022 && unityVersion < wasm2023;
                    case 2023:
                        return unityVersion >= wasm2023;
                }
            }

            throw new InvalidDataException($"Unknown WebAssembly library at {AssetDatabase.GUIDToAssetPath(pluginGuid)}.");
        }
    }
}
