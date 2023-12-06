// SPDX-FileCopyrightText: 2023 Unity Technologies and the Draco for Unity authors
// SPDX-License-Identifier: Apache-2.0

using UnityEngine;

namespace Draco.Sample.Decode
{

    [RequireComponent(typeof(MeshFilter))]
    [RequireComponent(typeof(MeshRenderer))]
    public class DecodeDracoToMeshData : MonoBehaviour
    {
        #region LoadDraco
        [SerializeField]
        TextAsset m_DracoData;

        [SerializeField]
        bool m_ConvertSpace;

        [SerializeField]
        bool m_RequireNormals;

        [SerializeField]
        bool m_RequireTangents;

        async void Start()
        {
            // Allocate single mesh data (you can/should bulk allocate multiple at once, if you're loading multiple draco meshes)
            var meshDataArray = Mesh.AllocateWritableMeshData(1);

            // Async decoding has to start on the main thread and spawns multiple C# jobs.
            var result = await DracoDecoder.DecodeMesh(
                meshDataArray[0],
                m_DracoData.bytes,
                convertSpace: m_ConvertSpace,
                requireNormals: m_RequireNormals, // Set to true if you require normals. If Draco data does not contain them, they are allocated and we have to calculate them below
                requireTangents: m_RequireTangents // Retrieve tangents is not supported, but this will ensure they are allocated and can be calculated later (see below)
                );

            if (result.success)
            {

                // Apply onto new Mesh
                var mesh = new Mesh();
                Mesh.ApplyAndDisposeWritableMeshData(meshDataArray, mesh);

                // If Draco mesh has bone weights, apply them now.
                // To get these, you have to supply the correct attribute IDs
                // to `ConvertDracoMeshToUnity` above (optional parameters).
                if (result.boneWeightData != null)
                {
                    result.boneWeightData.ApplyOnMesh(mesh);
                    result.boneWeightData.Dispose();
                }

                if (m_RequireNormals && result.calculateNormals)
                {
                    // If draco didn't contain normals, calculate them.
                    mesh.RecalculateNormals();
                }
                if (m_RequireTangents && m_RequireTangents)
                {
                    // If required (e.g. for consistent specular shading), calculate tangents
                    mesh.RecalculateTangents();
                }

                // Use the resulting mesh
                GetComponent<MeshFilter>().mesh = mesh;
            }
        }
        #endregion LoadDraco
    }
}
