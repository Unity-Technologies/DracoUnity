// SPDX-FileCopyrightText: 2023 Unity Technologies and the Draco for Unity authors
// SPDX-License-Identifier: Apache-2.0

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Draco.Encode;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Assertions;

namespace Draco.Sample.SceneEncodeDecode.Editor
{
    static class SceneEncoder
    {
        const string k_CompressedMeshesDirName = "CompressedMeshes";

        internal static List<MeshFilter> GetAllMeshFilters(Scene scene)
        {
            var objects = scene.GetRootGameObjects();
            var meshFilters = new List<MeshFilter>();
            if (objects != null && objects.Length > 0)
            {
                foreach (var gameObject in objects)
                {
                    meshFilters.AddRange(gameObject.GetComponentsInChildren<MeshFilter>());
                }
            }

            return meshFilters;
        }

        internal static MeshFilter[] GetAllMeshFilters(GameObject gameObject)
        {
            return gameObject.GetComponentsInChildren<MeshFilter>();
        }

        internal static async Task CompressScene(Scene scene, bool setupMeshDecoder = false)
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

            var meshFilters = GetAllMeshFilters(scene);
            await CompressMeshFilters(meshFilters.ToArray(), sceneDir, setupMeshDecoder);
        }

        internal static async Task CompressMeshFilters(IEnumerable<MeshFilter> meshFilters, string directory = null, bool setupMeshDecoder = false)
        {

            var instances = new Dictionary<TextAsset, DecodeInstance>();

            var meshDecoder = Object.FindObjectOfType<DracoSceneDecoder>();
            if (meshDecoder == null && setupMeshDecoder)
            {
                meshDecoder = new GameObject("MeshDecoder").AddComponent<DracoSceneDecoder>();
            }

            directory = directory ?? $"Assets/{k_CompressedMeshesDirName}";
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var dracoMeshes = new List<DracoMeshAsset>();
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
                var dracoMesh = new DracoMeshAsset(meshFilter, directory);
                var dracoFilesMissing = !dracoMesh.TryLoadDracoAssets(out var sampleAsset);

                if (dracoFilesMissing)
                {
                    var scale = meshFilter.transform.localToWorldMatrix.lossyScale;
                    var dracoData = await DracoEncoder.EncodeMesh(
                        mesh,
                        QuantizationSettings.FromWorldSize(mesh.bounds, scale, .0001f),
                        SpeedSettings.Default
                        );
                    if (dracoData != null && dracoData.Length > 0)
                    {
                        var projectPath = Directory.GetParent(Application.dataPath);
                        Assert.IsNotNull(projectPath);
                        for (var submesh = 0; submesh < dracoData.Length; submesh++)
                        {
                            if (submesh > 0) Debug.LogWarning("more than one submesh. not supported yet.");
                            var subPath = dracoMesh.GetSubmeshAssetPath(submesh);
                            File.WriteAllBytes(
                                Path.Combine(projectPath.FullName, subPath),
                                dracoData[submesh].data.ToArray()
                                );
                            dracoData[submesh].Dispose();
                            dracoFilesUpdated = true;
                            AssetDatabase.SaveAssets();
                            AssetDatabase.Refresh();
                            Debug.Log($"`{meshFilter.name}` was encoded to {subPath}.", AssetDatabase.LoadAssetAtPath<TextAsset>(subPath));
                        }
                    }
                    else
                    {
                        Debug.LogError($"Encoding `{meshFilter.name}` failed", meshFilter);
                        return;
                    }
                }
                else
                {
                    Debug.Log($"Skipping `{meshFilter.name}`. Draco file(s) have been encoded before (e.g. at {dracoMesh.GetSubmeshAssetPath(0)}.", sampleAsset);
                }

                dracoMeshes.Add(dracoMesh);
            }

            if (dracoFilesUpdated)
            {

                foreach (var dracoMesh in dracoMeshes)
                {
                    if (!dracoMesh.TryLoadDracoAssets(out _))
                    {
                        Debug.LogError("Loading draco assets failed");
                        return;
                    }
                }
            }

            foreach (var dracoMesh in dracoMeshes)
            {
                for (var submesh = 0; submesh < dracoMesh.submeshCount; submesh++)
                {
                    var dracoAsset = dracoMesh.dracoAssets[submesh];
                    if (instances.TryGetValue(dracoAsset, out var instance))
                    {
                        instance.AddTarget(dracoMesh.target);
                    }
                    else
                    {
                        var newInstance = ScriptableObject.CreateInstance<DecodeInstance>();
                        var bounds = dracoMesh.target.sharedMesh.bounds;
                        newInstance.SetAsset(dracoAsset, bounds);
                        newInstance.AddTarget(dracoMesh.target);
                        instances[dracoAsset] = newInstance;
                    }
                }

                if (setupMeshDecoder)
                {
                    dracoMesh.target.mesh = null;
                }
            }

            if (setupMeshDecoder)
            {
                meshDecoder.instances = instances.Values.ToArray();
            }
        }
    }
}
