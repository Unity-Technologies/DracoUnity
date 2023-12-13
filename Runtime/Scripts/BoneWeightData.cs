// SPDX-FileCopyrightText: 2023 Unity Technologies and the Draco for Unity authors
// SPDX-License-Identifier: Apache-2.0

using System;
using Unity.Collections;
using UnityEngine;

namespace Draco
{
    /// <summary>
    /// Draco encoded meshes might contain bone weights and indices that cannot be applied to the resulting Unity
    /// mesh right away. This class provides them and offers methods to apply them to Unity meshes.
    /// </summary>
    public sealed class BoneWeightData : IDisposable
    {
        NativeArray<byte> m_BonesPerVertex;
        NativeArray<BoneWeight1> m_BoneWeights;

        /// <summary>
        /// Constructs an object with parameters identical to <see cref="Mesh.SetBoneWeights"/>.
        /// </summary>
        /// <param name="bonesPerVertex">Bones per vertex </param>
        /// <param name="boneWeights">Bone weights</param>
        /// <seealso cref="Mesh.SetBoneWeights"/>
        public BoneWeightData(NativeArray<byte> bonesPerVertex, NativeArray<BoneWeight1> boneWeights)
        {
            m_BonesPerVertex = bonesPerVertex;
            m_BoneWeights = boneWeights;
        }

        /// <summary>
        /// Applies the bone weights and indices on a Unity mesh.
        /// </summary>
        /// <param name="mesh">The mesh to apply the data onto.</param>
        public void ApplyOnMesh(Mesh mesh)
        {
            mesh.SetBoneWeights(m_BonesPerVertex, m_BoneWeights);
        }

        /// <summary>
        /// Releases allocated resources.
        /// </summary>
        public void Dispose()
        {
            m_BonesPerVertex.Dispose();
            m_BoneWeights.Dispose();
        }
    }
}
