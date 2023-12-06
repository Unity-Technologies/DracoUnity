// SPDX-FileCopyrightText: 2023 Unity Technologies and the Draco for Unity authors
// SPDX-License-Identifier: Apache-2.0

using System.IO;
using Draco.Encode;
using UnityEngine;
using UnityEngine.Assertions;

namespace Draco.Sample.Encode
{

    [RequireComponent(typeof(MeshFilter))]
    public class EncodeMeshToDraco : MonoBehaviour
    {
        async void Start()
        {
            #region EncodeDraco
            var meshFilter = GetComponent<MeshFilter>();
            Assert.IsNotNull(meshFilter, "Couldn't find MeshFilter component");
            var mesh = meshFilter.sharedMesh; // Use sharedMesh, so no copy of the Mesh is created implicitly.

            // Encode to Draco
            var encodeResults = await DracoEncoder.EncodeMesh(mesh);

            if (encodeResults == null)
            {
                Debug.LogError("Encoding Draco failed!");
                return;
            }

            var meshName = mesh.name;
            if (string.IsNullOrEmpty(meshName))
            {
                meshName = "Mesh";
            }

            for (var i = 0; i < encodeResults.Length; i++)
            {
                var encodeResult = encodeResults[i];
                var destination = Path.Combine(Application.persistentDataPath, $"{meshName}-submesh-{i}.drc");
                File.WriteAllBytes(destination, encodeResult.data.ToArray());
                Debug.Log($"Saved submesh {i} to {destination}");
                // It's required to dispose the results
                encodeResult.Dispose();
            }
            #endregion EncodeDraco
        }
    }
}
