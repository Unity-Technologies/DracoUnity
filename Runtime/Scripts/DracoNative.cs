// SPDX-FileCopyrightText: 2023 Unity Technologies and the Draco for Unity authors
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using AOT;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Profiling;
using UnityEngine.Rendering;

[assembly: InternalsVisibleTo("Draco.Encode")]

namespace Draco
{
    [BurstCompile]
    unsafe class DracoNative
    {

#if !UNITY_EDITOR && (UNITY_WEBGL || UNITY_IOS || UNITY_TVOS || UNITY_VISIONOS)
        internal const string dracoUnityLib = "__Internal";
#elif UNITY_ANDROID || UNITY_STANDALONE || UNITY_WSA || UNITY_EDITOR || PLATFORM_LUMIN
        internal const string dracoUnityLib = "draco_unity";
#else
        // Unsupported platform
        internal const string dracoUnityLib = "UnsupportedPlatform";
#endif

        public const int maxStreamCount = 4;

        /// <summary>
        /// If Draco mesh has more vertices than this value, memory is allocated persistent,
        /// which is slower, but safe when spanning multiple frames.
        /// </summary>
        const int k_PersistentDataThreshold = 5_000;

        const int k_MeshPtrIndex = 0;
        const int k_DecoderPtrIndex = 1;
        const int k_BufferPtrIndex = 2;

        // Cached function pointers
        static FunctionPointer<GetDracoBonesJob.GetIndexValueDelegate> s_GetIndexValueInt8Method;
        static FunctionPointer<GetDracoBonesJob.GetIndexValueDelegate> s_GetIndexValueUInt8Method;
        static FunctionPointer<GetDracoBonesJob.GetIndexValueDelegate> s_GetIndexValueInt16Method;
        static FunctionPointer<GetDracoBonesJob.GetIndexValueDelegate> s_GetIndexValueUInt16Method;
        static FunctionPointer<GetDracoBonesJob.GetIndexValueDelegate> s_GetIndexValueInt32Method;
        static FunctionPointer<GetDracoBonesJob.GetIndexValueDelegate> s_GetIndexValueUInt32Method;

        readonly DecodeSettings m_DecodeSettings;

        List<AttributeMapBase> m_Attributes;
        int[] m_StreamStrides;
        int[] m_StreamMemberCount;

        Allocator m_Allocator;
        NativeArray<int> m_DracoDecodeResult;
        NativeArray<IntPtr> m_DracoTempResources;
        NativeArray<float3> m_PositionMinMax;

        bool m_IsPointCloud;

        Mesh.MeshData m_Mesh;
        int m_IndicesCount;

        // START BLEND-HACK
        // TODO: Unity does not support setting bone weights and indices via new Mesh API
        // https://fogbugz.unity3d.com/default.asp?1320869_7g7qeq40va98n6h6
        // As a workaround we extract those attributes separately so they can be fed into
        // Mesh.SetBoneWeights after the Mesh was created.
        AttributeMap m_BoneIndexMap;
        AttributeMap m_BoneWeightMap;
        public bool hasBoneWeightData => m_BoneIndexMap != null && m_BoneWeightMap != null;
        public NativeArray<byte> bonesPerVertex;
        public NativeArray<BoneWeight1> boneWeights;
        // END BLEND-HACK

        public DracoNative(
            Mesh.MeshData mesh,
            DecodeSettings decodeSettings
            )
        {
            m_DecodeSettings = decodeSettings;
            m_Mesh = mesh;
        }

        public JobHandle Init(IntPtr encodedData, int size)
        {
            var decodeJob = CreateDecodeJob(encodedData, size);
            return decodeJob.Schedule();
        }

#if UNITY_EDITOR
        public void InitSync(IntPtr encodedData, int size) {
            var decodeJob = CreateDecodeJob(encodedData, size);
            decodeJob.Run();
        }
#endif

        DecodeJob CreateDecodeJob(IntPtr encodedData, int size)
        {
            m_DracoDecodeResult = new NativeArray<int>(1, Allocator.Persistent);
            m_DracoTempResources = new NativeArray<IntPtr>(3, Allocator.Persistent);
            var decodeJob = new DecodeJob()
            {
                encodedData = (byte*)encodedData,
                size = size,
                result = m_DracoDecodeResult,
                dracoTempResources = m_DracoTempResources
            };
            return decodeJob;
        }

        public bool ErrorOccured()
        {
            return m_DracoDecodeResult[0] < 0;
        }

        void CalculateVertexParams(
            DracoMesh* dracoMesh,
            Dictionary<VertexAttribute, int> attributeIdMap,
            out bool calculateNormals
            )
        {
            Profiler.BeginSample("CalculateVertexParams");

            bool hasTexCoordOrColor;
            using (var generator = new AttributeMapsGenerator(dracoMesh, m_DecodeSettings, attributeIdMap))
            {
                m_Attributes = generator.GenerateAttributeMaps(
                    out calculateNormals,
                    out hasTexCoordOrColor,
                    out m_BoneWeightMap,
                    out m_BoneIndexMap
                );
            }

            m_StreamStrides = new int[maxStreamCount];
            m_StreamMemberCount = new int[maxStreamCount];
            var streamIndex = 0;

            var forceUnityVertexLayout = (m_DecodeSettings & DecodeSettings.ForceUnityVertexLayout) != 0;
            // skinning requires SkinnedMeshRenderer layout
            forceUnityVertexLayout |= m_BoneWeightMap != null && m_BoneIndexMap != null;

            // On scenes with lots of small meshes the overhead of lots
            // of dedicated vertex buffers can have severe negative impact
            // on performance. Therefore we stick to Unity's layout (which
            // combines pos+normal+tangent in one stream) for smaller meshes.
            // See: https://github.com/atteneder/glTFast/issues/197
            forceUnityVertexLayout |= dracoMesh->numVertices <= ushort.MaxValue;

            foreach (var attributeMap in m_Attributes)
            {
                // Stream assignment:
                // Positions get a dedicated stream (0)
                // The rest lands on stream 1

                // If blend weights or blend indices are present, they land on stream 1
                // while the rest is combined in stream 0

                // Mesh layout SkinnedMeshRenderer (used for skinning and blend shapes)
                // requires:
                // stream 0: position,normal,tangent
                // stream 1: UVs,colors
                // stream 2: blend weights/indices

                switch (attributeMap.attribute)
                {
                    case VertexAttribute.Position:
                        // Attributes that define/change the position go to stream 0
                        streamIndex = 0;
                        break;
                    case VertexAttribute.Normal:
                    case VertexAttribute.Tangent:
                        streamIndex = forceUnityVertexLayout ? 0 : 1;
                        break;
                    case VertexAttribute.TexCoord0:
                    case VertexAttribute.TexCoord1:
                    case VertexAttribute.TexCoord2:
                    case VertexAttribute.TexCoord3:
                    case VertexAttribute.TexCoord4:
                    case VertexAttribute.TexCoord5:
                    case VertexAttribute.TexCoord6:
                    case VertexAttribute.TexCoord7:
                    case VertexAttribute.Color:
                        streamIndex = 1;
                        break;
                    case VertexAttribute.BlendWeight:
                    case VertexAttribute.BlendIndices:
                        // Special case: blend weights/joints always have a special stream
                        streamIndex = hasTexCoordOrColor ? 2 : 1;
                        break;
                }
                var elementSize = attributeMap.elementSize;
                attributeMap.offset = m_StreamStrides[streamIndex];
                attributeMap.stream = streamIndex;
                m_StreamStrides[streamIndex] += elementSize;
                m_StreamMemberCount[streamIndex]++;
            }
            m_Attributes.Sort(AttributeMapBase.CompareByStreamAndOffset);
            Profiler.EndSample(); // CalculateVertexParams
        }

#if UNITY_EDITOR
        public void DecodeVertexDataSync() {
            DecodeVertexData(true);
        }
#endif

        public JobHandle DecodeVertexData(
#if UNITY_EDITOR
            bool sync = false
#endif
            )
        {
            var decodeVerticesJob = new DecodeVerticesJob()
            {
                result = m_DracoDecodeResult,
                dracoTempResources = m_DracoTempResources
            };
            var decodeVerticesJobHandle = decodeVerticesJob.Schedule();
#if UNITY_EDITOR
            if (sync) {
                decodeVerticesJobHandle.Complete();
            }
#endif

            JobHandle indicesJob;
            var dracoMeshJobCount = m_Attributes.Count;

            if (m_IsPointCloud)
            {
                indicesJob = new GeneratePointCloudIndicesJob
                {
                    mesh = m_Mesh
                }.Schedule();
            }
            else
            {
                indicesJob = new GetDracoIndicesJob
                {
                    result = m_DracoDecodeResult,
                    dracoTempResources = m_DracoTempResources,
                    flip = (m_DecodeSettings & DecodeSettings.ConvertSpace) != 0,
                    dataType = m_Mesh.indexFormat == IndexFormat.UInt16 ? DataType.UInt16 : DataType.UInt32,
                    mesh = m_Mesh
                }.Schedule(decodeVerticesJobHandle);
                dracoMeshJobCount++;
            }

            if (hasBoneWeightData) dracoMeshJobCount++;

            var jobIndex = 0;
            var jobHandles = new NativeArray<JobHandle>(dracoMeshJobCount, m_Allocator);

            if (!m_IsPointCloud)
            {
                jobHandles[jobIndex] = indicesJob;
                jobIndex++;
            }

#if UNITY_EDITOR
            if (sync) {
                indicesJob.Complete();
            }
#endif

            foreach (var mapBase in m_Attributes)
            {
                var map = mapBase as AttributeMap;
                if (map == null) continue;

                // BLEND-HACK: skip blend indices here (done below)
                // weights were removed from attributes before
                if (map.attribute == VertexAttribute.BlendIndices) continue; // Blend

                var calculateBound = map.attribute == VertexAttribute.Position;
                if (calculateBound)
                {
                    Assert.IsFalse(m_PositionMinMax.IsCreated, "Multiple position attributes are not supported");
                    Assert.AreEqual(3, map.numComponents, "Positions have to be 3 dimensional");
                    m_PositionMinMax = new NativeArray<float3>(2, Allocator.TempJob);
                }

                if (m_StreamMemberCount[map.stream] > 1)
                {
                    if (calculateBound)
                    {
                        var job = new GetDracoDataInterleavedBoundsJob()
                        {
                            result = m_DracoDecodeResult,
                            dracoTempResources = m_DracoTempResources,
                            attribute = map.dracoAttribute,
                            stride = m_StreamStrides[map.stream],
                            flip = map.convertSpace,
                            componentStride = map.numComponents,
                            mesh = m_Mesh,
                            streamIndex = map.stream,
                            offset = map.offset,
                            bounds = m_PositionMinMax,
                        };
                        jobHandles[jobIndex] = job.Schedule(decodeVerticesJobHandle);
                    }
                    else
                    {
                        var job = new GetDracoDataInterleavedJob()
                        {
                            result = m_DracoDecodeResult,
                            dracoTempResources = m_DracoTempResources,
                            attribute = map.dracoAttribute,
                            stride = m_StreamStrides[map.stream],
                            flip = map.convertSpace,
                            componentStride = map.numComponents,
                            mesh = m_Mesh,
                            streamIndex = map.stream,
                            offset = map.offset,
                        };
                        jobHandles[jobIndex] = job.Schedule(decodeVerticesJobHandle);
                    }
                }
                else
                {
                    if (calculateBound)
                    {
                        var job = new GetDracoDataBoundsJob()
                        {
                            result = m_DracoDecodeResult,
                            dracoTempResources = m_DracoTempResources,
                            attribute = map.dracoAttribute,
                            flip = map.convertSpace,
                            componentStride = map.numComponents,
                            mesh = m_Mesh,
                            streamIndex = map.stream,
                            bounds = m_PositionMinMax,
                        };
                        jobHandles[jobIndex] = job.Schedule(decodeVerticesJobHandle);
                    }
                    else
                    {
                        var job = new GetDracoDataJob()
                        {
                            result = m_DracoDecodeResult,
                            dracoTempResources = m_DracoTempResources,
                            attribute = map.dracoAttribute,
                            flip = map.convertSpace,
                            componentStride = map.numComponents,
                            mesh = m_Mesh,
                            streamIndex = map.stream,
                        };
                        jobHandles[jobIndex] = job.Schedule(decodeVerticesJobHandle);
                    }
                }
#if UNITY_EDITOR
                if (sync) {
                    jobHandles[jobIndex].Complete();
                }
#endif
                jobIndex++;
            }

            if (hasBoneWeightData)
            {
                // TODO: BLEND-HACK
                var job = new GetDracoBonesJob()
                {
                    result = m_DracoDecodeResult,
                    dracoTempResources = m_DracoTempResources,
                    indicesAttribute = m_BoneIndexMap.dracoAttribute,
                    weightsAttribute = m_BoneWeightMap.dracoAttribute,
                    bonesPerVertex = bonesPerVertex,
                    boneWeights = boneWeights,
                    indexValueConverter = GetIndexValueConverter(m_BoneIndexMap.format)
                };
                jobHandles[jobIndex] = job.Schedule(decodeVerticesJobHandle);
            }

            var jobHandle = JobHandle.CombineDependencies(jobHandles);
            jobHandles.Dispose();

            var releaseDracoMeshJob = new ReleaseDracoMeshJob
            {
                dracoTempResources = m_DracoTempResources
            };
            var releaseDracoMeshJobHandle = releaseDracoMeshJob.Schedule(jobHandle);

#if UNITY_EDITOR
            if (sync) {
                releaseDracoMeshJobHandle.Complete();
            }
#endif
            if (m_IsPointCloud)
            {
                var pointCloudJobHandles = new NativeArray<JobHandle>(2, m_Allocator);
                pointCloudJobHandles[0] = indicesJob;
                pointCloudJobHandles[1] = releaseDracoMeshJobHandle;
                var pointCloudJobHandle = JobHandle.CombineDependencies(pointCloudJobHandles);
                pointCloudJobHandles.Dispose();
                return pointCloudJobHandle;
            }

            return releaseDracoMeshJobHandle;
        }

        internal void CreateMesh(
            out bool calculateNormals,
            Dictionary<VertexAttribute, int> attributeIdMap
        )
        {
            Profiler.BeginSample("CreateMesh");

            var dracoMesh = (DracoMesh*)m_DracoTempResources[k_MeshPtrIndex];
            m_Allocator = dracoMesh->numVertices > k_PersistentDataThreshold ? Allocator.Persistent : Allocator.TempJob;

            CalculateVertexParams(
                dracoMesh,
                attributeIdMap,
                out calculateNormals
                );

            Profiler.BeginSample("SetParameters");
            m_IsPointCloud = dracoMesh->isPointCloud;

            if (m_IsPointCloud)
            {
                m_IndicesCount = dracoMesh->numVertices;
                m_Mesh.SetIndexBufferParams(dracoMesh->numVertices, dracoMesh->indexFormat);
            }
            else
            {
                m_IndicesCount = dracoMesh->numFaces * 3;
                m_Mesh.SetIndexBufferParams(dracoMesh->numFaces * 3, dracoMesh->indexFormat);
            }
            var vertexParams = new List<VertexAttributeDescriptor>(m_Attributes.Count);
            foreach (var map in m_Attributes)
            {
                vertexParams.Add(map.GetVertexAttributeDescriptor());
            }
            m_Mesh.SetVertexBufferParams(dracoMesh->numVertices, vertexParams.ToArray());
            if (hasBoneWeightData)
            {
                var boneCount = m_BoneIndexMap.numComponents;
                bonesPerVertex = new NativeArray<byte>(dracoMesh->numVertices, Allocator.Persistent);
                boneWeights = new NativeArray<BoneWeight1>(dracoMesh->numVertices * boneCount, Allocator.Persistent);
            }
            Profiler.EndSample(); // SetParameters
            Profiler.EndSample(); // CreateMesh
        }

        public void DisposeDracoMesh()
        {
            m_DracoDecodeResult.Dispose();
            m_DracoTempResources.Dispose();
        }

        public Bounds CreateBounds()
        {
            var bounds = new Bounds();
            if (m_PositionMinMax.IsCreated)
            {
                bounds.SetMinMax(m_PositionMinMax[0], m_PositionMinMax[1]);
                m_PositionMinMax.Dispose();
            }

            return bounds;
        }

        public bool
        PopulateMeshData(Bounds bounds)
        {

            Profiler.BeginSample("PopulateMeshData");

            foreach (var map in m_Attributes)
            {
                map.Dispose();
            }
            m_Attributes = null;

            Profiler.BeginSample("MeshAssign");

            const MeshUpdateFlags flags = DracoDecoder.defaultMeshUpdateFlags;

            m_Mesh.subMeshCount = 1;
            var subMeshDescriptor = new SubMeshDescriptor(
                0,
                m_IndicesCount,
                m_IsPointCloud ? MeshTopology.Points : MeshTopology.Triangles
                )
            {
                vertexCount = m_Mesh.vertexCount,
                bounds = bounds
            };
            m_Mesh.SetSubMesh(0, subMeshDescriptor, flags);
            Profiler.EndSample(); // CreateUnityMesh.CreateMesh
            Profiler.EndSample();

            return true;
        }

        public void DisposeBoneWeightData()
        {
            m_BoneIndexMap = null;
            m_BoneWeightMap = null;
        }

        /// <summary>
        /// Returns Burst compatible function that converts a (bone) index
        /// of type `format` into an int
        /// </summary>
        /// <param name="format">Data type of bone index</param>
        /// <returns>Burst Function Pointer to correct conversion function</returns>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        static FunctionPointer<GetDracoBonesJob.GetIndexValueDelegate> GetIndexValueConverter(VertexAttributeFormat format)
        {
            switch (format)
            {
                case VertexAttributeFormat.UInt8:
                    if (!s_GetIndexValueUInt8Method.IsCreated)
                    {
                        s_GetIndexValueUInt8Method = BurstCompiler.CompileFunctionPointer<GetDracoBonesJob.GetIndexValueDelegate>(GetDracoBonesJob.GetIndexValueUInt8);
                    }
                    return s_GetIndexValueUInt8Method;
                case VertexAttributeFormat.SInt8:
                    if (!s_GetIndexValueInt8Method.IsCreated)
                    {
                        s_GetIndexValueInt8Method = BurstCompiler.CompileFunctionPointer<GetDracoBonesJob.GetIndexValueDelegate>(GetDracoBonesJob.GetIndexValueInt8);
                    }
                    return s_GetIndexValueInt8Method;
                case VertexAttributeFormat.UInt16:
                    if (!s_GetIndexValueUInt16Method.IsCreated)
                    {
                        s_GetIndexValueUInt16Method = BurstCompiler.CompileFunctionPointer<GetDracoBonesJob.GetIndexValueDelegate>(GetDracoBonesJob.GetIndexValueUInt16);
                    }
                    return s_GetIndexValueUInt16Method;
                case VertexAttributeFormat.SInt16:
                    if (!s_GetIndexValueInt16Method.IsCreated)
                    {
                        s_GetIndexValueInt16Method = BurstCompiler.CompileFunctionPointer<GetDracoBonesJob.GetIndexValueDelegate>(GetDracoBonesJob.GetIndexValueInt16);
                    }
                    return s_GetIndexValueInt16Method;
                case VertexAttributeFormat.UInt32:
                    if (!s_GetIndexValueUInt32Method.IsCreated)
                    {
                        s_GetIndexValueUInt32Method = BurstCompiler.CompileFunctionPointer<GetDracoBonesJob.GetIndexValueDelegate>(GetDracoBonesJob.GetIndexValueUInt32);
                    }
                    return s_GetIndexValueUInt32Method;
                case VertexAttributeFormat.SInt32:
                    if (!s_GetIndexValueInt32Method.IsCreated)
                    {
                        s_GetIndexValueInt32Method = BurstCompiler.CompileFunctionPointer<GetDracoBonesJob.GetIndexValueDelegate>(GetDracoBonesJob.GetIndexValueInt32);
                    }
                    return s_GetIndexValueInt32Method;
                default:
                    throw new ArgumentOutOfRangeException(nameof(format), format, null);
            }
        }

        // The order must be consistent with C++ interface.
        [StructLayout(LayoutKind.Sequential)]
        struct DracoData
        {
            public int dataType;
            public IntPtr data;
        }

        [StructLayout(LayoutKind.Sequential)]
        struct DracoAttribute
        {
            // ReSharper disable MemberCanBePrivate.Local
            public int attributeType;
            public int dataType;
            public int numComponents;
            public int uniqueId;
            // ReSharper restore MemberCanBePrivate.Local
        }

        [StructLayout(LayoutKind.Sequential)]
        struct DracoMesh
        {
            public int numFaces;
            public int numVertices;
            // ReSharper disable once MemberCanBePrivate.Local
            public int numAttributes;
            public bool isPointCloud;

            public IndexFormat indexFormat => numVertices >= ushort.MaxValue ? IndexFormat.UInt32 : IndexFormat.UInt16;
        }

        /// <summary>
        /// Release data associated with DracoMesh.
        /// </summary>
        /// <param name="mesh">Draco mesh</param>
        [DllImport(dracoUnityLib)]
        static extern void ReleaseDracoMesh(
            DracoMesh** mesh);

        /// <summary>
        /// Release data associated with DracoAttribute.
        /// </summary>
        /// <param name="attr">Draco attribute</param>
        [DllImport(dracoUnityLib)]
        static extern void
            ReleaseDracoAttribute(DracoAttribute** attr);

        /// <summary>
        /// Release attribute data.
        /// </summary>
        /// <param name="data">Draco data</param>
        [DllImport(dracoUnityLib)]
        static extern void ReleaseDracoData(
            DracoData** data);

        /// <summary>
        /// Initializes decoding of a compressed Draco mesh.
        /// Has to be continued by calling <see cref="DecodeDracoMeshStep2"/> (if no error occured).
        /// The returned mesh must released with <see cref="ReleaseDracoMesh"/>.
        /// </summary>
        /// <param name="buffer">Pointer to compressed Draco input data</param>
        /// <param name="length">Length of input buffer</param>
        /// <param name="mesh">Resulting mesh pointer</param>
        /// <param name="decoder">Resulting decoder instance pointer</param>
        /// <param name="decoderBuffer">Resulting decoder buffer pointer</param>
        /// <returns>Draco error code</returns>
        [DllImport(dracoUnityLib)]
        static extern int DecodeDracoMeshStep1(
            byte* buffer, int length, DracoMesh** mesh, void** decoder, void** decoderBuffer);


        /// <summary>
        /// Decodes compressed DracoMesh.
        /// Comes after calling <see cref="DecodeDracoMeshStep1"/>.
        /// Mesh must released with <see cref="ReleaseDracoMesh"/>.
        /// </summary>
        /// <param name="mesh">Draco mesh instance pointer</param>
        /// <param name="decoder">Draco decoder instance pointer</param>
        /// <param name="decoderBuffer">Decoder buffer pointer</param>
        /// <returns>Draco error code</returns>
        [DllImport(dracoUnityLib)]
        static extern int DecodeDracoMeshStep2(
            DracoMesh** mesh, void* decoder, void* decoderBuffer);

        /// <summary>
        /// Returns the DracoAttribute of type at index in mesh. On input, attribute
        /// must be null. E.g. If the mesh has two texture coordinates then
        /// GetAttributeByType(mesh, AttributeType.TEX_COORD, 1, &amp;attr); will return
        /// the second TEX_COORD attribute. The returned attr must be released with
        /// ReleaseDracoAttribute.
        /// </summary>
        /// <param name="mesh">Draco mesh</param>
        /// <param name="type">Attribute type</param>
        /// <param name="index">Per attribute type sub-index</param>
        /// <param name="attr">Resulting attribute pointer</param>
        /// <returns>True if the attribute was retrieved successfully. False otherwise.</returns>
        [DllImport(dracoUnityLib)]
        static extern bool GetAttributeByType(
            DracoMesh* mesh, AttributeType type, int index, DracoAttribute** attr);

        /// <summary>
        /// Returns the DracoAttribute with unique_id in mesh. On input, attribute
        /// must be null.The returned attr must be released with
        /// ReleaseDracoAttribute.
        /// </summary>
        /// <param name="mesh">Draco mesh</param>
        /// <param name="uniqueID">Unique ID</param>
        /// <param name="attr">Resulting attribute pointer</param>
        /// <returns>True if the attribute was retrieved successfully. False otherwise.</returns>
        [DllImport(dracoUnityLib)]
        static extern bool
            GetAttributeByUniqueId(DracoMesh* mesh, int uniqueID,
                DracoAttribute** attr);

        /// <summary>
        /// Returns an array of indices as well as the type of data in data_type. On
        /// input, indices must be null. The returned indices must be released with
        /// ReleaseDracoData.
        /// </summary>
        /// <param name="mesh">DracoMesh to extract indices from</param>
        /// <param name="dataType">Index data type (int or short) </param>
        /// <param name="indices">Destination index buffer</param>
        /// <param name="indicesCount">Number of indices (equals triangle count * 3)</param>
        /// <param name="flip">If true, triangle vertex order is reverted</param>
        /// <returns>True if extraction succeeded, false otherwise</returns>
        [DllImport(dracoUnityLib)]
        static extern bool GetMeshIndices(
            DracoMesh* mesh,
            DataType dataType,
            void* indices,
            int indicesCount,
            bool flip
            );

        /// <summary>
        /// Returns an array of attribute data as well as the type of data in
        /// data_type. On input, data must be null. The returned data must be
        /// released with ReleaseDracoData.
        /// </summary>
        /// <param name="mesh">Draco mesh</param>
        /// <param name="attr">Attribute</param>
        /// <param name="data">Resulting data</param>
        /// <param name="flip">Determines whether a space conversion should be applied (flips one axis)</param>
        /// <param name="componentStride">Component stride</param>
        /// <returns>True if retrieving data was successful. False otherwise.</returns>
        [DllImport(dracoUnityLib)]
        static extern bool GetAttributeData(
            DracoMesh* mesh, DracoAttribute* attr, DracoData** data, bool flip, int componentStride);

        sealed class AttributeMapsGenerator : IDisposable
        {
            List<AttributeMapBase> m_Attributes = new List<AttributeMapBase>();
            HashSet<VertexAttribute> m_AttributeTypes = new HashSet<VertexAttribute>();
            Dictionary<VertexAttribute, int> m_AttributeIdMap;
            DracoMesh* m_DracoMesh;
            readonly DecodeSettings m_DecodeSettings;

            public AttributeMapsGenerator(
                DracoMesh* dracoMesh,
                DecodeSettings decodeSettings,
                Dictionary<VertexAttribute, int> attributeIdMap
                )
            {
                m_DracoMesh = dracoMesh;
                m_DecodeSettings = decodeSettings;
                m_AttributeIdMap = attributeIdMap;
            }

            public List<AttributeMapBase> GenerateAttributeMaps(
                out bool calculateNormals,
                out bool hasTexCoordOrColor,
                out AttributeMap boneWeightMap,
                out AttributeMap boneIndexMap
            )
            {
                // Vertex attributes are added in the order defined here:
                // https://docs.unity3d.com/2020.1/Documentation/ScriptReference/Rendering.VertexAttributeDescriptor.html

                CreateAttributeMap(AttributeType.Position, VertexAttribute.Position);

                var hasNormals = CreateAttributeMap(AttributeType.Normal, VertexAttribute.Normal, true);

                calculateNormals = !hasNormals && (m_DecodeSettings & DecodeSettings.RequireNormalsAndTangents) != 0;
                if (calculateNormals)
                {
                    calculateNormals = true;
                    m_Attributes.Add(new CalculatedAttributeMap(VertexAttribute.Normal, VertexAttributeFormat.Float32, 3, 4));
                }

                if (TryCreateAttributeMapById(VertexAttribute.Tangent, out var tangentMap))
                {
                    m_Attributes.Add(tangentMap);
                }
                else if ((m_DecodeSettings & DecodeSettings.RequireTangents) != 0)
                {
                    m_Attributes.Add(new CalculatedAttributeMap(VertexAttribute.Tangent, VertexAttributeFormat.Float32, 4, 4));
                }

                hasTexCoordOrColor = CreateAttributeMapsByType(AttributeType.Color, 1, true);

                if (TryCreateAttributeMapById(VertexAttribute.TexCoord0, out var uvMap))
                {
                    hasTexCoordOrColor = true;
                    m_Attributes.Add(uvMap);
                    for (var i = 1; i < 8; i++)
                    {
                        var att = VertexAttribute.TexCoord0 + i;
                        if (TryCreateAttributeMapById(att, out uvMap))
                        {
                            m_Attributes.Add(uvMap);
                        }
                        else
                        {
                            break;
                        }
                    }
                }
                else
                {
                    hasTexCoordOrColor |= CreateAttributeMapsByType(AttributeType.TextureCoordinate, 8, true);
                }

                boneIndexMap = null;
                TryCreateAttributeMapById(VertexAttribute.BlendWeight, out boneWeightMap);
                TryCreateAttributeMapById(VertexAttribute.BlendIndices, out boneIndexMap);

                // BLEND-HACK: Notice that boneWeightMap and boneIndexMap are not added to the attributes as they'd get
                // deleted upon calling Mesh.SetBoneWeights

                return m_Attributes;
            }

            bool TryCreateAttributeMapById(VertexAttribute vertexAttribute, out AttributeMap attributeMap)
            {
                attributeMap = null;
                return m_AttributeIdMap != null && m_AttributeIdMap.TryGetValue(vertexAttribute, out var attributeId)
                    && CreateAttributeMapById(vertexAttribute, attributeId, out attributeMap);
            }

            bool CreateAttributeMap(AttributeType attributeType, VertexAttribute vertexAttribute, bool normalized = false)
            {
                if (TryCreateAttributeMapById(vertexAttribute, out var map))
                {
                    m_Attributes.Add(map);
                    return true;
                }

                return CreateAttributeMapsByType(attributeType, 1, normalized);
            }

            bool CreateAttributeMapsByType(
                AttributeType attributeType,
                int count,
                bool normalized = false
                )
            {
                var foundAttribute = false;
                for (var i = 0; i < count; i++)
                {
                    var type = GetVertexAttribute(attributeType, i);
                    if (!type.HasValue)
                    {
#if UNITY_EDITOR
                        // ReSharper disable once Unity.PerformanceCriticalCodeInvocation
                        Debug.LogWarning($"Unknown attribute {attributeType}!");
#endif
                        continue;
                    }
                    if (WasAlreadyAdded(type.Value))
                        return false;

                    DracoAttribute* attribute = null;
                    if (GetAttributeByType(m_DracoMesh, attributeType, i, &attribute))
                    {
                        var format = GetVertexAttributeFormat(
                            (DataType)attribute->dataType, normalized);
                        if (!format.HasValue) { continue; }
                        var map = new AttributeMap(
                            attribute,
                            type.Value,
                            format.Value,
                            ConvertSpace(type.Value) && (m_DecodeSettings & DecodeSettings.ConvertSpace) != 0
                        );
                        m_Attributes.Add(map);
                        m_AttributeTypes.Add(type.Value);
                        foundAttribute = true;
                    }
                    else
                    {
                        // attributeType was not found
                        break;
                    }
                }
                return foundAttribute;
            }

            bool CreateAttributeMapById(VertexAttribute type, int id, out AttributeMap map, bool normalized = false)
            {
                map = null;
                if (WasAlreadyAdded(type))
                    return false;

                DracoAttribute* attribute;
                if (GetAttributeByUniqueId(m_DracoMesh, id, &attribute))
                {
                    var format = GetVertexAttributeFormat((DataType)attribute->dataType, normalized);
                    if (!format.HasValue) { return false; }

                    map = new AttributeMap(
                        attribute,
                        type,
                        format.Value,
                        ConvertSpace(type) && (m_DecodeSettings & DecodeSettings.ConvertSpace) != 0
                    );
                    m_AttributeTypes.Add(type);
                    return true;
                }
                return false;
            }

            // ReSharper disable Unity.PerformanceAnalysis
            bool WasAlreadyAdded(VertexAttribute attribute)
            {
                if (m_AttributeTypes.Contains(attribute))
                {
#if UNITY_EDITOR
                    Debug.LogWarning($"Multiple {attribute} attributes!");
#endif
                    return true;
                }

                return false;
            }

            public void Dispose()
            {
                m_Attributes = null;
                m_AttributeTypes = null;
                m_AttributeIdMap = null;
                m_DracoMesh = null;
            }
        }

        abstract class AttributeMapBase : IDisposable
        {
            public readonly VertexAttribute attribute;
            public VertexAttributeFormat format;
            public int offset;
            public int stream;

            protected AttributeMapBase(VertexAttribute attribute, VertexAttributeFormat format)
            {
                this.attribute = attribute;
                this.format = format;
                offset = 0;
                stream = 0;
            }

            public abstract int numComponents { get; }
            public abstract int elementSize { get; }

            public void Dispose()
            {
                Dispose(true);
                GC.SuppressFinalize(this);
            }

            protected abstract void Dispose(bool disposing);

            public VertexAttributeDescriptor GetVertexAttributeDescriptor()
            {
                return new VertexAttributeDescriptor(attribute, format, numComponents, stream);
            }

            public static int CompareByStreamAndOffset(AttributeMapBase a, AttributeMapBase b)
            {
                var result = a.stream.CompareTo(b.stream);
                if (result == 0) result = a.offset.CompareTo(b.offset);
                return result;
            }
        }

        class AttributeMap : AttributeMapBase
        {
            public DracoAttribute* dracoAttribute;
            public bool convertSpace;

            public AttributeMap(DracoAttribute* dracoAttribute, VertexAttribute attribute, VertexAttributeFormat format, bool convertSpace) : base(attribute, format)
            {
                this.dracoAttribute = dracoAttribute;
                this.convertSpace = convertSpace;
            }

            /// <summary>
            /// Unity specifies that attribute data size must be divisible by 4.
            /// This value may contain an additional pad to meet this requirement.
            /// </summary>
            public override int numComponents
            {
                get
                {
                    int dracoElemSize = DataTypeSize((DataType)dracoAttribute->dataType) * dracoAttribute->numComponents;

                    if (dracoElemSize % 4 == 0)
                    {
                        return dracoAttribute->numComponents;
                    }
                    else
                    {
                        // Pad such that element size is divisible by 4.
                        int padBytes = 4 - dracoElemSize % 4;
                        int padComponents = padBytes / DataTypeSize((DataType)dracoAttribute->dataType);
                        return dracoAttribute->numComponents + padComponents;
                    }
                }
            }

            public override int elementSize => numComponents * DataTypeSize((DataType)dracoAttribute->dataType);

            protected override void Dispose(bool disposing)
            {
                var tmp = dracoAttribute;
                ReleaseDracoAttribute(&tmp);
                dracoAttribute = null;
            }
        }

        class CalculatedAttributeMap : AttributeMapBase
        {
            readonly int m_NumComponents;
            readonly int m_ElementSize;

            public CalculatedAttributeMap(VertexAttribute attribute, VertexAttributeFormat format, int numComponents, int componentSize) : base(attribute, format)
            {
                m_NumComponents = numComponents;
                m_ElementSize = componentSize * numComponents;
            }

            public override int numComponents => m_NumComponents;
            public override int elementSize => m_ElementSize;

            protected override void Dispose(bool disposing) { }
        }

        [BurstCompile]
        struct DecodeJob : IJob
        {

            [ReadOnly]
            [NativeDisableUnsafePtrRestriction]
            public byte* encodedData;

            [ReadOnly]
            public int size;

            public NativeArray<int> result;
            public NativeArray<IntPtr> dracoTempResources;

            public void Execute()
            {
                DracoMesh* dracoMeshPtr;
                DracoMesh** dracoMeshPtrPtr = &dracoMeshPtr;
                void* decoder;
                void* buffer;
                var decodeResult = DecodeDracoMeshStep1(encodedData, size, dracoMeshPtrPtr, &decoder, &buffer);
                result[0] = decodeResult;
                if (decodeResult < 0)
                {
                    return;
                }
                dracoTempResources[k_MeshPtrIndex] = (IntPtr)dracoMeshPtr;
                dracoTempResources[k_DecoderPtrIndex] = (IntPtr)decoder;
                dracoTempResources[k_BufferPtrIndex] = (IntPtr)buffer;
                result[0] = 0;
            }
        }

        [BurstCompile]
        struct DecodeVerticesJob : IJob
        {

            public NativeArray<int> result;
            public NativeArray<IntPtr> dracoTempResources;

            public void Execute()
            {
                if (result[0] < 0)
                {
                    return;
                }
                var dracoMeshPtr = (DracoMesh*)dracoTempResources[k_MeshPtrIndex];
                var dracoMeshPtrPtr = &dracoMeshPtr;
                var decoder = (void*)dracoTempResources[k_DecoderPtrIndex];
                var buffer = (void*)dracoTempResources[k_BufferPtrIndex];
                var decodeResult = DecodeDracoMeshStep2(dracoMeshPtrPtr, decoder, buffer);
                result[0] = decodeResult;
            }
        }

        [BurstCompile]
        struct GetDracoIndicesJob : IJob
        {

            [ReadOnly]
            public NativeArray<int> result;
            [ReadOnly]
            public NativeArray<IntPtr> dracoTempResources;
            [ReadOnly]
            public bool flip;
            [ReadOnly]
            public DataType dataType;
            public Mesh.MeshData mesh;

            public void Execute()
            {
                if (result[0] < 0)
                {
                    return;
                }
                var dracoMesh = (DracoMesh*)dracoTempResources[k_MeshPtrIndex];
                Assert.IsFalse(dracoMesh->isPointCloud);
                void* indicesPtr;
                int indicesLength;

                switch (dataType)
                {
                    case DataType.UInt16:
                        {
                            var indices = mesh.GetIndexData<ushort>();
                            indicesPtr = indices.GetUnsafePtr();
                            indicesLength = indices.Length;
                            break;
                        }
                    case DataType.UInt32:
                        {
                            var indices = mesh.GetIndexData<uint>();
                            indicesPtr = indices.GetUnsafePtr();
                            indicesLength = indices.Length;
                            break;
                        }
                    default:
                        result[0] = -1;
                        return;
                }
                GetMeshIndices(dracoMesh, dataType, indicesPtr, indicesLength, flip);
            }
        }

        [BurstCompile]
        struct GeneratePointCloudIndicesJob : IJob
        {
            public Mesh.MeshData mesh;

            public void Execute()
            {
                switch (mesh.indexFormat)
                {
                    case IndexFormat.UInt16:
                        {
                            var indices = mesh.GetIndexData<ushort>();
                            for (var i = 0; i < indices.Length; i++)
                            {
                                indices[i] = (ushort)i;
                            }
                            break;
                        }
                    case IndexFormat.UInt32:
                        {
                            var indices = mesh.GetIndexData<uint>();
                            for (var i = 0; i < indices.Length; i++)
                            {
                                indices[i] = (uint)i;
                            }
                            break;
                        }
                }
            }
        }

        [BurstCompile]
        struct GetDracoDataJob : IJob
        {

            [ReadOnly]
            public NativeArray<int> result;
            [ReadOnly]
            public NativeArray<IntPtr> dracoTempResources;

            [ReadOnly]
            [NativeDisableUnsafePtrRestriction]
            public DracoAttribute* attribute;

            [ReadOnly]
            public bool flip;

            [ReadOnly]
            public int componentStride;

            public Mesh.MeshData mesh;
            [ReadOnly]
            public int streamIndex;

            public void Execute()
            {
                if (result[0] < 0)
                {
                    return;
                }
                var dracoMesh = (DracoMesh*)dracoTempResources[k_MeshPtrIndex];
                DracoData* data = null;
                GetAttributeData(dracoMesh, attribute, &data, flip, componentStride);
                var elementSize = DataTypeSize((DataType)data->dataType) * componentStride;
                var dst = mesh.GetVertexData<byte>(streamIndex);
                var dstPtr = dst.GetUnsafePtr();
                UnsafeUtility.MemCpy(dstPtr, (void*)data->data, elementSize * dracoMesh->numVertices);
                ReleaseDracoData(&data);
            }
        }

        [BurstCompile]
        struct GetDracoDataBoundsJob : IJob
        {

            [ReadOnly]
            public NativeArray<int> result;
            [ReadOnly]
            public NativeArray<IntPtr> dracoTempResources;

            [ReadOnly]
            [NativeDisableUnsafePtrRestriction]
            public DracoAttribute* attribute;

            [ReadOnly]
            public bool flip;

            [ReadOnly]
            public int componentStride;

            public Mesh.MeshData mesh;
            [ReadOnly]
            public int streamIndex;

            public NativeArray<float3> bounds;

            public void Execute()
            {
                if (result[0] < 0)
                {
                    return;
                }
                var dracoMesh = (DracoMesh*)dracoTempResources[k_MeshPtrIndex];
                DracoData* data = null;
                GetAttributeData(dracoMesh, attribute, &data, flip, componentStride);
                var elementSize = DataTypeSize((DataType)data->dataType) * componentStride;
                var dst = mesh.GetVertexData<byte>(streamIndex);
                var dstPtr = dst.GetUnsafePtr();
                for (var v = 0; v < dracoMesh->numVertices; v++)
                {
                    var value = *(float3*)((byte*)data->data + elementSize * v);
                    bounds[0] = math.min(bounds[0], value);
                    bounds[1] = math.max(bounds[1], value);
                    *((float3*)(((byte*)dstPtr) + elementSize * v)) = value;
                }
                ReleaseDracoData(&data);
            }
        }

        [BurstCompile]
        struct GetDracoDataInterleavedJob : IJob
        {

            [ReadOnly]
            public NativeArray<int> result;
            [ReadOnly]
            public NativeArray<IntPtr> dracoTempResources;

            [ReadOnly]
            [NativeDisableUnsafePtrRestriction]
            public DracoAttribute* attribute;

            [ReadOnly]
            public int stride;

            [ReadOnly]
            public bool flip;

            [ReadOnly]
            public int componentStride;

            public Mesh.MeshData mesh;

            [ReadOnly]
            public int streamIndex;

            [ReadOnly]
            public int offset;

            public void Execute()
            {
                if (result[0] < 0)
                {
                    return;
                }
                var dracoMesh = (DracoMesh*)dracoTempResources[k_MeshPtrIndex];
                DracoData* data = null;
                GetAttributeData(dracoMesh, attribute, &data, flip, componentStride);
                var elementSize = DataTypeSize((DataType)data->dataType) * componentStride;
                var dst = mesh.GetVertexData<byte>(streamIndex);
                var dstPtr = ((byte*)dst.GetUnsafePtr()) + offset;
                for (var v = 0; v < dracoMesh->numVertices; v++)
                {
                    UnsafeUtility.MemCpy(dstPtr + (stride * v), ((byte*)data->data) + (elementSize * v), elementSize);
                }
                ReleaseDracoData(&data);
            }
        }


        [BurstCompile]
        struct GetDracoDataInterleavedBoundsJob : IJob
        {

            [ReadOnly]
            public NativeArray<int> result;
            [ReadOnly]
            public NativeArray<IntPtr> dracoTempResources;

            [ReadOnly]
            [NativeDisableUnsafePtrRestriction]
            public DracoAttribute* attribute;

            [ReadOnly]
            public int stride;

            [ReadOnly]
            public bool flip;

            [ReadOnly]
            public int componentStride;

            public Mesh.MeshData mesh;

            [ReadOnly]
            public int streamIndex;

            [ReadOnly]
            public int offset;

            public NativeArray<float3> bounds;

            public void Execute()
            {
                if (result[0] < 0)
                {
                    return;
                }
                var dracoMesh = (DracoMesh*)dracoTempResources[k_MeshPtrIndex];
                DracoData* data = null;
                GetAttributeData(dracoMesh, attribute, &data, flip, componentStride);
                var elementSize = DataTypeSize((DataType)data->dataType) * componentStride;
                var dst = mesh.GetVertexData<byte>(streamIndex);
                var dstPtr = ((byte*)dst.GetUnsafePtr()) + offset;
                for (var v = 0; v < dracoMesh->numVertices; v++)
                {
                    var value = *(float3*)((byte*)data->data + elementSize * v);
                    bounds[0] = math.min(bounds[0], value);
                    bounds[1] = math.max(bounds[1], value);
                    *((float3*)(dstPtr + stride * v)) = value;
                }
                ReleaseDracoData(&data);
            }
        }

        [BurstCompile]
        struct GetDracoBonesJob : IJob
        {

            public delegate int GetIndexValueDelegate(IntPtr baseAddress, int index);

            public FunctionPointer<GetIndexValueDelegate> indexValueConverter;

            [ReadOnly]
            public NativeArray<int> result;
            [ReadOnly]
            public NativeArray<IntPtr> dracoTempResources;

            [ReadOnly]
            [NativeDisableUnsafePtrRestriction]
            public DracoAttribute* indicesAttribute;

            [ReadOnly]
            [NativeDisableUnsafePtrRestriction]
            public DracoAttribute* weightsAttribute;

            [WriteOnly]
            public NativeArray<byte> bonesPerVertex;

            [WriteOnly]
            public NativeArray<BoneWeight1> boneWeights;

            public void Execute()
            {
                if (result[0] < 0)
                {
                    return;
                }
                var dracoMesh = (DracoMesh*)dracoTempResources[k_MeshPtrIndex];

                DracoData* indicesData = null;
                GetAttributeData(dracoMesh, indicesAttribute, &indicesData, false, indicesAttribute->numComponents);
                var indexSize = DataTypeSize((DataType)indicesData->dataType) * indicesAttribute->numComponents;

                DracoData* weightsData = null;
                GetAttributeData(dracoMesh, weightsAttribute, &weightsData, false, weightsAttribute->numComponents);
                var weightSize = DataTypeSize((DataType)weightsData->dataType) * weightsAttribute->numComponents;

                for (var v = 0; v < dracoMesh->numVertices; v++)
                {
                    bonesPerVertex[v] = (byte)indicesAttribute->numComponents;
                    var indicesPtr = (IntPtr)(((byte*)indicesData->data) + (indexSize * v));
                    var weightsPtr = (float*)(((byte*)weightsData->data) + (weightSize * v));
                    for (var b = 0; b < indicesAttribute->numComponents; b++)
                    {
                        boneWeights[v * indicesAttribute->numComponents + b] = new BoneWeight1
                        {
                            boneIndex = indexValueConverter.Invoke(indicesPtr, b),
                            weight = *(weightsPtr + b)
                        };
                    }
                }
                ReleaseDracoData(&indicesData);
                ReleaseDracoData(&weightsData);
            }

            [BurstCompile]
            [MonoPInvokeCallback(typeof(GetIndexValueDelegate))]
            public static int GetIndexValueUInt8(IntPtr baseAddress, int index)
            {
                return *((byte*)baseAddress + index);
            }

            [BurstCompile]
            [MonoPInvokeCallback(typeof(GetIndexValueDelegate))]
            public static int GetIndexValueInt8(IntPtr baseAddress, int index)
            {
                return *(((sbyte*)baseAddress) + index);
            }

            [BurstCompile]
            [MonoPInvokeCallback(typeof(GetIndexValueDelegate))]
            public static int GetIndexValueUInt16(IntPtr baseAddress, int index)
            {
                return *(((ushort*)baseAddress) + index);
            }

            [BurstCompile]
            [MonoPInvokeCallback(typeof(GetIndexValueDelegate))]
            public static int GetIndexValueInt16(IntPtr baseAddress, int index)
            {
                return *(((short*)baseAddress) + index);
            }

            [BurstCompile]
            [MonoPInvokeCallback(typeof(GetIndexValueDelegate))]
            public static int GetIndexValueUInt32(IntPtr baseAddress, int index)
            {
                return (int)*(((uint*)baseAddress) + index);
            }

            [BurstCompile]
            [MonoPInvokeCallback(typeof(GetIndexValueDelegate))]
            public static int GetIndexValueInt32(IntPtr baseAddress, int index)
            {
                return *(((int*)baseAddress) + index);
            }
        }

        [BurstCompile]
        struct ReleaseDracoMeshJob : IJob
        {

            public NativeArray<IntPtr> dracoTempResources;

            public void Execute()
            {
                if (dracoTempResources[k_MeshPtrIndex] != IntPtr.Zero)
                {
                    var dracoMeshPtr = (DracoMesh**)dracoTempResources.GetUnsafePtr();
                    ReleaseDracoMesh(dracoMeshPtr);
                }
                dracoTempResources[k_MeshPtrIndex] = IntPtr.Zero;
                dracoTempResources[k_DecoderPtrIndex] = IntPtr.Zero;
                dracoTempResources[k_BufferPtrIndex] = IntPtr.Zero;
            }
        }

        static int DataTypeSize(DataType dt)
        {
            switch (dt)
            {
                case DataType.Int8:
                case DataType.UInt8:
                    return 1;
                case DataType.Int16:
                case DataType.UInt16:
                    return 2;
                case DataType.Int32:
                case DataType.UInt32:
                    return 4;
                case DataType.Int64:
                case DataType.UInt64:
                    return 8;
                case DataType.Float32:
                    return 4;
                case DataType.Float64:
                    return 8;
                case DataType.Bool:
                    return 1;
                default:
                    return -1;
            }
        }

        static VertexAttributeFormat? GetVertexAttributeFormat(DataType inputType, bool normalized = false)
        {
            switch (inputType)
            {
                case DataType.Int8:
                    return normalized ? VertexAttributeFormat.SNorm8 : VertexAttributeFormat.SInt8;
                case DataType.UInt8:
                    return normalized ? VertexAttributeFormat.UNorm8 : VertexAttributeFormat.UInt8;
                case DataType.Int16:
                    return normalized ? VertexAttributeFormat.SNorm16 : VertexAttributeFormat.SInt16;
                case DataType.UInt16:
                    return normalized ? VertexAttributeFormat.UNorm16 : VertexAttributeFormat.UInt16;
                case DataType.Int32:
                    return VertexAttributeFormat.SInt32;
                case DataType.UInt32:
                    return VertexAttributeFormat.UInt32;
                case DataType.Float32:
                    return VertexAttributeFormat.Float32;
                // Not supported by Unity
                // TODO: convert to supported types
                // case DataType.DT_INT64:
                // case DataType.DT_UINT64:
                // case DataType.DT_FLOAT64:
                // case DataType.DT_BOOL:
                default:
                    return null;
            }
        }

        static VertexAttribute? GetVertexAttribute(AttributeType inputType, int index = 0)
        {
            switch (inputType)
            {
                case AttributeType.Position:
                    return VertexAttribute.Position;
                case AttributeType.Normal:
                    return VertexAttribute.Normal;
                case AttributeType.Color:
                    return VertexAttribute.Color;
                case AttributeType.TextureCoordinate:
                    Assert.IsTrue(index < 8);
                    return (VertexAttribute)((int)VertexAttribute.TexCoord0 + index);
                default:
                    return null;
            }
        }

        internal static bool ConvertSpace(VertexAttribute attr)
        {
            switch (attr)
            {
                case VertexAttribute.Position:
                case VertexAttribute.Normal:
                case VertexAttribute.Tangent:
                case VertexAttribute.TexCoord0:
                case VertexAttribute.TexCoord1:
                case VertexAttribute.TexCoord2:
                case VertexAttribute.TexCoord3:
                case VertexAttribute.TexCoord4:
                case VertexAttribute.TexCoord5:
                case VertexAttribute.TexCoord6:
                case VertexAttribute.TexCoord7:
                    return true;
                default:
                    return false;
            }
        }
    }
}
