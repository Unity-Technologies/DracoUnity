using UnityEngine;


namespace SubPackage
{
    [System.Serializable]
    public struct SubPackageEntrySchema
    {
        public string minimumUnityVersion;
        public string name;
        public string version;
        
        public string fullName => $"{name}@{version}";
    }
}