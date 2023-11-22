// SPDX-FileCopyrightText: 2023 Unity Technologies and the Draco for Unity authors
// SPDX-License-Identifier: Apache-2.0

using UnityEngine;

namespace Draco
{
    /// <summary>
    /// Holds the result of the Draco decoding process.
    /// </summary>
    public struct DecodeResult
    {
        /// <summary>
        /// True if the decoding was successful
        /// </summary>
        public bool success;

        /// <summary>
        /// Axis aligned bounding box of the mesh/point cloud.
        /// </summary>
        public Bounds bounds;

        /// <summary>
        /// True, if the normals were marked required, but not present in Draco mesh.
        /// They have to get calculated.
        /// </summary>
        public bool calculateNormals;

        /// <summary>
        /// If the Draco file contained bone indices and bone weights,
        /// this property is used to carry them over (since MeshData currently
        /// provides no way to apply those values)
        /// </summary>
        public BoneWeightData boneWeightData;
    }
}
