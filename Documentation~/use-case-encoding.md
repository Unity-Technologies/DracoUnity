# Use case: Encoding

*Draco for Unity* let's you encode Draco&trade; data at run-time.

## Before you start

Make sure you have *Draco for Unity* [installed](installation.md) and referenced the `Draco.Encode` assembly.

## Encode

Encoding can be achieved by calling one of the [EncodeMesh](xref:Draco.Encode.DracoEncoder.EncodeMesh*) overloads. Since sub-meshes are [not supported](known-limitations.md#sub-mesh) they'll get split into separate Draco meshes. The result is an array of [EncodeResults](xref:Draco.Encode.EncodeResult), one for each sub-mesh.

Here's an example that encodes a [Mesh](xref:UnityEngine.Mesh) and saves the results into files.

[!code-cs[encode-draco-runtime](../Samples~/Encode/Scripts/EncodeMeshToDraco.cs#EncodeDraco)]

### Optimize and tweak

#### Speed parameters

The parameters `encodingSpeed` and `decodingSpeed` allow you to tweak the tradeoff between resulting data size in bytes and encoding/decoding speed by turning on/off different compression features. In general, the lowest setting, 0, will have the most compression but worst speed. 10 will have the least compression, but best speed.

#### Quantization settings

Quantization lets you reduce the resulting byte size by limiting the value precision to a certain number of bits. A value of 0 for the quantization parameter will not perform any quantization on the specified attribute. Any value other than 0 will quantize the input values for the specified attribute to that number of bits.

In general, the more you quantize your attributes the better compression rate you will get. It is up to your project to decide how much deviation it will tolerate.

Quantization is specified per attribute type:

- Position (`positionQuantization` parameter)
- Normal (`normalQuantization` parameter)
- Texture Coordinate (`texCoordQuantization` parameter)
- Color (`colorQuantization` parameter)
- Generic (`genericQuantization` parameter)

##### Position Quantization

Instead of providing position quantization in bits you can also provide the `worldScale`, which is the global scale of the mesh in your scene, and a `precision`, which is the desired minimum precision in world units. The final position quantization will be calculated from the mesh's [bounds](xref:UnityEngine.Mesh.bounds) and those two parameters.

### Encode using the advanced Mesh API

If you want to encode multiple Meshes you'll get the best performance by using the advanced Mesh API [EncodeMesh](xref:Draco.Encode.DracoEncoder.EncodeMesh*) overloads that take a [Mesh](xref:UnityEngine.Mesh)/[MeshData](xref:UnityEngine.Mesh.MeshData) pair as first parameters.

The steps are:

1. Call [AcquireReadOnlyMeshData](xref:UnityEngine.Mesh.AcquireReadOnlyMeshData(UnityEngine.Mesh[])) with a collection of meshes you want to encode.
2. Iterate over the meshes and invoke [EncodeMesh](xref:Draco.Encode.DracoEncoder.EncodeMesh(UnityEngine.Mesh,UnityEngine.Mesh.MeshData,System.Int32,System.Int32,System.Int32,System.Int32,System.Int32,System.Int32,System.Int32)) on each of them.
3. Await each encoding job, handle the results (e.g. store them to a file or upload them to a server) and [dispose the EncodeResult](xref:Draco.Encode.EncodeResult.Dispose).
4. Dispose the [MeshDataArray](xref:UnityEngine.Mesh.MeshDataArray) that [AcquireReadOnlyMeshData](xref:UnityEngine.Mesh.AcquireReadOnlyMeshData(UnityEngine.Mesh[])) returned.

## Encode Sample

A fully setup sample scene can be found in the [Encode Sample](sample-encode.md).

## Trademarks

*Unity* is a registered trademark of [*Unity Technologies*][unity].

*Draco&trade;* is a trademark of [*Google LLC*][GoogleLLC].

[GoogleLLC]: https://about.google/
[unity]: https://unity.com
