# What's new

Here's what changes since version 4.x (and 3.x respectively).

## Encoding

Encoding got much faster due to the use of the C# Job System (threads) and avoiding full memory copies of the result. All encoding methods are async now. Readonly meshes now can be encoded in the Editor.

## More Platforms

Support for a couple of platforms was added

- iOS simulator
- [Windows ARM64](https://learn.microsoft.com/en-us/windows/arm/overview)
- Android x86_64 (which enables usage on Chrome OS, Magic Leap 2 and certain Android emulators)

## WebGL now builds on all supported Unity versions

WebGL binaries are not compatible across Unity versions. To overcome this shortcoming, WebGL native binaries are now provided in a separate sub-package that is installed automatically.

### Advanced Mesh API support

Support for efficient, self-managed encoding of multiple meshes. Users may use the advanced Mesh API to acquire readable mesh data for multiple meshes at once and pass the data on to new `DracoEncoder.EncodeMesh` overloads that accept said `MeshData`.

## API Cleanup

The API was refactored, so that decoding Draco can be as simple as this:

```csharp
using Draco;

var mesh = await DracoDecoder.DecodeMesh(data);
```

If you've used [`DracoMeshLoader`](xref:Draco.DracoMeshLoader) in the past, don't worry. It's still there for compatibility reasons. Make sure to transition to [`DracoDecoder`](xref:Draco.DracoDecoder) soon though.

## Package Samples

Three package samples have been added:

- [*Draco Decoding*](sample-decode.md)
- [*Draco Encoding*](sample-encode.md)
- [*Scene/GameObject Encoding/Decoding via Menu*](sample-scene-encode-decode.md)

> **Note**: Parts of the *Scene/GameObject Encoding/Decoding via Menu* sample, including the *Tools* menu used to be part of the main package and have been moved.

## Fixes

Decoded meshes' bounds are calculated and returned or set accordingly. Point clouds' index buffer is properly initialized. With that potential rendering and culling problems should be gone.
