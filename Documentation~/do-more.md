# Going further

## Draco and glTF

[Draco&trade;][draco] files contain raw mesh or point cloud data only, but Draco compression can be used on mesh data within [glTF&trade;][gltf] asset files. This allows you to enrich your Draco data in many ways, for example:

- Transforms (location, rotation, scale) and object/node hierarchy
- Multiple meshes/point clouds per file
- Materials
- Animation
- Application specific meta-data

The [*Unity glTFast*][glTFast] package provides support for loading, importing and exporting glTF in Unity and utilizes *Draco for Unity* to gain Draco compression support. You have to install recent versions of both packages to unlock that feature. See the package's [documentation][glTFast] to learn how to create and load Draco compressed glTF files.

> **Info:** Support for Draco in glTF is available via the [KHR_draco_mesh_compression][DracoGltfExt] glTF extension.

## Trademarks

*Unity&reg;* is a registered trademark of [Unity Technologies][unity].

*Draco&trade;* is a trademark of [*Google LLC*][GoogleLLC].

*Khronos&reg;* is a registered trademark and [glTF&trade;][gltf] is a trademark of [The Khronos Group Inc][khronos].

[draco]: https://google.github.io/draco
[DracoGltfExt]: https://github.com/KhronosGroup/glTF/blob/main/extensions/2.0/Khronos/KHR_draco_mesh_compression/README.md
[gltf]: https://www.khronos.org/gltf/
[glTFast]: https://docs.unity3d.com/Packages/com.unity.cloud.gltfast@latest/
[GoogleLLC]: https://about.google/
[khronos]: https://www.khronos.org
[unity]: https://unity.com
