// SPDX-FileCopyrightText: 2023 Unity Technologies and the Draco for Unity authors
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine.Rendering;

namespace Draco.Encode
{
    /// <summary>
    /// Contains encoded data and additional meta information.
    /// The responsibility to dispose this struct and the native resources behind it (via <see cref="Dispose"/>)
    /// is handed over to the receiver.
    /// </summary>
    public unsafe struct EncodeResult : IDisposable
    {

        /// <summary>Number of triangle indices</summary>
        public readonly uint indexCount;
        /// <summary>Number vertices</summary>
        public readonly uint vertexCount;
        /// <summary>Encoded data</summary>
        public readonly NativeArray<byte> data;
        /// <summary>Vertex attribute to Draco property ID mapping</summary>
        public readonly Dictionary<VertexAttribute, (uint identifier, int dimensions)> vertexAttributes;

        IntPtr m_DracoEncoder;

#if ENABLE_UNITY_COLLECTIONS_CHECKS
        readonly AtomicSafetyHandle m_SafetyHandle;
#endif

        /// <summary>
        /// Constructs an EncodeResult.
        /// </summary>
        /// <param name="dracoEncoder">Native Draco encoder instance.</param>
        /// <param name="indexCount">Number of indices.</param>
        /// <param name="vertexCount">Number of vertices.</param>
        /// <param name="vertexAttributes">For each vertex attribute type there's a tuple containing
        /// the draco identifier and the attribute dimensions (e.g. 3 for 3D positions).</param>
        public EncodeResult(
            IntPtr dracoEncoder,
            uint indexCount,
            uint vertexCount,
            Dictionary<VertexAttribute, (uint identifier, int dimensions)> vertexAttributes
        )
        {
            m_DracoEncoder = dracoEncoder;
            this.indexCount = indexCount;
            this.vertexCount = vertexCount;
            DracoEncoder.dracoEncoderGetEncodeBuffer(m_DracoEncoder, out var dracoData, out var size);
            data = NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray<byte>(dracoData, (int)size, Allocator.None);
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            m_SafetyHandle = AtomicSafetyHandle.Create();
            NativeArrayUnsafeUtility.SetAtomicSafetyHandle(array: ref data, m_SafetyHandle);
#endif
            this.vertexAttributes = vertexAttributes;
        }

        /// <summary>
        /// Releases allocated resources.
        /// </summary>
        public void Dispose()
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle.Release(m_SafetyHandle);
#endif
            if (m_DracoEncoder != IntPtr.Zero)
                DracoEncoder.dracoEncoderRelease(m_DracoEncoder);
            m_DracoEncoder = IntPtr.Zero;
        }
    }
}
