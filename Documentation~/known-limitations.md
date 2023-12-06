# Known limitations

## Draco bit-stream version

*Draco for Unity* supports bit-stream version 2,2 (details in the [specification](https://google.github.io/draco/spec/)). Older Draco data (with a bit-stream version is older than 2,2) might not be supported. See Draco compiler option [`DRACO_BACKWARDS_COMPATIBILITY`](https://github.com/google/draco/blob/9f856abaafb4b39f1f013763ff061522e0261c6f/cmake/draco_options.cmake#L94) for more information.

That implies support for the following vertex attribute types

- Position
- Normal
- Color
- Texture coordinate

In addition, support for bone joints and weights (required for blend shapes) can be achieved via generic attributes and attribute ID parameters. See [Blend shapes parameters](use-case-decoding.md#blend-shapes-parameters) for instructions.

Draco bit-stream version 2,2 does not support vertex attribute types *tangent* and *material*.

## Sub-mesh

Multiple [sub-meshes](xref:Mesh.subMeshCount) are currently not supported. [DracoDecoder](xref:Draco.DracoDecoder) will always return a mesh with a single sub-mesh and the [DracoEncoder](xref:Draco.Encode.DracoEncoder) produces one result/Draco&trade; mesh per sub-mesh.

## Animation

Encoding animation (which was added in [Draco 1.3.4](https://github.com/google/draco#version-134-release)) is not supported.

## Edge breaker support

Only the standard edge breaker is supported, the predictive edge breaker is not.

## Attribute Deduplication

Attribute Deduplication is not enabled. See Draco compiler option [`DRACO_DECODER_ATTRIBUTE_DEDUPLICATION`](https://github.com/google/draco/blob/9f856abaafb4b39f1f013763ff061522e0261c6f/cmake/draco_options.cmake#L98) for more information.

## Trademarks

*Unity* is a registered trademark of [*Unity Technologies*][unity].

*Draco&trade;* is a trademark of [*Google LLC*][GoogleLLC].

[GoogleLLC]: https://about.google/
[unity]: https://unity.com
