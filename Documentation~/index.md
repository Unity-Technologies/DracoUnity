# Draco for Unity

Unity&reg; package that integrates the [Draco&trade; 3D data compression library][draco] within Unity.

Encoding triangular meshes or point clouds to Draco reduces their byte size to just a fraction of the original. The compressed meshes can be decoded at both runtime and in the Editor (by placing `.drc` files in the `Assets` folder).

> [!NOTE]
> This package can be used to load or create Draco compressed [glTF][gltf] files. See [Draco and glTF](do-more.md#draco-and-gltf) for more information.

## Trademarks

*Unity* is a registered trademark of [*Unity Technologies*][unity].

*Draco&trade;* is a trademark of [*Google LLC*][GoogleLLC].

*Khronos&reg;* is a registered trademark and [glTF&trade;][gltf] is a trademark of [The Khronos Group Inc][khronos].

[draco]: https://google.github.io/draco
[gltf]: https://www.khronos.org/gltf/
[GoogleLLC]: https://about.google/
[khronos]: https://www.khronos.org
[unity]: https://unity.com
