// SPDX-FileCopyrightText: 2023 Unity Technologies and the Draco for Unity authors
// SPDX-License-Identifier: Apache-2.0

using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using Draco.Encoder;

namespace Draco.Editor
{

    static class DracoEditorEncoder
    {
        const string k_CompressedMeshesDirName = "CompressedMeshes";

        struct DracoMesh
        {
            public MeshFilter target;
            public TextAsset[] dracoAssets;
            string[] m_SubmeshAssetPaths;

            public DracoMesh(MeshFilter target, string directory)
            {
                this.target = target;
                var mesh = target.sharedMesh;
                dracoAssets = new TextAsset[mesh.subMeshCount];
                var submeshFilenames
                    = new string[mesh.subMeshCount];
                m_SubmeshAssetPaths = new string[mesh.subMeshCount];

                var filename = string.IsNullOrEmpty(mesh.name) ? "Mesh-submesh-0.drc" : $"{mesh.name}-submesh-{{0}}.drc.bytes";
                for (int submesh = 0; submesh < mesh.subMeshCount; submesh++)
                {
                    submeshFilenames[submesh] = string.Format(filename, submesh);
                    m_SubmeshAssetPaths[submesh] = Path.Combine(directory, submeshFilenames[submesh]);
                }
            }

            public int submeshCount => dracoAssets.Length;

            public bool TryLoadDracoAssets()
            {
                var mesh = target.sharedMesh;
                for (int submesh = 0; submesh < mesh.subMeshCount; submesh++)
                {
                    if (dracoAssets[submesh] != null) continue;
                    dracoAssets[submesh] = AssetDatabase.LoadAssetAtPath<TextAsset>(m_SubmeshAssetPaths[submesh]);
                    if (dracoAssets[submesh] == null)
                    {
                        return false;
                    }
                }
                return true;
            }

            public string GetSubmeshAssetPath(int submeshIndex)
            {
                var projectPath = Directory.GetParent(Application.dataPath);
                if (projectPath == null)
                    return null;
                return Path.Combine(projectPath.FullName, m_SubmeshAssetPaths[submeshIndex]);
            }
        }

        [MenuItem("Tools/Draco/Encode Selected GameObject")]
        static void Compress()
        {

            var original = (GameObject)Selection.activeObject;
            if (original == null)
            {
                Debug.Log("No GameObject selected");
                return;
            }

            var root = Object.Instantiate(original);
            var meshFilters = root.GetComponentsInChildren<MeshFilter>();

            if (meshFilters.Length < 1)
            {
                Debug.Log("No GameObject with MeshFilter in selection");
                Object.DestroyImmediate(root);
                return;
            }

            CompressMeshFilters(meshFilters);

            Object.DestroyImmediate(root);
        }

        [MenuItem("Tools/Draco/Encode Active Scene")]
        static void CompressSceneMenu()
        {
            CompressScene(SceneManager.GetActiveScene());
        }

        static void CompressScene(Scene scene)
        {

            var scenePath = scene.path;
            var sceneDir = scenePath.Substring(0, scenePath.Length - 6);

            if (!Directory.Exists(sceneDir))
            {
                Directory.CreateDirectory(sceneDir);
            }

            sceneDir = Path.Combine(sceneDir, k_CompressedMeshesDirName);
            if (!Directory.Exists(sceneDir))
            {
                Directory.CreateDirectory(sceneDir);
            }

            var objects = scene.GetRootGameObjects();

            var meshFilters = new List<MeshFilter>();
            foreach (var gameObject in objects)
            {
                meshFilters.AddRange(gameObject.GetComponentsInChildren<MeshFilter>());
            }
            CompressMeshFilters(meshFilters.ToArray(), sceneDir);
        }

        static void CompressMeshFilters(MeshFilter[] meshFilters, string directory = null)
        {

            var instances = new Dictionary<TextAsset, DracoDecodeInstance>();

            var meshDecoder = Object.FindObjectOfType<DracoDecoder>();
            if (meshDecoder == null)
            {
                meshDecoder = new GameObject("MeshDecoder").AddComponent<DracoDecoder>();
            }

            directory = directory ?? $"Assets/{k_CompressedMeshesDirName}";
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var dracoMeshes = new List<DracoMesh>();
            var dracoFilesUpdated = false;

            foreach (var meshFilter in meshFilters)
            {
                var mesh = meshFilter.sharedMesh;
                if (mesh == null) continue;
#if !UNITY_EDITOR
                if (!mesh.isReadable)
                {
                    Debug.LogError("Mesh is not readable!");
                    return;
                }
#endif
                var dracoMesh = new DracoMesh(meshFilter, directory);
                var dracoFilesMissing = !dracoMesh.TryLoadDracoAssets();

                if (dracoFilesMissing)
                {
                    var scale = meshFilter.transform.localToWorldMatrix.lossyScale;
                    var dracoData = AsyncHelpers.RunSync(() => DracoEncoder.EncodeMesh(mesh, scale, .0001f));
                    if (dracoData != null && dracoData.Length > 0)
                    {
                        for (var submesh = 0; submesh < dracoData.Length; submesh++)
                        {
                            if (submesh > 0) Debug.LogWarning("more than one submesh. not supported yet.");
                            File.WriteAllBytes(dracoMesh.GetSubmeshAssetPath(submesh), dracoData[submesh].data.ToArray());
                            dracoData[submesh].Dispose();
                            dracoFilesUpdated = true;
                            AssetDatabase.SaveAssets();
                            AssetDatabase.Refresh();
                        }
                    }
                    else
                    {
                        Debug.LogError("Draco encoding failed");
                        return;
                    }
                }

                dracoMeshes.Add(dracoMesh);
            }

            if (dracoFilesUpdated)
            {

                foreach (var dracoMesh in dracoMeshes)
                {
                    if (!dracoMesh.TryLoadDracoAssets())
                    {
                        Debug.LogError("Loading draco assets failed");
                        return;
                    }
                }
            }

            foreach (var dracoMesh in dracoMeshes)
            {
                for (int submesh = 0; submesh < dracoMesh.submeshCount; submesh++)
                {
                    // meshDecoder.AddTarget(dracoMesh.target,dracoMesh.dracoAssets[submesh]);
                    var dracoAsset = dracoMesh.dracoAssets[submesh];
                    if (instances.TryGetValue(dracoAsset, out var instance))
                    {
                        instance.AddTarget(dracoMesh.target);
                    }
                    else
                    {
                        var newInstance = ScriptableObject.CreateInstance<DracoDecodeInstance>();
                        var bounds = dracoMesh.target.sharedMesh.bounds;
                        newInstance.SetAsset(dracoAsset, bounds);
                        newInstance.AddTarget(dracoMesh.target);
                        instances[dracoAsset] = newInstance;
                    }
                }
                dracoMesh.target.mesh = null;
            }

            meshDecoder.instances = instances.Values.ToArray();
        }

        [MenuItem("Tools/Draco/Encode Selected Mesh")]
        static void EncodeSelectedMeshes()
        {
            var meshes = Selection.GetFiltered<Mesh>(SelectionMode.Deep);
            if (meshes == null || meshes.Length <= 0)
            {
                Debug.Log("No mesh selected");
                return;
            }

            var directory = EditorUtility.OpenFolderPanel("Save Draco files to folder", "", "");
            if (string.IsNullOrEmpty(directory)) return;

            foreach (var mesh in meshes)
            {
                EncodeMesh(mesh, directory);
            }
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        static void EncodeMesh(Mesh mesh, string directory)
        {
            Debug.Log($"Encode mesh {mesh.name} to {directory}");
#if !UNITY_EDITOR
            if (!mesh.isReadable)
            {
                Debug.LogError($"Mesh {mesh.name} is not readable!");
                return;
            }
#endif
            var dracoData = AsyncHelpers.RunSync(() => DracoEncoder.EncodeMesh(mesh));
            if (dracoData.Length > 1)
            {
                var filename = string.IsNullOrEmpty(mesh.name) ? "Mesh-submesh-{0}.drc.bytes" : $"{mesh.name}-submesh-{{0}}.drc.bytes";
                for (var submesh = 0; submesh < dracoData.Length; submesh++)
                {
                    File.WriteAllBytes(Path.Combine(directory, string.Format(filename, submesh)), dracoData[submesh].data.ToArray());
                    dracoData[submesh].Dispose();
                }
            }
            else
            {
                var filename = string.IsNullOrEmpty(mesh.name) ? "Mesh.drc.bytes" : $"{mesh.name}.drc.bytes";
                File.WriteAllBytes(Path.Combine(directory, filename), dracoData[0].data.ToArray());
                dracoData[0].Dispose();
            }
        }
    }
}
