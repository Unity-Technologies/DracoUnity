// SPDX-FileCopyrightText: 2023 Unity Technologies and the Draco for Unity authors
// SPDX-License-Identifier: Apache-2.0

using System;
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
        /// <param name="mesh">MeshData used to create the mesh</param>
        /// <param name="encodedData">Compressed Draco data</param>
        /// <param name="convertSpace">If true, coordinate space is converted from right-hand (like in glTF) to left-hand (Unity).</param>
        /// <param name="requireNormals">If draco does not contain normals and this is set to true, normals are calculated.</param>
        /// <param name="requireTangents">If draco does not contain tangents and this is set to true, tangents and normals are calculated.</param>
        /// <param name="weightsAttributeId">Draco attribute ID that contains bone weights (for skinning)</param>
        /// <param name="jointsAttributeId">Draco attribute ID that contains bone joint indices (for skinning)</param>
        /// <param name="forceUnityLayout">Enforces vertex buffer layout with highest compatibility. Enable this if you want to use blend shapes on the resulting mesh</param>
        /// <returns>A DecodeResult</returns>
        public static async Task<DecodeResult> DecodeMesh(
            Mesh.MeshData mesh,
            NativeSlice<byte> encodedData,
            bool convertSpace = true,
            bool requireNormals = false,
            bool requireTangents = false,
            int weightsAttributeId = -1,
            int jointsAttributeId = -1,
            bool forceUnityLayout = false
        )
        {
            var encodedDataPtr = GetUnsafeReadOnlyIntPtr(encodedData);
            var result = await DecodeMesh(
                mesh,
                encodedDataPtr,
                encodedData.Length,
                requireNormals,
                requireTangents,
                convertSpace,
                weightsAttributeId,
                jointsAttributeId,
                forceUnityLayout
            );
            return result;
        }

        /// <inheritdoc cref="DecodeMesh(Mesh.MeshData,NativeSlice{byte},bool,bool,bool,int,int,bool)"/>
        public static async Task<DecodeResult> DecodeMesh(
            Mesh.MeshData mesh,
            byte[] encodedData,
            bool convertSpace = true,
            bool requireNormals = false,
            bool requireTangents = false,
            int weightsAttributeId = -1,
            int jointsAttributeId = -1,
            bool forceUnityLayout = false
            )
        {
            var encodedDataPtr = PinGCArrayAndGetDataAddress(encodedData, out var gcHandle);
            var result = await DecodeMesh(
                mesh,
                encodedDataPtr,
                encodedData.Length,
                requireNormals,
                requireTangents,
                convertSpace,
                weightsAttributeId,
                jointsAttributeId,
                forceUnityLayout
                );
            UnsafeUtility.ReleaseGCObject(gcHandle);
            return result;
        }

        /// <summary>
        /// Decodes a Draco mesh.
        /// Consider using <see cref="DecodeMesh(Mesh.MeshData,NativeSlice{byte},bool,bool,bool,int,int,bool)"/>
        /// for increased performance.
        /// </summary>
        /// <param name="encodedData">Compressed Draco data</param>
        /// <param name="convertSpace">If true, coordinate space is converted from right-hand (like in glTF) to left-hand (Unity).</param>
        /// <param name="requireNormals">If draco does not contain normals and this is set to true, normals are calculated.</param>
        /// <param name="requireTangents">If draco does not contain tangents and this is set to true, tangents and normals are calculated.</param>
        /// <param name="weightsAttributeId">Draco attribute ID that contains bone weights (for skinning)</param>
        /// <param name="jointsAttributeId">Draco attribute ID that contains bone joint indices (for skinning)</param>
        /// <param name="forceUnityLayout">Enforces vertex buffer layout with highest compatibility. Enable this if you want to use blend shapes on the resulting mesh</param>
        /// <returns>Unity Mesh or null in case of errors</returns>
        public static async Task<Mesh> DecodeMesh(
            NativeSlice<byte> encodedData,
            bool convertSpace = true,
            bool requireNormals = false,
            bool requireTangents = false,
            int weightsAttributeId = -1,
            int jointsAttributeId = -1,
            bool forceUnityLayout = false
            )
        {
            var encodedDataPtr = GetUnsafeReadOnlyIntPtr(encodedData);
            var meshDataArray = Mesh.AllocateWritableMeshData(1);
            var mesh = meshDataArray[0];
            var result = await DecodeMesh(
                mesh,
                encodedDataPtr,
                encodedData.Length,
                requireNormals,
                requireTangents,
                convertSpace,
                weightsAttributeId,
                jointsAttributeId,
                forceUnityLayout
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
                if (requireTangents)
                {
                    unityMesh.RecalculateTangents();
                }
            }
            return unityMesh;
        }

        /// <inheritdoc cref="DecodeMesh(NativeSlice{byte},bool,bool,bool,int,int,bool)"/>
        public static async Task<Mesh> DecodeMesh(
            byte[] encodedData,
            bool convertSpace = true,
            bool requireNormals = false,
            bool requireTangents = false,
            int weightsAttributeId = -1,
            int jointsAttributeId = -1,
            bool forceUnityLayout = false
        )
        {
            return await DecodeMeshInternal(
                encodedData,
                convertSpace,
                requireNormals,
                requireTangents,
                weightsAttributeId,
                jointsAttributeId,
                forceUnityLayout
            );
        }

        internal static async Task<Mesh> DecodeMeshInternal(
            byte[] encodedData,
            bool convertSpace = true,
            bool requireNormals = false,
            bool requireTangents = false,
            int weightsAttributeId = -1,
            int jointsAttributeId = -1,
            bool forceUnityLayout = false
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
                requireNormals,
                requireTangents,
                convertSpace,
                weightsAttributeId,
                jointsAttributeId,
                forceUnityLayout
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
            if (requireTangents)
            {
                unityMesh.RecalculateTangents();
            }
            return unityMesh;
        }


        static async Task<DecodeResult> DecodeMesh(
            Mesh.MeshData mesh,
            IntPtr encodedData,
            int size,
            bool requireNormals,
            bool requireTangents,
            bool convertSpace = true,
            int weightsAttributeId = -1,
            int jointsAttributeId = -1,
            bool forceUnityLayout = false
#if UNITY_EDITOR
            ,bool sync = false
#endif
        )
        {
            var dracoNative = new DracoNative(mesh, convertSpace);
            var result = new DecodeResult();

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
                return result;
            }

            // Normals are required for calculating tangents
            requireNormals |= requireTangents;

            dracoNative.CreateMesh(
                out result.calculateNormals,
                requireNormals,
                requireTangents,
                weightsAttributeId,
                jointsAttributeId,
                forceUnityLayout
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
                return result;
            }

            result.bounds = dracoNative.CreateBounds();
            result.success = dracoNative.PopulateMeshData(result.bounds);
            if (result.success && dracoNative.hasBoneWeightData)
            {
                result.boneWeightData = new BoneWeightData(dracoNative.bonesPerVertex, dracoNative.boneWeights);
                dracoNative.DisposeBoneWeightData();
            }
            return result;
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
    }
}
