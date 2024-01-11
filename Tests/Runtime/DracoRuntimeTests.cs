// SPDX-FileCopyrightText: 2023 Unity Technologies and the Draco for Unity authors
// SPDX-License-Identifier: Apache-2.0

#if !(UNITY_ANDROID || UNITY_WEBGL) || UNITY_EDITOR
#define LOCAL_LOADING
#endif

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Threading;
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

        // Default cube with position(0), normal(1), tangent(2), UV1(3) and UV2(4)
        static readonly byte[] k_CubeDracoData = { 0x44, 0x52, 0x41, 0x43, 0x4F, 0x02, 0x02, 0x01, 0x01, 0x00, 0x00, 0x00, 0x18, 0x0C, 0x04, 0x0C, 0x00, 0x00, 0x05, 0xEF, 0xFB, 0xBE, 0xEF, 0x0B, 0xFF, 0x02, 0x66, 0x40, 0xFF, 0x02, 0x66, 0x40, 0xFF, 0x02, 0x66, 0x40, 0xFF, 0x02, 0x66, 0x40, 0xFF, 0x02, 0x66, 0x40, 0x05, 0xFF, 0x00, 0x00, 0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x02, 0x00, 0x00, 0x03, 0x00, 0x00, 0x01, 0x00, 0x09, 0x03, 0x00, 0x00, 0x02, 0x01, 0x01, 0x09, 0x03, 0x00, 0x01, 0x03, 0x01, 0x04, 0x09, 0x04, 0x00, 0x02, 0x02, 0x01, 0x03, 0x09, 0x02, 0x00, 0x03, 0x02, 0x01, 0x03, 0x09, 0x02, 0x00, 0x04, 0x02, 0x01, 0x01, 0x01, 0x01, 0x02, 0x03, 0x1D, 0x27, 0x39, 0x0E, 0xAD, 0x0A, 0x0F, 0x76, 0x9D, 0xC4, 0x95, 0xA9, 0x66, 0xC7, 0x90, 0xC7, 0x8D, 0x6E, 0x3B, 0x3F, 0x53, 0x80, 0x00, 0x00, 0x00, 0x00, 0xFF, 0x3F, 0x00, 0x00, 0x00, 0x00, 0x00, 0xBF, 0x00, 0x00, 0x00, 0xBF, 0x00, 0x00, 0x00, 0xBF, 0x00, 0x00, 0x80, 0x3F, 0x0E, 0x00, 0x03, 0x01, 0x01, 0x02, 0x81, 0x04, 0x01, 0x34, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xF7, 0xAD, 0x06, 0x55, 0x05, 0x08, 0x88, 0xCC, 0xDD, 0xDE, 0x7A, 0xBD, 0xB7, 0x80, 0xFF, 0x03, 0x00, 0x00, 0xFF, 0x01, 0x00, 0x00, 0x0A, 0x01, 0x01, 0x01, 0x00, 0x0D, 0x03, 0xAD, 0x3A, 0x27, 0x55, 0x05, 0x04, 0x48, 0xED, 0x9F, 0x80, 0xFF, 0x0F, 0x00, 0x00, 0x00, 0x00, 0x00, 0x40, 0x00, 0xF0, 0xFF, 0x00, 0xD0, 0xFF, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0xFF, 0x0F, 0x00, 0x00, 0x00, 0x00, 0x80, 0xBF, 0x00, 0x00, 0x00, 0x80, 0x00, 0x00, 0x80, 0xBF, 0x00, 0x00, 0x80, 0xBF, 0x00, 0x00, 0x00, 0x40, 0x0C, 0x01, 0x01, 0x01, 0x00, 0x03, 0x03, 0x01, 0x30, 0x01, 0x10, 0x05, 0x00, 0x60, 0x2C, 0xA4, 0x82, 0x59, 0x64, 0x91, 0x45, 0x16, 0x59, 0x64, 0x01, 0x00, 0x00, 0x00, 0x00, 0xFF, 0x0F, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x80, 0x3F, 0x0C, 0x01, 0x01, 0x01, 0x00, 0x0D, 0x03, 0x01, 0x10, 0x03, 0xAD, 0x02, 0x1F, 0x55, 0x2D, 0x06, 0x49, 0x12, 0x37, 0x02, 0x59, 0x80, 0x00, 0x00, 0xB1, 0xDA, 0xA9, 0x9D, 0x00, 0x90, 0x9D, 0x1C, 0xD9, 0x99, 0x9D, 0x00, 0xA0, 0x9D, 0x34, 0x2C, 0x00, 0x64, 0xA7, 0x76, 0x6A, 0x27, 0x00, 0xA0, 0x9D, 0x0F, 0x9B, 0x9D, 0xDA, 0xA9, 0x9D, 0x00, 0xC0, 0xC3, 0x6A, 0xA7, 0x76, 0x66, 0x67, 0x76, 0x02, 0x00, 0xDB, 0x99, 0x9D, 0xD9, 0xA9, 0x9D, 0xDA, 0x09, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0xFF, 0x0F, 0x00, 0x00, 0x3C, 0xE6, 0x84, 0x3B, 0x80, 0xBF, 0xB1, 0x3E, 0x30, 0x1B, 0x7E, 0x3F, 0x0C };

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
                while (!x.isDone)
                {
                    Thread.Sleep(100);
                }
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
                var decodeSettings = requireNormals ? DecodeSettings.RequireNormals : 0;
                decodeSettings |= requireTangents ? DecodeSettings.RequireTangents : 0;
                var task = DracoDecoder.DecodeMesh(
                    meshDataArray[i],
                    data,
                    decodeSettings
                    );
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
                var decodeSettings = requireNormals ? DecodeSettings.RequireNormals : 0;
                decodeSettings |= requireTangents ? DecodeSettings.RequireTangents : 0;
                var task = DracoDecoder.DecodeMesh(
                    data,
                    decodeSettings
                    );
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
        public IEnumerator EncodeMesh()
        {

            var gameObject = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            var mesh = gameObject.GetComponent<MeshFilter>().sharedMesh;

            var task = Encode.DracoEncoder.EncodeMesh(mesh);
            while (!task.IsCompleted)
            {
                yield return null;
            }

            var result = task.Result;

            Assert.NotNull(result);
            Assert.AreEqual(1, result.Length);
            Assert.AreEqual(7040, result[0].data.Length);

            result[0].Dispose();
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
            Assert.IsNull(result);
        }

        [UnityTest]
        public IEnumerator DecodeCube()
        {
            var task = DracoDecoder.DecodeMesh(k_CubeDracoData, DecodeSettings.Default);

            while (!task.IsCompleted)
            {
                yield return null;
            }

            var result = task.Result;
            Assert.IsNotNull(result);
            // Without an attribute id map, no tangents get decoded.
            Assert.AreEqual(0, result.tangents.Length);
        }

        [UnityTest]
        public IEnumerator DecodeCubeWithAttributeIdMap()
        {
            var attributeIdMap = new Dictionary<VertexAttribute, int>
            {
                [VertexAttribute.Position] = 0,
                [VertexAttribute.Normal] = 1,
                [VertexAttribute.Tangent] = 2,
                // Notice that the UV set order is reverted, so one can check against decoding without an attribute map.
                [VertexAttribute.TexCoord0] = 4,
                [VertexAttribute.TexCoord1] = 3,
            };

            var task = DracoDecoder.DecodeMesh(k_CubeDracoData, DecodeSettings.Default, attributeIdMap);

            while (!task.IsCompleted)
            {
                yield return null;
            }

            var result = task.Result;
            Assert.IsNotNull(result);
            Assert.AreEqual(24, result.tangents.Length);
        }

        [UnityTest]
        public IEnumerator DecodeCubeWithAttributeIdMapPartial()
        {
            var attributeIdMap = new Dictionary<VertexAttribute, int>
            {
                [VertexAttribute.Position] = 0,
                [VertexAttribute.Normal] = 1,
                [VertexAttribute.TexCoord0] = 3,
            };

            var task = DracoDecoder.DecodeMesh(k_CubeDracoData, DecodeSettings.Default, attributeIdMap);

            while (!task.IsCompleted)
            {
                yield return null;
            }

            var result = task.Result;
            Assert.IsNotNull(result);
            Assert.AreEqual(24, result.uv.Length);
            Assert.AreEqual(0, result.uv2.Length);
            Assert.AreEqual(0, result.tangents.Length);
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
