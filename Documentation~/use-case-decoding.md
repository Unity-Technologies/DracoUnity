# Use case: Decoding

*Draco for Unity* let's you decode Draco&trade; data at run-time.

## Before you start

Make sure you have *Draco for Unity* [installed](installation.md) and referenced the `Draco` assembly.

## Prepare a Draco file

You need a Draco file to load, so either [create one](obtain.md) or download a sample (like the famous and cute [Stanford Bunny](https://raw.githubusercontent.com/google/draco/master/testdata/bunny_gltf.drc)).

If you copy a Draco (`.drc`) into the Assets folder directly, it'll get decoded and [imported](use-case-editor-import.md) as mesh in the Editor instantly. To avoid that and preserve the compressed state until run-time you can either:

- Change the file extension from `.drc` to `.drc.bytes` to convert it into a [TextAsset](xref: UnityEngine.TextAsset)
- Copy the file into the `StreamingAssets` folder within `Assets`

## Decoding

Decoding can be achieved by calling one of the [DecodeMesh](xref:Draco.DracoDecoder.DecodeMesh*) overloads.

They differ in input data type (`byte[]`/`NativeSlice<byte>`) and [simple vs. advanced Mesh API](xref:UnityEngine.Mesh).

### Decode to Mesh

Here's sample code that decodes a TextAsset containing Draco data and assigns it to the GameObject's MeshFilter, in order to have it rendered.

[!code-cs[load-draco-runtime](../Samples~/Decode/Scripts/DecodeDracoToMesh.cs#LoadDraco)]

### Decode using the advanced Mesh API

Starting with Unity 2020.2 you can create Meshes more efficiently via [`MeshData`](xref:UnityEngine.Mesh.MeshData) and [`MeshDataArray`](xref:UnityEngine.Mesh.MeshDataArray).

*Draco for Unity* allows you to leverage that improved performance at the cost of a bit more overhead code.

The important difference is that instead of returning a [`Mesh`](xref:UnityEngine.Mesh) directly, it just configures the [`MeshData`](xref:UnityEngine.Mesh.MeshData) properly and fills its buffers. It's up to the user to:

- Create the [`Mesh`](xref:UnityEngine.Mesh) instance(s)
- Apply the data via [`ApplyAndDisposeWritableMeshData`](xref:UnityEngine.Mesh.ApplyAndDisposeWritableMeshData(UnityEngine.Mesh/MeshDataArray,System.Collections.Generic.List`1<UnityEngine.Mesh>,UnityEngine.Rendering.MeshUpdateFlags))
- Calculate correct normals/tangents if required (as explained in [shading parameters](#shading-parameters))
- In case the mesh had bone weight data, apply and dispose those as well (optional extra step)

Here's a full example

[!code-cs[load-draco-runtime](../Samples~/Decode/Scripts/DecodeDracoToMeshData.cs#LoadDraco)]

### Shading parameters

Some shaders might require a mesh to have correct normals (or normals plus tangents). For example, the *Unlit* (a flat-shaded color) shader doesn't require neither while another one with a normal map requires both. A Draco mesh might not contain normals or tangents and in those cases normals (or normals and tangents) need to be calculated. The [DecodeMesh](xref:Draco.DracoDecoder.DecodeMesh*) parameters `requireNormals` and `requireTangents` allow you to pass on that aspect of the anticipated mesh application.

[DecodeMesh](xref:Draco.DracoDecoder.DecodeMesh*) overloads the return a ready-to-use [Mesh](xref:UnityEngine.Mesh) will perform the normals/tangents calculations internally.

On [DecodeMesh](xref:Draco.DracoDecoder.DecodeMesh*) overloads that work on [MeshData](xref:UnityEngine.Mesh.MeshData) you'll have to perform those calculations yourself. It'll only ensure that the vertex buffer layout allocates a normal/tangent attribute and the returned [DecodeResult's](xref:Draco.DecodeResult) [calculateNormals field](xref:Draco.DecodeResult.calculateNormals) indicates whether normals need to be calculated. This can be achieved by calling [RecalculateNormals](xref:UnityEngine.Mesh.RecalculateNormals()) after the data was applied (via [`ApplyAndDisposeWritableMeshData`](xref:UnityEngine.Mesh.ApplyAndDisposeWritableMeshData(UnityEngine.Mesh/MeshDataArray,System.Collections.Generic.List`1<UnityEngine.Mesh>,UnityEngine.Rendering.MeshUpdateFlags))). Tangents decoding is [not supported](known-limitations.md#draco-bit-stream-version) so they always need to be calculated.

### Blend shapes parameters

Bone weight and joint attributes (required for blend shapes, also known as skinning or morph targets) are stored as generic attributes within Draco, thus it's not feasible to detect their type from the meta-data only. Usually an enclosing data format (like [glTF&trade;](do-more.md#draco-and-gltf)) stores this information in form of an attribute ID. The [DecodeMesh](xref:Draco.DracoDecoder.DecodeMesh*) parameters `weightsAttributeId` and `jointsAttributeId` allow you to pass this information so those generic attributes can be assigned to their respective purpose.

During development it was discovered that not all vertex buffer layout variants are compatible with blend shapes. You can enforce a Unity specific layout that promises high compatibility by setting the `forceUnityLayout` to `true`.

## Decode Sample

A fully setup sample scene can be found in the [Decode Sample](sample-decode.md).

## Trademarks

*Unity* is a registered trademark of [*Unity Technologies*][unity].

*Draco&trade;* is a trademark of [*Google LLC*][GoogleLLC].

*Khronos&reg;* is a registered trademark and [glTF&trade;][gltf] is a trademark of [The Khronos Group Inc][khronos].

[gltf]: https://www.khronos.org/gltf/
[GoogleLLC]: https://about.google/
[khronos]: https://www.khronos.org
[unity]: https://unity.com
