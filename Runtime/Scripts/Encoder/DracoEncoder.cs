// SPDX-FileCopyrightText: 2023 Unity Technologies and the Draco for Unity authors
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.Rendering;

namespace Draco.Encode
{
    /// <summary>
    /// Provides Draco encoding capabilities.
    /// </summary>
    public static class DracoEncoder
    {
        struct AttributeData
        {
            public int stream;
            public int offset;
        }

        /// <summary>
        /// Applies Draco compression to a given mesh and returns the encoded result (one per sub-mesh)
        /// </summary>
        /// <param name="unityMesh">Input mesh</param>
        /// <returns>Encoded data (one per sub-mesh)</returns>
        public static async Task<EncodeResult[]> EncodeMesh(Mesh unityMesh)
        {
            return await EncodeMesh(unityMesh, QuantizationSettings.Default, SpeedSettings.Default);
        }

        /// <inheritdoc cref="EncodeMesh(UnityEngine.Mesh)"/>
        /// <param name="quantization">Quantization settings</param>
        /// <param name="speed">Encode/decode speed settings</param>
        // ReSharper disable once MemberCanBePrivate.Global
        public static async Task<EncodeResult[]> EncodeMesh(
            Mesh unityMesh,
            QuantizationSettings quantization,
            SpeedSettings speed
        )
        {
            DracoDecoder.CertifySupportedPlatform(
#if UNITY_EDITOR
                false
#endif
            );
#if !UNITY_EDITOR
            if (!unityMesh.isReadable)
            {
                Debug.LogError("Mesh is not readable");
                return null;
            }
#endif
            var dataArray = Mesh.AcquireReadOnlyMeshData(unityMesh);
            var data = dataArray[0];

            var result = await EncodeMesh(unityMesh, data, quantization, speed);

            dataArray.Dispose();
            return result;
        }

        /// <summary>
        /// Applies Draco compression to a given mesh/meshData and returns the encoded result (one per sub-mesh).
        /// The user is responsible for
        /// <see cref="UnityEngine.Mesh.AcquireReadOnlyMeshData(Mesh)">acquiring the readable MeshData</see>
        /// and disposing it.
        /// </summary>
        /// <param name="mesh">Input mesh</param>
        /// <param name="meshData">Previously acquired readable mesh data</param>
        /// <returns>Encoded data (one per sub-mesh)</returns>
        // ReSharper disable once MemberCanBePrivate.Global
        public static async Task<EncodeResult[]> EncodeMesh(Mesh mesh, Mesh.MeshData meshData)
        {
            return await EncodeMesh(mesh, meshData, QuantizationSettings.Default, SpeedSettings.Default);
        }

        /// <inheritdoc cref="EncodeMesh(Mesh,Mesh.MeshData)"/>
        /// <param name="quantization">Quantization settings</param>
        /// <param name="speed">Encode/decode speed settings</param>
        // ReSharper disable once MemberCanBePrivate.Global
        public static async Task<EncodeResult[]> EncodeMesh(
            Mesh mesh,
            Mesh.MeshData meshData,
            QuantizationSettings quantization,
            SpeedSettings speed
        )
        {
#if !UNITY_EDITOR
            if (!mesh.isReadable)
            {
                Debug.LogError("Mesh is not readable");
                return null;
            }
#endif

            if (!quantization.IsValid)
            {
                // Could be ill-configured or accidental use of QuantizationSettings's implicit default constructor.
                Debug.LogError($"Invalid {quantization}! Falling back to Default");
                quantization = QuantizationSettings.Default;
            }
            Profiler.BeginSample("EncodeMesh.Prepare");

            var result = new EncodeResult[meshData.subMeshCount];
            var vertexAttributes = mesh.GetVertexAttributes();

            var strides = new int[DracoNative.maxStreamCount];
            var attributeDataDict = new Dictionary<VertexAttribute, AttributeData>();

            foreach (var attribute in vertexAttributes)
            {
                var attributeData = new AttributeData { offset = strides[attribute.stream], stream = attribute.stream };
                var size = attribute.dimension * GetAttributeSize(attribute.format);
                strides[attribute.stream] += size;
                attributeDataDict[attribute.attribute] = attributeData;
            }

            var streamCount = 1;
            for (var stream = 0; stream < strides.Length; stream++)
            {
                var stride = strides[stream];
                if (stride <= 0) continue;
                streamCount = stream + 1;
            }

            var vData = new NativeArray<byte>[streamCount];
            for (var stream = 0; stream < streamCount; stream++)
            {
                vData[stream] = meshData.GetVertexData<byte>(stream);
            }

            var vDataPtr = GetReadOnlyPointers(streamCount, vData);
            Profiler.EndSample(); // EncodeMesh.Prepare

            for (var subMeshIndex = 0; subMeshIndex < mesh.subMeshCount; subMeshIndex++)
            {

                Profiler.BeginSample("EncodeMesh.SubMesh.Prepare");
                var subMesh = mesh.GetSubMesh(subMeshIndex);

                if (subMesh.topology != MeshTopology.Triangles && subMesh.topology != MeshTopology.Points)
                {
                    Debug.LogError($"Mesh topology {subMesh.topology} is not supported");
                    return null;
                }

                var dracoEncoder = subMesh.topology == MeshTopology.Triangles
                    ? dracoEncoderCreate(mesh.vertexCount)
                    : dracoEncoderCreatePointCloud(mesh.vertexCount);

                var attributeIds = new Dictionary<VertexAttribute, (uint identifier, int dimensions)>();

                foreach (var attributeTuple in attributeDataDict)
                {
                    var attribute = attributeTuple.Key;
                    var attrData = attributeTuple.Value;
                    var format = mesh.GetVertexAttributeFormat(attribute);
                    var dimension = mesh.GetVertexAttributeDimension(attribute);
                    var stride = strides[attrData.stream];
                    var baseAddr = vDataPtr[attrData.stream] + attrData.offset;
                    var id = dracoEncoderSetAttribute(
                        dracoEncoder,
                        (int)GetAttributeType(attribute),
                        GetDataType(format),
                        dimension,
                        stride,
                        DracoNative.ConvertSpace(attribute),
                        baseAddr
                        );
                    attributeIds[attribute] = (id, dimension);
                }

                if (subMesh.topology == MeshTopology.Triangles)
                {
                    var indices = mesh.GetIndices(subMeshIndex);
                    var indicesData = PinArray(indices, out var gcHandle);
                    dracoEncoderSetIndices(
                        dracoEncoder,
                        DataType.UInt32,
                        (uint)indices.Length,
                        true,
                        indicesData
                        );
                    UnsafeUtility.ReleaseGCObject(gcHandle);
                }

                // For both encoding and decoding (0 = slow and best compression; 10 = fast)
                dracoEncoderSetCompressionSpeed(
                    dracoEncoder,
                    speed.encodingSpeed,
                    speed.decodingSpeed
                );
                dracoEncoderSetQuantizationBits(
                    dracoEncoder,
                    quantization.positionQuantization,
                    quantization.normalQuantization,
                    quantization.texCoordQuantization,
                    quantization.colorQuantization,
                    QuantizationSettings.genericQuantization
                );

                var encodeJob = new EncodeJob
                {
                    dracoEncoder = dracoEncoder
                };

                Profiler.EndSample(); //EncodeMesh.SubMesh.Prepare

                var jobHandle = encodeJob.Schedule();
                while (!jobHandle.IsCompleted)
                {
                    await Task.Yield();
                }
                jobHandle.Complete();

                Profiler.BeginSample("EncodeMesh.SubMesh.Aftermath");

                result[subMeshIndex] = new EncodeResult(
                    dracoEncoder,
                    dracoEncoderGetEncodedIndexCount(dracoEncoder),
                    dracoEncoderGetEncodedVertexCount(dracoEncoder),
                    attributeIds
                );

                Profiler.EndSample(); // EncodeMesh.SubMesh.Aftermath
            }

            Profiler.BeginSample("EncodeMesh.Aftermath");
            for (var stream = 0; stream < streamCount; stream++)
            {
                vData[stream].Dispose();
            }

            Profiler.EndSample();
            return result;
        }

        /// <summary>
        /// Applies Draco compression to a given mesh and returns the encoded result (one per sub-mesh)
        /// The quality and quantization parameters are calculated from the mesh's bounds, its worldScale and desired precision.
        /// The quantization parameters help to find a balance between compressed size and quality / precision.
        /// </summary>
        /// <param name="unityMesh">Input mesh</param>
        /// <param name="worldScale">Local-to-world scale this mesh is present in the scene</param>
        /// <param name="precision">Desired minimum precision in world units</param>
        /// <param name="encodingSpeed">Encoding speed level. 0 means slow and small. 10 is fastest.</param>
        /// <param name="decodingSpeed">Decoding speed level. 0 means slow and small. 10 is fastest.</param>
        /// <param name="normalQuantization">Normal quantization</param>
        /// <param name="texCoordQuantization">Texture coordinate quantization</param>
        /// <param name="colorQuantization">Color quantization</param>
        /// <param name="genericQuantization">Generic quantization (e.g. blend weights and indices). unused at the moment</param>
        /// <returns>Encoded data (one per sub-mesh)</returns>
        /// <seealso cref="EncodeMesh(Mesh,QuantizationSettings,SpeedSettings)"/>
        /// <seealso cref="QuantizationSettings.FromWorldSize"/>
        [Obsolete("Use EncodeMesh(Mesh,QuantizationSettings,SpeedSettings) instead")]
        public static async Task<EncodeResult[]> EncodeMesh(
            Mesh unityMesh,
            Vector3 worldScale,
            float precision = .001f,
            int encodingSpeed = 0,
            int decodingSpeed = 4,
            int normalQuantization = 10,
            int texCoordQuantization = 12,
            int colorQuantization = 8,
            int genericQuantization = 12
            )
        {
            return await EncodeMesh(
                unityMesh,
                QuantizationSettings.FromWorldSize(
                    unityMesh.bounds,
                    worldScale,
                    precision,
                    normalQuantization,
                    texCoordQuantization,
                    colorQuantization
                ),
                new SpeedSettings(encodingSpeed, decodingSpeed)
            );
        }

        /// <summary>
        /// Applies Draco compression to a given mesh/meshData and returns the encoded result (one per sub-mesh)
        /// The user is responsible for
        /// <see cref="UnityEngine.Mesh.AcquireReadOnlyMeshData(Mesh)">acquiring the readable MeshData</see>
        /// and disposing it.
        /// The quality and quantization parameters are calculated from the mesh's bounds, its worldScale and desired precision.
        /// The quantization parameters help to find a balance between compressed size and quality / precision.
        /// </summary>
        /// <param name="mesh">Input mesh</param>
        /// <param name="meshData">Previously acquired readable mesh data</param>
        /// <param name="worldScale">Local-to-world scale this mesh is present in the scene</param>
        /// <param name="precision">Desired minimum precision in world units</param>
        /// <param name="encodingSpeed">Encoding speed level. 0 means slow and small. 10 is fastest.</param>
        /// <param name="decodingSpeed">Decoding speed level. 0 means slow and small. 10 is fastest.</param>
        /// <param name="normalQuantization">Normal quantization</param>
        /// <param name="texCoordQuantization">Texture coordinate quantization</param>
        /// <param name="colorQuantization">Color quantization</param>
        /// <param name="genericQuantization">Generic quantization (e.g. blend weights and indices). unused at the moment</param>
        /// <returns>Encoded data (one per sub-mesh)</returns>
        /// <seealso cref="EncodeMesh(Mesh,Mesh.MeshData,QuantizationSettings,SpeedSettings)"/>
        /// <seealso cref="QuantizationSettings.FromWorldSize"/>
        [Obsolete("Use EncodeMesh(Mesh,Mesh.MeshData,QuantizationSettings,SpeedSettings) instead")]
        public static async Task<EncodeResult[]> EncodeMesh(
            Mesh mesh,
            Mesh.MeshData meshData,
            Vector3 worldScale,
            float precision = .001f,
            int encodingSpeed = 0,
            int decodingSpeed = 4,
            int normalQuantization = 10,
            int texCoordQuantization = 12,
            int colorQuantization = 8,
            int genericQuantization = 12
            )
        {
            return await EncodeMesh(
                mesh,
                meshData,
                QuantizationSettings.FromWorldSize(
                    mesh.bounds,
                    worldScale,
                    precision,
                    normalQuantization,
                    texCoordQuantization,
                    colorQuantization
                    ),
                new SpeedSettings(encodingSpeed, decodingSpeed)
                );
        }

        /// <summary>
        /// Applies Draco compression to a given mesh and returns the encoded result (one per sub-mesh)
        /// The quantization parameters help to find a balance between encoded size and quality / precision.
        /// </summary>
        /// <param name="unityMesh">Input mesh</param>
        /// <param name="encodingSpeed">Encoding speed level. 0 means slow and small. 10 is fastest.</param>
        /// <param name="decodingSpeed">Decoding speed level. 0 means slow and small. 10 is fastest.</param>
        /// <param name="positionQuantization">Vertex position quantization</param>
        /// <param name="normalQuantization">Normal quantization</param>
        /// <param name="texCoordQuantization">Texture coordinate quantization</param>
        /// <param name="colorQuantization">Color quantization</param>
        /// <param name="genericQuantization">Generic quantization (e.g. blend weights and indices). unused at the moment</param>
        /// <returns>Encoded data (one per sub-mesh)</returns>
        /// <seealso cref="EncodeMesh(Mesh,QuantizationSettings,SpeedSettings)"/>
        [Obsolete("Use EncodeMesh(Mesh,QuantizationSettings,SpeedSettings) instead")]
        public static async Task<EncodeResult[]> EncodeMesh(
            Mesh unityMesh,
            // ReSharper disable once MethodOverloadWithOptionalParameter
            int encodingSpeed = 0,
            int decodingSpeed = 4,
            int positionQuantization = 14,
            int normalQuantization = 10,
            int texCoordQuantization = 12,
            int colorQuantization = 8,
            int genericQuantization = 12
        )
        {
            return await EncodeMesh(
                unityMesh,
                new QuantizationSettings(
                    positionQuantization: positionQuantization,
                    normalQuantization: normalQuantization,
                    texCoordQuantization: texCoordQuantization,
                    colorQuantization: colorQuantization
                ),
                new SpeedSettings(encodingSpeed, decodingSpeed));
        }

        /// <summary>
        /// Applies Draco compression to a given mesh/meshData and returns the encoded result (one per sub-mesh)
        /// The user is responsible for
        /// <see cref="UnityEngine.Mesh.AcquireReadOnlyMeshData(Mesh)">acquiring the readable MeshData</see>
        /// and disposing it.
        /// The quantization parameters help to find a balance between encoded size and quality / precision.
        /// </summary>
        /// <param name="mesh">Input mesh</param>
        /// <param name="meshData">Previously acquired readable mesh data</param>
        /// <param name="encodingSpeed">Encoding speed level. 0 means slow and small. 10 is fastest.</param>
        /// <param name="decodingSpeed">Decoding speed level. 0 means slow and small. 10 is fastest.</param>
        /// <param name="positionQuantization">Vertex position quantization</param>
        /// <param name="normalQuantization">Normal quantization</param>
        /// <param name="texCoordQuantization">Texture coordinate quantization</param>
        /// <param name="colorQuantization">Color quantization</param>
        /// <param name="genericQuantization">Generic quantization (e.g. blend weights and indices). unused at the moment</param>
        /// <returns>Encoded data (one per sub-mesh)</returns>
        /// <seealso cref="EncodeMesh(Mesh,Mesh.MeshData,QuantizationSettings,SpeedSettings)"/>
        // ReSharper disable once MemberCanBePrivate.Global
        [Obsolete("Use EncodeMesh(Mesh,Mesh.MeshData,QuantizationSettings,SpeedSettings) instead")]
        public static async Task<EncodeResult[]> EncodeMesh(
            Mesh mesh,
            Mesh.MeshData meshData,
            // ReSharper disable once MethodOverloadWithOptionalParameter
            int encodingSpeed = 0,
            int decodingSpeed = 4,
            int positionQuantization = 14,
            int normalQuantization = 10,
            int texCoordQuantization = 12,
            int colorQuantization = 8,
            int genericQuantization = 12
        )
        {
            return await EncodeMesh(
                mesh,
                meshData,
                new QuantizationSettings(
                    positionQuantization: positionQuantization,
                    normalQuantization: normalQuantization,
                    texCoordQuantization: texCoordQuantization,
                    colorQuantization: colorQuantization
                ),
                new SpeedSettings(encodingSpeed, decodingSpeed));
        }

        static unsafe IntPtr PinArray(int[] indices, out ulong gcHandle)
        {
            return (IntPtr)UnsafeUtility.PinGCArrayAndGetDataAddress(indices, out gcHandle);
        }

        static unsafe IntPtr[] GetReadOnlyPointers(int count, NativeArray<byte>[] vData)
        {
            var result = new IntPtr[count];
            for (var stream = 0; stream < count; stream++)
            {
                result[stream] = (IntPtr)vData[stream].GetUnsafeReadOnlyPtr();
            }

            return result;
        }

        static DataType GetDataType(VertexAttributeFormat format)
        {
            switch (format)
            {
                case VertexAttributeFormat.Float32:
                case VertexAttributeFormat.Float16:
                    return DataType.Float32;
                case VertexAttributeFormat.UNorm8:
                case VertexAttributeFormat.UInt8:
                    return DataType.UInt8;
                case VertexAttributeFormat.SNorm8:
                case VertexAttributeFormat.SInt8:
                    return DataType.Int8;
                case VertexAttributeFormat.UInt16:
                case VertexAttributeFormat.UNorm16:
                    return DataType.UInt16;
                case VertexAttributeFormat.SInt16:
                case VertexAttributeFormat.SNorm16:
                    return DataType.Int16;
                case VertexAttributeFormat.UInt32:
                case VertexAttributeFormat.SInt32:
                    return DataType.Int32;
                default:
                    throw new ArgumentOutOfRangeException(nameof(format), format, null);
            }
        }

        static AttributeType GetAttributeType(VertexAttribute attribute)
        {
            switch (attribute)
            {
                case VertexAttribute.Position:
                    return AttributeType.Position;
                case VertexAttribute.Normal:
                    return AttributeType.Normal;
                case VertexAttribute.Color:
                    return AttributeType.Color;
                case VertexAttribute.TexCoord0:
                case VertexAttribute.TexCoord1:
                case VertexAttribute.TexCoord2:
                case VertexAttribute.TexCoord3:
                case VertexAttribute.TexCoord4:
                case VertexAttribute.TexCoord5:
                case VertexAttribute.TexCoord6:
                case VertexAttribute.TexCoord7:
                    return AttributeType.TextureCoordinate;
                case VertexAttribute.Tangent:
                case VertexAttribute.BlendWeight:
                case VertexAttribute.BlendIndices:
                    return AttributeType.Generic;
                default:
                    throw new ArgumentOutOfRangeException(nameof(attribute), attribute, null);
            }
        }

        static unsafe int GetAttributeSize(VertexAttributeFormat format)
        {
            switch (format)
            {
                case VertexAttributeFormat.Float32:
                    return sizeof(float);
                case VertexAttributeFormat.Float16:
                    return sizeof(half);
                case VertexAttributeFormat.UNorm8:
                    return sizeof(byte);
                case VertexAttributeFormat.SNorm8:
                    return sizeof(sbyte);
                case VertexAttributeFormat.UNorm16:
                    return sizeof(ushort);
                case VertexAttributeFormat.SNorm16:
                    return sizeof(short);
                case VertexAttributeFormat.UInt8:
                    return sizeof(byte);
                case VertexAttributeFormat.SInt8:
                    return sizeof(sbyte);
                case VertexAttributeFormat.UInt16:
                    return sizeof(ushort);
                case VertexAttributeFormat.SInt16:
                    return sizeof(short);
                case VertexAttributeFormat.UInt32:
                    return sizeof(uint);
                case VertexAttributeFormat.SInt32:
                    return sizeof(int);
                default:
                    throw new ArgumentOutOfRangeException(nameof(format), format, null);
            }
        }

        [DllImport(DracoNative.dracoUnityLib)]
        static extern IntPtr dracoEncoderCreate(int vertexCount);

        [DllImport(DracoNative.dracoUnityLib)]
        static extern IntPtr dracoEncoderCreatePointCloud(int vertexCount);

        [DllImport(DracoNative.dracoUnityLib)]
        internal static extern void dracoEncoderRelease(IntPtr encoder);

        [DllImport(DracoNative.dracoUnityLib)]
        static extern void dracoEncoderSetCompressionSpeed(IntPtr encoder, int encodingSpeed, int decodingSpeed);

        [DllImport(DracoNative.dracoUnityLib)]
        static extern void dracoEncoderSetQuantizationBits(IntPtr encoder, int position, int normal, int uv, int color, int generic);

        [DllImport(DracoNative.dracoUnityLib)]
        internal static extern bool dracoEncoderEncode(IntPtr encoder, bool preserveTriangleOrder);

        [DllImport(DracoNative.dracoUnityLib)]
        static extern uint dracoEncoderGetEncodedVertexCount(IntPtr encoder);

        [DllImport(DracoNative.dracoUnityLib)]
        static extern uint dracoEncoderGetEncodedIndexCount(IntPtr encoder);

        [DllImport(DracoNative.dracoUnityLib)]
        internal static extern unsafe void dracoEncoderGetEncodeBuffer(IntPtr encoder, out void* data, out ulong size);

        [DllImport(DracoNative.dracoUnityLib)]
        static extern bool dracoEncoderSetIndices(
            IntPtr encoder,
            DataType indexComponentType,
            uint indexCount,
            bool flip,
            IntPtr indices
            );

        [DllImport(DracoNative.dracoUnityLib)]
        static extern uint dracoEncoderSetAttribute(
            IntPtr encoder,
            int attributeType,
            DataType dracoDataType,
            int componentCount,
            int stride,
            bool flip,
            IntPtr data);
    }
}
