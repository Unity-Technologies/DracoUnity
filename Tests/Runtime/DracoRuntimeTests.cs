// SPDX-FileCopyrightText: 2023 Unity Technologies and the Draco for Unity authors
// SPDX-License-Identifier: Apache-2.0

#if !(UNITY_ANDROID || UNITY_WEBGL) || UNITY_EDITOR
#define LOCAL_LOADING
#endif

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using NUnit.Framework;
using Unity.Collections;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.Rendering;
using UnityEngine.TestTools;

namespace Draco.Tests
{

    [TestFixture]
    class DracoRuntimeTests : IPrebuildSetup
    {

        const string k_URLPrefix = "https://raw.githubusercontent.com/google/draco/master/testdata/";

        const string k_StreamingAssetsDir = "draco-test-data";

        static readonly string[] k_TestDataUrls =
        {
            "bunny_gltf.drc",
            "car.drc",
            "cube_att.obj.edgebreaker.cl10.2.2.drc",
            "cube_att.obj.edgebreaker.cl4.2.2.drc",
            "cube_att.obj.sequential.cl3.2.2.drc",
            "cube_att_sub_o_2.drc",
            "cube_att_sub_o_no_metadata.drc",
            "octagon_preserved.drc",
            "pc_kd_color.drc",
            "point_cloud_no_qp.drc",
            "test_nm.obj.edgebreaker.cl10.2.2.drc",
            "test_nm.obj.edgebreaker.cl4.2.2.drc",
            "test_nm.obj.sequential.cl3.2.2.drc",
            // // Unknown why it does not work
            // "cube_att.drc",
        };

        /// <summary>
        /// Legacy versions not supported
        /// </summary>
        static readonly string[] k_LegacyTestDataUrls = {
            "cube_pc.drc",
            "pc_color.drc",
            "test_nm.obj.edgebreaker.0.10.0.drc",
            "test_nm.obj.edgebreaker.0.9.1.drc",
            "test_nm.obj.edgebreaker.1.0.0.drc",
            "test_nm.obj.edgebreaker.1.1.0.drc",
            "test_nm.obj.sequential.0.10.0.drc",
            "test_nm.obj.sequential.0.9.1.drc",
            "test_nm.obj.sequential.1.0.0.drc",
            "test_nm.obj.sequential.1.1.0.drc",
            "test_nm_quant.0.9.0.drc",
        };

        public void Setup()
        {
#if UNITY_EDITOR
            DownloadTestData();
            AssetDatabase.Refresh();
#endif
        }

#if UNITY_EDITOR
        static void DownloadTestData()
        {
            var allUrls = new List<string>();
            allUrls.AddRange(k_TestDataUrls);
            allUrls.AddRange(k_LegacyTestDataUrls);

            var dir = Path.Combine(Application.streamingAssetsPath, k_StreamingAssetsDir);
            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            foreach (var url in allUrls)
            {
                var destination = GetAbsolutePath(url);
                if (File.Exists(destination))
                {
                    continue;
                }
                var webRequest = UnityWebRequest.Get(k_URLPrefix + url);
                var x = webRequest.SendWebRequest();
                while (!x.isDone) { }
                if (!string.IsNullOrEmpty(webRequest.error))
                {
                    Debug.LogError($"Loading Draco test data failed!\nError loading {url}: {webRequest.error}");
                    return;
                }

                File.WriteAllBytes(destination, webRequest.downloadHandler.data);
            }
        }
#endif

        static string GetAbsolutePath(string name)
        {
            return Path.Combine(Application.streamingAssetsPath, k_StreamingAssetsDir, name);
        }

        static async Task<NativeArray<byte>> GetTestData(string name)
        {
            var path = GetAbsolutePath(name);

#if LOCAL_LOADING
            path = $"file://{path}";
#endif
            var webRequest = UnityWebRequest.Get(path);
            var x = webRequest.SendWebRequest();
            while (!x.isDone)
            {
                await Task.Yield();
            }
            if (!string.IsNullOrEmpty(webRequest.error))
            {
                Debug.LogErrorFormat("Error loading {0}: {1}", path, webRequest.error);
            }

            return new NativeArray<byte>(webRequest.downloadHandler.data, Allocator.Persistent);
        }

        [UnityTest]
        [UseDracoTestFileCase(new[] {
            "bunny_gltf.drc",
            "car.drc",
            "cube_att.obj.edgebreaker.cl10.2.2.drc",
            "cube_att.obj.edgebreaker.cl4.2.2.drc",
            "cube_att.obj.sequential.cl3.2.2.drc",
            "cube_att_sub_o_2.drc",
            "cube_att_sub_o_no_metadata.drc",
            "octagon_preserved.drc",
            "pc_kd_color.drc",
            "point_cloud_no_qp.drc",
            "test_nm.obj.edgebreaker.cl10.2.2.drc",
            "test_nm.obj.edgebreaker.cl4.2.2.drc",
            "test_nm.obj.sequential.cl3.2.2.drc",
        })]
        public IEnumerator DecodeMesh(string url)
        {
            yield return RunTest(url, LoadBatchToMesh);
        }

        [UnityTest]
        [UseDracoTestFileCase(new[] {
            "bunny_gltf.drc",
            "car.drc",
            "cube_att.obj.edgebreaker.cl10.2.2.drc",
            "cube_att.obj.edgebreaker.cl4.2.2.drc",
            "cube_att.obj.sequential.cl3.2.2.drc",
            "cube_att_sub_o_2.drc",
            "cube_att_sub_o_no_metadata.drc",
            "octagon_preserved.drc",
            "pc_kd_color.drc",
            "point_cloud_no_qp.drc",
            "test_nm.obj.edgebreaker.cl10.2.2.drc",
            "test_nm.obj.edgebreaker.cl4.2.2.drc",
            "test_nm.obj.sequential.cl3.2.2.drc",
        })]
        public IEnumerator Decode(string url)
        {
            yield return RunTest(url, LoadBatchToMeshData);
        }

        [UnityTest]
        [UseDracoTestFileCase(new[] {
            "bunny_gltf.drc",
            "car.drc",
            "cube_att.obj.edgebreaker.cl10.2.2.drc",
            "cube_att.obj.edgebreaker.cl4.2.2.drc",
            "cube_att.obj.sequential.cl3.2.2.drc",
            "cube_att_sub_o_2.drc",
            "cube_att_sub_o_no_metadata.drc",
            "octagon_preserved.drc",
            "test_nm.obj.edgebreaker.cl10.2.2.drc",
            "test_nm.obj.edgebreaker.cl4.2.2.drc",
            "test_nm.obj.sequential.cl3.2.2.drc",
        })]
        public IEnumerator DecodeNormals(string url)
        {
            yield return RunTest(url, LoadBatchToMeshData, requireNormals: true);
        }

        [UnityTest]
        [UseDracoTestFileCase(new[] {
            "bunny_gltf.drc",
            "car.drc",
            "cube_att.obj.edgebreaker.cl10.2.2.drc",
            "cube_att.obj.edgebreaker.cl4.2.2.drc",
            "cube_att.obj.sequential.cl3.2.2.drc",
            "cube_att_sub_o_2.drc",
            "cube_att_sub_o_no_metadata.drc",
            "octagon_preserved.drc",
            "test_nm.obj.edgebreaker.cl10.2.2.drc",
            "test_nm.obj.edgebreaker.cl4.2.2.drc",
            "test_nm.obj.sequential.cl3.2.2.drc",
        })]
        public IEnumerator DecodeNormalsTangents(string url)
        {
            yield return RunTest(url, LoadBatchToMeshData, requireNormals: true, requireTangents: true);
        }

        [UnityTest]
        [UseDracoTestFileCase(new[] {
            "cube_pc.drc",
            "pc_color.drc",
            "test_nm.obj.edgebreaker.0.10.0.drc",
            "test_nm.obj.edgebreaker.0.9.1.drc",
            "test_nm.obj.edgebreaker.1.0.0.drc",
            "test_nm.obj.edgebreaker.1.1.0.drc",
            "test_nm.obj.sequential.0.10.0.drc",
            "test_nm.obj.sequential.0.9.1.drc",
            "test_nm.obj.sequential.1.0.0.drc",
            "test_nm.obj.sequential.1.1.0.drc",
            "test_nm_quant.0.9.0.drc",
        })]
        public IEnumerator DecodeLegacyMustFail(string url)
        {
            yield return RunTest(url, LoadBatchToMeshData, false);
        }

        static IEnumerator RunTest(
            string url,
            Func<int, NativeArray<byte>, bool, bool, Task> loadBatchFunc,
            bool mustSucceed = true,
            bool requireNormals = false,
            bool requireTangents = false
            )
        {
            var dataTask = GetTestData(url);
            while (!dataTask.IsCompleted)
            {
                yield return null;
            }
            if (dataTask.Exception != null)
            {
                throw dataTask.Exception;
            }
            using var data = dataTask.Result;
            var task = loadBatchFunc(1, data, requireNormals, requireTangents);
            while (!task.IsCompleted)
            {
                yield return null;
            }

            if (mustSucceed)
            {
                Assert.IsNull(task.Exception);
            }
            else
            {
                Assert.IsNotNull(task.Exception?.InnerException);
                Assert.AreEqual("Draco mesh decoding failed.", task.Exception.InnerException.Message);
            }
        }

        static async Task LoadBatchToMeshData(int quantity, NativeArray<byte> data, bool requireNormals = false, bool requireTangents = false)
        {

            var tasks = new List<Task<DecodeResult>>(quantity);
            var meshDataArray = Mesh.AllocateWritableMeshData(quantity);

            for (var i = 0; i < quantity; i++)
            {
                var task = DracoDecoder.DecodeMesh(meshDataArray[i], data, requireNormals: requireNormals, requireTangents: requireTangents);
                tasks.Add(task);
            }

            while (tasks.Count > 0)
            {
                var task = await Task.WhenAny(tasks);
                tasks.Remove(task);
                var result = await task;
                if (!result.success)
                {
                    meshDataArray.Dispose();
                    throw new ArgumentException("Draco mesh decoding failed.");
                }
            }

            var meshes = CreateMeshes(quantity);
            Mesh.ApplyAndDisposeWritableMeshData(meshDataArray, meshes, DracoDecoder.defaultMeshUpdateFlags);

            for (var i = 0; i < quantity; i++)
            {
                if (requireNormals)
                {
                    var normals = meshes[i].normals;
                    Assert.Greater(normals.Length, 0);
                }
                if (requireTangents)
                {
                    var tangents = meshes[i].tangents;
                    Assert.Greater(tangents.Length, 0);
                }
            }

            await Task.Yield();
        }

        static async Task LoadBatchToMesh(int quantity, NativeArray<byte> data, bool requireNormals = false, bool requireTangents = false)
        {
            var tasks = new List<Task<Mesh>>(quantity);

            for (var i = 0; i < quantity; i++)
            {
                var task = DracoDecoder.DecodeMesh(data, requireNormals: requireNormals, requireTangents: requireTangents);
                tasks.Add(task);
            }

            while (tasks.Count > 0)
            {
                var task = await Task.WhenAny(tasks);
                tasks.Remove(task);
                var mesh = await task;
                if (mesh == null)
                {
                    Debug.LogError("Loading mesh failed");
                }
                else
                {
                    if (requireNormals)
                    {
                        var normals = mesh.normals;
                        Assert.Greater(normals.Length, 0);
                    }
                    if (requireTangents)
                    {
                        var tangents = mesh.tangents;
                        Assert.Greater(tangents.Length, 0);
                    }
                }
            }
            await Task.Yield();
        }

        [UnityTest]
        public IEnumerator EncodePointCloud()
        {

            var sphereGo = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            var sphere = sphereGo.GetComponent<MeshFilter>().sharedMesh;
            var vertices = sphere.vertices;

            var mesh = new Mesh
            {
                subMeshCount = 1
            };
            mesh.SetSubMesh(0, new SubMeshDescriptor(0, 0, MeshTopology.Points));
            mesh.vertices = vertices;

            var task = Encode.DracoEncoder.EncodeMesh(mesh);
            while (!task.IsCompleted)
            {
                yield return null;
            }

            var result = task.Result;
            Assert.NotNull(result);
            Assert.AreEqual(1, result.Length);
            Assert.AreEqual(2325, result[0].data.Length);

            result[0].Dispose();
        }

        [UnityTest]
        public IEnumerator LoadGarbage()
        {
            var garbage = new byte[] { 71, 65, 82, 66, 65, 71, 69, 71, 65, 82, 66, 65, 71, 69, 71, 65, 82, 66, 65, 71, 69 };

            var task = DracoDecoder.DecodeMesh(garbage);

            while (!task.IsCompleted)
            {
                yield return null;
            }

            var result = task.Result;
            UnityEngine.Assertions.Assert.IsNull(result);
        }

        static Mesh[] CreateMeshes(int quantity)
        {
            var meshes = new Mesh[quantity];
            for (var index = 0; index < meshes.Length; index++)
            {
                meshes[index] = new Mesh();
            }
            return meshes;
        }
    }
}
