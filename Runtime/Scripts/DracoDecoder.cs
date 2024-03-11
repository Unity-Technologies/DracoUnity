// SPDX-FileCopyrightText: 2023 Unity Technologies and the Draco for Unity authors
// SPDX-License-Identifier: Apache-2.0

#if UNITY_STANDALONE || UNITY_WEBGL || UNITY_IOS || UNITY_TVOS || UNITY_VISIONOS || UNITY_ANDROID || UNITY_WSA || UNITY_LUMIN
#define DRACO_PLATFORM_SUPPORTED
#else
#define DRACO_PLATFORM_NOT_SUPPORTED
#endif

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using UnityEngine;
using UnityEngine.Rendering;

[assembly: InternalsVisibleTo("Draco.Editor")]

namespace Draco
{
    /// <summary>
    /// Provides Draco mesh decoding.
    /// </summary>
    public static class DracoDecoder
    {
        /// <summary>
        /// These <see cref="MeshUpdateFlags"/> ensure best performance when using DecodeMesh variants that use
        /// <see cref="Mesh.MeshData"/> as parameter. Pass them to the subsequent
        /// <see cref="UnityEngine.Mesh.ApplyAndDisposeWritableMeshData(Mesh.MeshDataArray,Mesh,MeshUpdateFlags)"/>
        /// method. They're used internally for DecodeMesh variants returning a <see cref="Mesh"/> directly.
        /// </summary>
        public const MeshUpdateFlags defaultMeshUpdateFlags = MeshUpdateFlags.DontRecalculateBounds | MeshUpdateFlags.DontValidateIndices | MeshUpdateFlags.DontNotifyMeshUsers | MeshUpdateFlags.DontResetBoneBounds;


        /// <summary>
        /// Decodes a Draco mesh.
        /// </summary>
        /// <param name="meshData">MeshData used to create the mesh</param>
        /// <param name="encodedData">Compressed Draco data</param>
        /// <returns>A DecodeResult</returns>
        public static async Task<DecodeResult> DecodeMesh(
            Mesh.MeshData meshData,
            NativeSlice<byte> encodedData
        )
        {
            return await DecodeMesh(meshData, encodedData, DecodeSettings.Default, null);
        }

        /// <inheritdoc cref="DecodeMesh(Mesh.MeshData,NativeSlice{byte})"/>
        /// <param name="decodeSettings">Decode setting flags</param>
        public static async Task<DecodeResult> DecodeMesh(
            Mesh.MeshData meshData,
            NativeSlice<byte> encodedData,
            DecodeSettings decodeSettings
        )
        {
            return await DecodeMesh(meshData, encodedData, decodeSettings, null);
        }

        /// <inheritdoc cref="DecodeMesh(Mesh.MeshData,NativeSlice{byte},DecodeSettings)"/>
        /// <param name="attributeIdMap">Attribute type to index map</param>
        public static async Task<DecodeResult> DecodeMesh(
            Mesh.MeshData meshData,
            NativeSlice<byte> encodedData,
            DecodeSettings decodeSettings,
            Dictionary<VertexAttribute, int> attributeIdMap
        )
        {
            CertifySupportedPlatform(
#if UNITY_EDITOR
                false
#endif
            );
            var encodedDataPtr = GetUnsafeReadOnlyIntPtr(encodedData);
            var result = await DecodeMesh(
                meshData,
                encodedDataPtr,
                encodedData.Length,
                decodeSettings,
                attributeIdMap
            );
            return result;
        }

        /// <inheritdoc cref="DecodeMesh(Mesh.MeshData,NativeSlice{byte})"/>
        public static async Task<DecodeResult> DecodeMesh(
            Mesh.MeshData meshData,
            byte[] encodedData
        )
        {
            return await DecodeMesh(meshData, encodedData, DecodeSettings.Default, null);
        }

        /// <inheritdoc cref="DecodeMesh(Mesh.MeshData,byte[])"/>
        /// <param name="decodeSettings">Decode setting flags</param>
        public static async Task<DecodeResult> DecodeMesh(
            Mesh.MeshData meshData,
            byte[] encodedData,
            DecodeSettings decodeSettings
        )
        {
            return await DecodeMesh(meshData, encodedData, decodeSettings, null);
        }

        /// <inheritdoc cref="DecodeMesh(Mesh.MeshData,byte[],DecodeSettings)"/>
        /// <param name="attributeIdMap">Attribute type to index map</param>
        public static async Task<DecodeResult> DecodeMesh(
            Mesh.MeshData meshData,
            byte[] encodedData,
            DecodeSettings decodeSettings,
            Dictionary<VertexAttribute, int> attributeIdMap
            )
        {
            CertifySupportedPlatform(
#if UNITY_EDITOR
                false
#endif
            );
            var encodedDataPtr = PinGCArrayAndGetDataAddress(encodedData, out var gcHandle);
            var result = await DecodeMesh(
                meshData,
                encodedDataPtr,
                encodedData.Length,
                decodeSettings,
                attributeIdMap
                );
            UnsafeUtility.ReleaseGCObject(gcHandle);
            return result;
        }

        /// <summary>
        /// Decodes a Draco mesh.
        /// </summary>
        /// <remarks>
        /// Consider using <see cref="DecodeMesh(Mesh.MeshData,NativeSlice{byte})"/>
        /// for increased performance.
        /// </remarks>
        /// <param name="encodedData">Compressed Draco data</param>
        /// <returns>Unity Mesh or null in case of errors</returns>
        public static async Task<Mesh> DecodeMesh(
            NativeSlice<byte> encodedData
        )
        {
            return await DecodeMesh(encodedData, DecodeSettings.Default, null);
        }

        /// <inheritdoc cref="DecodeMesh(NativeSlice{byte})"/>
        /// <param name="decodeSettings">Decode setting flags</param>
        public static async Task<Mesh> DecodeMesh(
            NativeSlice<byte> encodedData,
            DecodeSettings decodeSettings
        )
        {
            return await DecodeMesh(encodedData, decodeSettings, null);
        }

        /// <inheritdoc cref="DecodeMesh(NativeSlice{byte},DecodeSettings)"/>
        /// <param name="attributeIdMap">Attribute type to index map</param>
        public static async Task<Mesh> DecodeMesh(
            NativeSlice<byte> encodedData,
            DecodeSettings decodeSettings,
            Dictionary<VertexAttribute, int> attributeIdMap
            )
        {
            CertifySupportedPlatform(
#if UNITY_EDITOR
                false
#endif
            );
            var encodedDataPtr = GetUnsafeReadOnlyIntPtr(encodedData);
            var meshDataArray = Mesh.AllocateWritableMeshData(1);
            var mesh = meshDataArray[0];
            var result = await DecodeMesh(
                mesh,
                encodedDataPtr,
                encodedData.Length,
                decodeSettings,
                attributeIdMap
                );
            if (!result.success)
            {
                meshDataArray.Dispose();
                return null;
            }
            var unityMesh = new Mesh();
            Mesh.ApplyAndDisposeWritableMeshData(meshDataArray, unityMesh, defaultMeshUpdateFlags);
            if (result.boneWeightData != null)
            {
                result.boneWeightData.ApplyOnMesh(unityMesh);
                result.boneWeightData.Dispose();
            }

            if (unityMesh.GetTopology(0) == MeshTopology.Triangles)
            {
                if (result.calculateNormals)
                {
                    unityMesh.RecalculateNormals();
                }
                if ((decodeSettings & DecodeSettings.RequireTangents) != 0)
                {
                    unityMesh.RecalculateTangents();
                }
            }
            return unityMesh;
        }

        /// <inheritdoc cref="DecodeMesh(NativeSlice{byte})"/>
        public static async Task<Mesh> DecodeMesh(
            byte[] encodedData
        )
        {
            return await DecodeMesh(encodedData, DecodeSettings.Default, null);
        }

        /// <inheritdoc cref="DecodeMesh(NativeSlice{byte},DecodeSettings)"/>
        public static async Task<Mesh> DecodeMesh(
            byte[] encodedData,
            DecodeSettings decodeSettings
        )
        {
            return await DecodeMesh(encodedData, decodeSettings, null);
        }

        /// <inheritdoc cref="DecodeMesh(NativeSlice{byte},DecodeSettings,Dictionary{AttributeType,int})"/>
        public static async Task<Mesh> DecodeMesh(
            byte[] encodedData,
            DecodeSettings decodeSettings,
            Dictionary<VertexAttribute, int> attributeIdMap
        )
        {
            CertifySupportedPlatform(
#if UNITY_EDITOR
                false
#endif
            );
            return await DecodeMeshInternal(
                encodedData,
                decodeSettings,
                attributeIdMap
            );
        }

        /// <summary>
        /// Creates an attribute type to index map from indices for bone weights and joints.
        /// </summary>
        /// <param name="weightsAttributeId">Bone weights attribute index.</param>
        /// <param name="jointsAttributeId">Bone joints attribute index.</param>
        /// <returns></returns>
        public static Dictionary<VertexAttribute, int> CreateAttributeIdMap(
            int weightsAttributeId,
            int jointsAttributeId
            )
        {
            Dictionary<VertexAttribute, int> result = null;
            if (weightsAttributeId >= 0)
            {
                result = new Dictionary<VertexAttribute, int>
                {
                    [VertexAttribute.BlendWeight] = weightsAttributeId
                };
            }

            if (jointsAttributeId >= 0)
            {
                result ??= new Dictionary<VertexAttribute, int>();
                result[VertexAttribute.BlendIndices] = jointsAttributeId;
            }

            return result;
        }

        internal static async Task<Mesh> DecodeMeshInternal(
            byte[] encodedData,
            DecodeSettings decodeSettings,
            Dictionary<VertexAttribute, int> attributeIdMap
#if UNITY_EDITOR
            ,bool sync = false
#endif
        )
        {
            var encodedDataPtr = PinGCArrayAndGetDataAddress(encodedData, out var gcHandle);
            var meshDataArray = Mesh.AllocateWritableMeshData(1);
            var mesh = meshDataArray[0];
            var result = await DecodeMesh(
                mesh,
                encodedDataPtr,
                encodedData.Length,
                decodeSettings,
                attributeIdMap
#if UNITY_EDITOR
                ,sync
#endif
            );
            UnsafeUtility.ReleaseGCObject(gcHandle);
            if (!result.success)
            {
                meshDataArray.Dispose();
                return null;
            }
            var unityMesh = new Mesh();
            Mesh.ApplyAndDisposeWritableMeshData(meshDataArray, unityMesh, defaultMeshUpdateFlags);
            unityMesh.bounds = result.bounds;
            if (result.calculateNormals)
            {
                unityMesh.RecalculateNormals();
            }
            if ((decodeSettings & DecodeSettings.RequireTangents) != 0)
            {
                unityMesh.RecalculateTangents();
            }
            return unityMesh;
        }


        static async Task<DecodeResult> DecodeMesh(
            Mesh.MeshData meshData,
            IntPtr encodedData,
            int size,
            DecodeSettings decodeSettings,
            Dictionary<VertexAttribute, int> attributeIdMap
#if UNITY_EDITOR
            ,bool sync = false
#endif
        )
        {
            var dracoNative = new DracoNative(meshData, decodeSettings);

#if UNITY_EDITOR
            if (sync) {
                dracoNative.InitSync(encodedData, size);
            }
            else
#endif
            {
                await WaitForJobHandle(dracoNative.Init(encodedData, size));
            }
            if (dracoNative.ErrorOccured())
            {
                dracoNative.DisposeDracoMesh();
                return new DecodeResult();
            }

            dracoNative.CreateMesh(
                out var calculateNormals,
                attributeIdMap
                );
#if UNITY_EDITOR
            if (sync) {
                dracoNative.DecodeVertexDataSync();
            }
            else
#endif
            {
                await WaitForJobHandle(dracoNative.DecodeVertexData());
            }
            var error = dracoNative.ErrorOccured();
            dracoNative.DisposeDracoMesh();
            if (error)
            {
                return new DecodeResult();
            }

            var bounds = dracoNative.CreateBounds();
            var success = dracoNative.PopulateMeshData(bounds);
            BoneWeightData boneWeightData = null;
            if (success && dracoNative.hasBoneWeightData)
            {
                boneWeightData = new BoneWeightData(dracoNative.bonesPerVertex, dracoNative.boneWeights);
                dracoNative.DisposeBoneWeightData();
            }
            return new DecodeResult(
                success,
                bounds,
                calculateNormals,
                boneWeightData
                );
        }

        static async Task WaitForJobHandle(JobHandle jobHandle)
        {
            while (!jobHandle.IsCompleted)
            {
                await Task.Yield();
            }
            jobHandle.Complete();
        }

        static unsafe IntPtr GetUnsafeReadOnlyIntPtr(NativeSlice<byte> encodedData)
        {
            return (IntPtr)encodedData.GetUnsafeReadOnlyPtr();
        }

        static unsafe IntPtr PinGCArrayAndGetDataAddress(byte[] encodedData, out ulong gcHandle)
        {
            return (IntPtr)UnsafeUtility.PinGCArrayAndGetDataAddress(encodedData, out gcHandle);
        }

#if !UNITY_EDITOR && DRACO_PLATFORM_SUPPORTED
        [System.Diagnostics.Conditional("FALSE")]
#endif
        internal static void CertifySupportedPlatform(
#if UNITY_EDITOR
            bool editorImport
#endif
        )
        {
#if DRACO_PLATFORM_NOT_SUPPORTED
#if UNITY_EDITOR
#if !DRACO_IGNORE_PLATFORM_NOT_SUPPORTED
            if (!editorImport)
            {
                throw new NotSupportedException("Draco for Unity is not supported on the active build target. This will not work in a build, please switch to a supported platform in the build settings. You can bypass this exception in the Editor by setting the scripting define `DRACO_IGNORE_PLATFORM_NOT_SUPPORTED`.");
            }
#endif // !DRACO_IGNORE_PLATFORM_NOT_SUPPORTED
#else
            // In a build, always throw the exception.
            throw new NotSupportedException("Draco for Unity is not supported on this platform.");
#endif
#endif // DRACO_PLATFORM_NOT_SUPPORTED
        }
    }
}
