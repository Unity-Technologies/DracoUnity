// SPDX-FileCopyrightText: 2023 Unity Technologies and the Draco for Unity authors
// SPDX-License-Identifier: Apache-2.0

using UnityEngine;

namespace Draco
{
    /// <summary>
    /// Holds the result of the Draco decoding process.
    /// </summary>
    public readonly struct DecodeResult
    {
        /// <summary>
        /// True if the decoding was successful
        /// </summary>
        public readonly bool success;

        /// <summary>
        /// Axis aligned bounding box of the mesh/point cloud.
        /// </summary>
        public readonly Bounds bounds;

        /// <summary>
        /// True, if the normals were marked required, but not present in Draco mesh.
        /// They have to get calculated.
        /// </summary>
        public readonly bool calculateNormals;

        /// <summary>
        /// If the Draco file contained bone indices and bone weights,
        /// this property is used to carry them over (since MeshData currently
        /// provides no way to apply those values)
        /// </summary>
        public readonly BoneWeightData boneWeightData;

        /// <summary>
        /// Constructs a DecodeResult with values.
        /// </summary>
        /// <param name="success">Depicts if the decoding was successful.</param>
        /// <param name="bounds">Axis aligned bounding box of the mesh/point cloud.</param>
        /// <param name="calculateNormals">Depicts if the normals still have to be calculated.</param>
        /// <param name="boneWeightData">Bone indices and weights.</param>
        public DecodeResult(
            bool success,
            Bounds bounds,
            bool calculateNormals,
            BoneWeightData boneWeightData
            )
        {
            this.success = success;
            this.bounds = bounds;
            this.calculateNormals = calculateNormals;
            this.boneWeightData = boneWeightData;
        }
    }
}
