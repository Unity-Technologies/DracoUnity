// SPDX-FileCopyrightText: 2023 Unity Technologies and the Draco for Unity authors
// SPDX-License-Identifier: Apache-2.0

using UnityEngine;

namespace SubPackage
{
    static class SubPackageConfiguration
    {
        internal const string packageName = "Draco for Unity";

        internal static readonly SubPackageConfigSchema config = new SubPackageConfigSchema
        {
            dialogTitle = "Installing Sub Packages",
            dialogText = $"The {packageName} package requires sub-packages which vary, depending on the Unity version. These dependencies will now be updated automatically and will appear in your project's manifest file.",

            cleanupRegex = @"^com\.unity\.cloud\.draco\.webgl-.*$",

            subPackages = new[]
            {
                new SubPackageEntrySchema
                {
                    name = "com.unity.cloud.draco.webgl-2023",
                    minimumUnityVersion = "2023.2.0a17",
                    version = "1.0.0"
                },
                new SubPackageEntrySchema
                {
                    name = "com.unity.cloud.draco.webgl-2022",
                    minimumUnityVersion = "2022.2.0",
                    version = "1.0.0"
                },
                new SubPackageEntrySchema
                {
                    name = "com.unity.cloud.draco.webgl-2021",
                    minimumUnityVersion = "2021.2.0",
                    version = "1.0.0"
                },
                new SubPackageEntrySchema
                {
                    name = "com.unity.cloud.draco.webgl-2020",
                    minimumUnityVersion = "2019.2.0",
                    version = "1.0.0"
                }
            }
        };
    }
}
