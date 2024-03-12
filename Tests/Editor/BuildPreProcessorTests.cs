// SPDX-FileCopyrightText: 2024 Unity Technologies and the Draco for Unity authors
// SPDX-License-Identifier: Apache-2.0

using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Draco.Editor.Tests
{
    class BuildPreProcessorTests
    {
        [Test]
        public void AppleIOSLibraryTypeCheck()
        {
            AppleLibraryTypeCheck(BuildTarget.iOS);
        }

        [Test]
        public void AppleTvOSLibraryTypeCheck()
        {
            AppleLibraryTypeCheck(BuildTarget.tvOS);
        }

#if UNITY_2022_3_OR_NEWER
        [Test]
        public void AppleVisionOSLibraryTypeCheck()
        {
            AppleLibraryTypeCheck(BuildTarget.tvOS);
        }
#endif

        static void AppleLibraryTypeCheck(BuildTarget buildTarget)
        {
            var allPlugins = PluginImporter.GetImporters(buildTarget)
                .Where(plugin => plugin.isNativePlugin && plugin.assetPath.StartsWith(BuildPreProcessor.packagePath))
                .ToList();
            Assert.GreaterOrEqual(2, allPlugins.Count);
            foreach (var plugin in allPlugins)
            {
                // Checks that it does not throw an InvalidDataException.
                BuildPreProcessor.IsAppleSimulatorLibrary(plugin);
            }
        }
    }
}
