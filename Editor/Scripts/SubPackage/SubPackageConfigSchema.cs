using UnityEngine;
using UnityEngine.Serialization;

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
