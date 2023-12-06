// SPDX-FileCopyrightText: 2023 Unity Technologies and the Draco for Unity authors
// SPDX-License-Identifier: Apache-2.0

using System.IO;
using UnityEditor;
using UnityEngine;

namespace Draco.Sample.SceneEncodeDecode.Editor
{
    struct DracoMeshAsset
    {
        public MeshFilter target;
        public TextAsset[] dracoAssets;
        string[] m_SubmeshAssetPaths;

        public DracoMeshAsset(MeshFilter target, string directory)
        {
            this.target = target;
            var mesh = target.sharedMesh;
            dracoAssets = new TextAsset[mesh.subMeshCount];
            var submeshFilenames
                = new string[mesh.subMeshCount];
            m_SubmeshAssetPaths = new string[mesh.subMeshCount];

            var filename = string.IsNullOrEmpty(mesh.name) ? "Mesh-submesh-0.drc" : $"{mesh.name}-submesh-{{0}}.drc.bytes";
            for (var submesh = 0; submesh < mesh.subMeshCount; submesh++)
            {
                submeshFilenames[submesh] = string.Format(filename, submesh);
                m_SubmeshAssetPaths[submesh] = Path.Combine(directory, submeshFilenames[submesh]);
            }
        }

        public int submeshCount => dracoAssets.Length;

        public bool TryLoadDracoAssets(out TextAsset sampleAsset)
        {
            sampleAsset = null;
            var mesh = target.sharedMesh;
            for (var submesh = 0; submesh < mesh.subMeshCount; submesh++)
            {
                if (dracoAssets[submesh] != null) continue;
                sampleAsset = AssetDatabase.LoadAssetAtPath<TextAsset>(m_SubmeshAssetPaths[submesh]);
                dracoAssets[submesh] = sampleAsset;
                if (dracoAssets[submesh] == null)
                {
                    return false;
                }
            }
            return true;
        }

        public string GetSubmeshAssetPath(int submeshIndex)
        {
            return m_SubmeshAssetPaths[submeshIndex];
        }
    }
}
