// SPDX-FileCopyrightText: 2023 Unity Technologies and the Draco for Unity authors
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using NUnit.Framework;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.Rendering;
using UnityEngine.TestTools;

namespace Draco.Tests
{

    [TestFixture]
    class DracoRuntimeTests
    {

        const string k_URLPrefix = "https://raw.githubusercontent.com/google/draco/master/testdata/";

        static readonly string[] k_TestDataUrls = {
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

            // // Legacy versions not supported
            // "cube_pc.drc",
            // "pc_color.drc",
            // "test_nm.obj.edgebreaker.0.10.0.drc",
            // "test_nm.obj.edgebreaker.0.9.1.drc",
            // "test_nm.obj.edgebreaker.1.0.0.drc",
            // "test_nm.obj.edgebreaker.1.1.0.drc",
            // "test_nm.obj.sequential.0.10.0.drc",
            // "test_nm.obj.sequential.0.9.1.drc",
            // "test_nm.obj.sequential.1.0.0.drc",
            // "test_nm.obj.sequential.1.1.0.drc",
            // "test_nm_quant.0.9.0.drc",

            // // Unknown why it does not work
            // "cube_att.drc",
        };

        static Dictionary<string, NativeArray<byte>> s_TestData;

        [UnitySetUp]
        public IEnumerator OneTimeSetup()
        {
            if (s_TestData == null)
            {
                var task = LoadAllUrls();
                while (!task.IsCompleted)
                {
                    yield return null;
                }
            }
        }

        static async Task LoadAllUrls()
        {
            s_TestData = new Dictionary<string, NativeArray<byte>>(k_TestDataUrls.Length);

            foreach (var url in k_TestDataUrls)
            {
                var webRequest = UnityWebRequest.Get(k_URLPrefix + url);
                var x = webRequest.SendWebRequest();
                while (!x.isDone)
                {
                    await Task.Yield();
                }
                if (!string.IsNullOrEmpty(webRequest.error))
                {
                    Debug.LogErrorFormat("Error loading {0}: {1}", url, webRequest.error);
                    return;
                }

                s_TestData[url] = new NativeArray<byte>(webRequest.downloadHandler.data, Allocator.Persistent);
            }
        }

        [OneTimeTearDown]
        public void OneTimeTearDown()
        {
            foreach (var set in s_TestData)
            {
                set.Value.Dispose();
            }
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
            yield return RunTest(url, LoadBatchToMeshData, true);
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
            yield return RunTest(url, LoadBatchToMeshData, true, true);
        }

        static IEnumerator RunTest(string url, Func<int, NativeArray<byte>, bool, bool, Task> loadBatchFunc, bool requireNormals = false, bool requireTangents = false)
        {
            var data = s_TestData[url];
            var task = loadBatchFunc(1, data, requireNormals, requireTangents);
            while (!task.IsCompleted)
            {
                yield return null;
            }
            Assert.IsNull(task.Exception);
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
                    Debug.LogError("Loading mesh failed");
                    return;
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
