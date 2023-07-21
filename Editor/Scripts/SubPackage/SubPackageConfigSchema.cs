// SPDX-FileCopyrightText: 2023 Unity Technologies and the Draco for Unity authors
// SPDX-License-Identifier: Apache-2.0

using UnityEngine;

namespace SubPackage
{

    [System.Serializable]
    class SubPackageConfigSchema
    {
        public string dialogTitle;
        public string dialogText;

        public string cleanupRegex;
        public SubPackageEntrySchema[] subPackages;
    }

}
