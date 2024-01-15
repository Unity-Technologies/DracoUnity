# Upgrade

These guides will help you upgrade your project to use the latest version of *Draco for Unity*. If you still encounter problems, help us improving this guide and *Draco for Unity* in general by reaching out or by raising an issue.

## Upgrading to *Draco for Unity* 5.0.0

Minimum required Unity version was decreased to 2020.3, so you are able to upgrade from DracoUnity 3 (skipping version 4).

Support for building to WebGL is provided by a Unity version specific WebGL sub-package. Upon package installation, you'll be prompted to install said package automatically.

Assembly definition `DracoEditor` was renamed to `Draco.Editor`, so make sure you update your references.

### API changes

[DracoMeshLoader](xref:Draco.DracoMeshLoader) is now obsolete. Please use the [DecodeMesh](xref:Draco.DracoDecoder.DecodeMesh*) methods for decoding Draco data from now on. Instead of many individual parameters, [`decodeSettings`](xref:Draco.DecodeSettings) and `attributeIdMap` now encapsulate all settings to fine-tune decoding. See [Attribute assignment via Draco identifier](use-case-decoding.md#attribute-assignment-via-draco-identifier) for details about attribute assignment via `attributeIdMap`.

In similar fashion, instead of many individual encode settings parameters, [QuantizationSettings](xref:Draco.Encode.QuantizationSettings) and [SpeedSettings](xref:Draco.Encode.SpeedSettings) can be passed to [EncodeMesh](xref:Draco.Encode.DracoEncoder.EncodeMesh*). The existing overloads have been marked obsolete.

### Draco Tools Menu

If you've used the *Tools* -> *Draco* menu items or one of the classes associated with them, those have been removed from the package and put into the package sample *Scene/GameObject Encoding/Decoding via Menu*. You can install it from the package manager directly to your project.

## Unity Fork

With the release of version 5.0.0 the package name and identifier were changed to *Draco for Unity* (`com.unity.cloud.draco`) for the following reasons:

- Better integration into Unity internal development processes (including quality assurance and support)
- Distribution via the Unity Package Manager (no scoped libraries required anymore)

For now, both the Unity variant and the original version will receive updates.

### Transition to *Draco for Unity*

The C# namespaces are identical between the variants, so all you need to do is:

- Removed original *Draco 3D Data Compression* (with package identifier `com.atteneder.draco`).
- Add *Draco for Unity* (`com.unity.cloud.draco`).
- Update assembly definition references (if your project had any).
- Update any dependencies in your packages manifest (if your package had any)

### Keep using the original Draco 3D Data Compression

The original *Draco 3D Data Compression* (`com.atteneder.draco`) will still receive identical updates for now. You may choose to continue using it.

If you've installed the packages via the installer script (i.e. via [OpenUPM][OpenUPM] scoped registry - the recommended way), you don't need to change anything. You'll receive updates as usual.

See [Original *Draco 3D Data Compression*](./original.md) for instructions to install the original version from scratch.

## Trademarks

*Unity* is a registered trademark of [*Unity Technologies*][unity].

*Draco&trade;* is a trademark of [*Google LLC*][GoogleLLC].

[GoogleLLC]: https://about.google/
[OpenUPM]: https://openupm.com/
[unity]: https://unity.com
