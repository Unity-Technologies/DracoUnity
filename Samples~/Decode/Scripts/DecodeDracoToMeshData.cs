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
            // Allocate single mesh data (you can/should bulk allocate multiple at once, if you're loading multiple
            // Draco meshes)
            var meshDataArray = Mesh.AllocateWritableMeshData(1);

            // DecodeSettings hold a couple of decode settings
            var decodeSettings = DecodeSettings.None;

            if (m_ConvertSpace)
            {
                // Coordinate space is converted from right-hand (like in glTF) to left-hand (Unity) by inverting the
                // x-axis.
                decodeSettings |= DecodeSettings.ConvertSpace;
            }

            if (m_RequireTangents)
            {
                // Ensures normal and tangent vertex attributes. If Draco data does not contain them, they are still
                // allocated and we have to calculate them afterwards (see below).
                decodeSettings |= DecodeSettings.RequireNormalsAndTangents;
            }
            else if (m_RequireNormals)
            {
                // Ensures normal vertex attribute. If Draco data does not normals, they are still allocated and we have
                // to calculate them afterwards (see below)
                decodeSettings |= DecodeSettings.RequireNormals;
            }

            // Async decoding has to start on the main thread and spawns multiple C# jobs.
            var result = await DracoDecoder.DecodeMesh(meshDataArray[0], m_DracoData.bytes, decodeSettings);

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
