// SPDX-FileCopyrightText: 2023 Unity Technologies and the Draco for Unity authors
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Threading.Tasks;
using Unity.Collections;
using UnityEngine;

namespace Draco
{
    /// <summary>
    /// Obsolete! Please use <see cref="DracoDecoder"/> instead. Provides Draco mesh decoding.
    /// </summary>
    /// <seealso cref="DracoDecoder"/>
    [Obsolete("Use DracoDecoder.DecodeMesh methods instead.")]
    public class DracoMeshLoader
    {
        /// <summary>
        /// If true, coordinate space is converted from right-hand (like in glTF) to left-hand (Unity).
        /// </summary>
        readonly bool m_ConvertSpace;

        /// <summary>
        /// Create a DracoMeshLoader instance which let's you decode Draco data.
        /// </summary>
        /// <param name="convertSpace">If true, coordinate space is converted from right-hand (like in glTF) to left-hand (Unity).</param>
        public DracoMeshLoader(bool convertSpace = true)
        {
            m_ConvertSpace = convertSpace;
        }

        /// <summary>
        /// Decodes a Draco mesh
        /// </summary>
        /// <param name="encodedData">Compressed Draco data</param>
        /// <param name="requireNormals">If draco does not contain normals and this is set to true, normals are calculated.</param>
        /// <param name="requireTangents">If draco does not contain tangents and this is set to true, tangents and normals are calculated.</param>
        /// <param name="weightsAttributeId">Draco attribute ID that contains bone weights (for skinning)</param>
        /// <param name="jointsAttributeId">Draco attribute ID that contains bone joint indices (for skinning)</param>
        /// <param name="forceUnityLayout">Enforces vertex buffer layout with highest compatibility. Enable this if you want to use blend shapes on the resulting mesh</param>
        /// <returns>Unity Mesh or null in case of errors</returns>
        /// <seealso cref="Draco.DracoDecoder.DecodeMesh(NativeSlice{byte})"/>
        [Obsolete("Use DracoDecoder.DecodeMesh instead.")]
        public async Task<Mesh> ConvertDracoMeshToUnity(
            NativeSlice<byte> encodedData,
            bool requireNormals = false,
            bool requireTangents = false,
            int weightsAttributeId = -1,
            int jointsAttributeId = -1,
            bool forceUnityLayout = false
            )
        {
            return await DracoDecoder.DecodeMesh(
                encodedData,
                CreateDecodeSettings(requireNormals, requireTangents, forceUnityLayout),
                DracoDecoder.CreateAttributeIdMap(weightsAttributeId, jointsAttributeId)
                );
        }

        /// <summary>
        /// Decodes a Draco mesh
        /// </summary>
        /// <param name="encodedData">Compressed Draco data</param>
        /// <param name="requireNormals">If draco does not contain normals and this is set to true, normals are calculated.</param>
        /// <param name="requireTangents">If draco does not contain tangents and this is set to true, tangents and normals are calculated.</param>
        /// <param name="weightsAttributeId">Draco attribute ID that contains bone weights (for skinning)</param>
        /// <param name="jointsAttributeId">Draco attribute ID that contains bone joint indices (for skinning)</param>
        /// <param name="forceUnityLayout">Enforces vertex buffer layout with highest compatibility. Enable this if you want to use blend shapes on the resulting mesh</param>
        /// <returns>Unity Mesh or null in case of errors</returns>
        /// <seealso cref="Draco.DracoDecoder.DecodeMesh(byte[])"/>
        [Obsolete("Use DracoDecoder.DecodeMesh instead.")]
        public async Task<Mesh> ConvertDracoMeshToUnity(
            byte[] encodedData,
            bool requireNormals = false,
            bool requireTangents = false,
            int weightsAttributeId = -1,
            int jointsAttributeId = -1,
            bool forceUnityLayout = false
        )
        {
            return await DracoDecoder.DecodeMesh(
                encodedData,
                CreateDecodeSettings(requireNormals, requireTangents, forceUnityLayout),
                DracoDecoder.CreateAttributeIdMap(weightsAttributeId, jointsAttributeId)
            );
        }

        /// <summary>
        /// Decodes a Draco mesh
        /// </summary>
        /// <param name="mesh">MeshData used to create the mesh</param>
        /// <param name="encodedData">Compressed Draco data</param>
        /// <param name="requireNormals">If draco does not contain normals and this is set to true, normals are calculated.</param>
        /// <param name="requireTangents">If draco does not contain tangents and this is set to true, tangents and normals are calculated.</param>
        /// <param name="weightsAttributeId">Draco attribute ID that contains bone weights (for skinning)</param>
        /// <param name="jointsAttributeId">Draco attribute ID that contains bone joint indices (for skinning)</param>
        /// <param name="forceUnityLayout">Enforces vertex buffer layout with highest compatibility. Enable this if you want to use blend shapes on the resulting mesh</param>
        /// <returns>A DecodeResult</returns>
        /// <seealso cref="Draco.DracoDecoder.DecodeMesh(Mesh.MeshData,byte[])"/>
        [Obsolete("Use DracoDecoder.DecodeMesh instead.")]
        public async Task<DecodeResult> ConvertDracoMeshToUnity(
            Mesh.MeshData mesh,
            byte[] encodedData,
            bool requireNormals = false,
            bool requireTangents = false,
            int weightsAttributeId = -1,
            int jointsAttributeId = -1,
            bool forceUnityLayout = false
            )
        {
            return await DracoDecoder.DecodeMesh(
                mesh,
                encodedData,
                CreateDecodeSettings(requireNormals, requireTangents, forceUnityLayout),
                DracoDecoder.CreateAttributeIdMap(weightsAttributeId, jointsAttributeId)
            );
        }

        /// <summary>
        /// Decodes a Draco mesh
        /// </summary>
        /// <param name="mesh">MeshData used to create the mesh</param>
        /// <param name="encodedData">Compressed Draco data</param>
        /// <param name="requireNormals">If draco does not contain normals and this is set to true, normals are calculated.</param>
        /// <param name="requireTangents">If draco does not contain tangents and this is set to true, tangents and normals are calculated.</param>
        /// <param name="weightsAttributeId">Draco attribute ID that contains bone weights (for skinning)</param>
        /// <param name="jointsAttributeId">Draco attribute ID that contains bone joint indices (for skinning)</param>
        /// <param name="forceUnityLayout">Enforces vertex buffer layout with highest compatibility. Enable this if you want to use blend shapes on the resulting mesh</param>
        /// <returns>A DecodeResult</returns>
        /// <seealso cref="Draco.DracoDecoder.DecodeMesh(Mesh.MeshData,NativeSlice{byte})"/>
        [Obsolete("Use DracoDecoder.DecodeMesh instead.")]
        public async Task<DecodeResult> ConvertDracoMeshToUnity(
            Mesh.MeshData mesh,
            NativeArray<byte> encodedData,
            bool requireNormals = false,
            bool requireTangents = false,
            int weightsAttributeId = -1,
            int jointsAttributeId = -1,
            bool forceUnityLayout = false
        )
        {
            return await DracoDecoder.DecodeMesh(
                mesh,
                encodedData,
                CreateDecodeSettings(requireNormals, requireTangents, forceUnityLayout),
                DracoDecoder.CreateAttributeIdMap(weightsAttributeId, jointsAttributeId)
            );
        }

        DecodeSettings CreateDecodeSettings(bool requireNormals, bool requireTangents, bool forceUnityLayout)
        {
            var flags = DecodeSettings.None;
            if (requireNormals) flags |= DecodeSettings.RequireNormals;
            if (requireTangents) flags |= DecodeSettings.RequireTangents;
            if (forceUnityLayout) flags |= DecodeSettings.ForceUnityVertexLayout;
            if (m_ConvertSpace) flags |= DecodeSettings.ConvertSpace;
            return flags;
        }
    }
}
