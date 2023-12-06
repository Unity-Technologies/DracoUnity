// SPDX-FileCopyrightText: 2023 Unity Technologies and the Draco for Unity authors
// SPDX-License-Identifier: Apache-2.0

using UnityEngine;

namespace Draco.Sample.Decode
{

    [RequireComponent(typeof(MeshFilter))]
    [RequireComponent(typeof(MeshRenderer))]
    public class DecodeDracoToMesh : MonoBehaviour
    {
        #region LoadDraco
        [SerializeField]
        TextAsset m_DracoData;

        async void Start()
        {
            // Async decoding has to start on the main thread and spawns multiple C# jobs.
            var mesh = await DracoDecoder.DecodeMesh(m_DracoData.bytes);

            if (mesh != null)
            {
                // Use the resulting mesh
                GetComponent<MeshFilter>().mesh = mesh;
            }
        }
        #endregion LoadDraco
    }
}
