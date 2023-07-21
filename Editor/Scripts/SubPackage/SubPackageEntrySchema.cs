// SPDX-FileCopyrightText: 2023 Unity Technologies and the Draco for Unity authors
// SPDX-License-Identifier: Apache-2.0

using UnityEngine;


namespace SubPackage
{
    [System.Serializable]
    struct SubPackageEntrySchema
    {
        public string minimumUnityVersion;
        public string name;
        public string version;
        
        public string fullName => $"{name}@{version}";
    }
}