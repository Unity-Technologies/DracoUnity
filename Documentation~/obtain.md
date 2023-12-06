# Create Draco Files

For creating Draco&trade; (`.drc`) files, there are multiple options.

## Command Line Tool

Draco's [Encoding Tool][dracoEncoder] is a command line tool that is able to convert a mesh or point cloud into a Draco (`.drc`) file. It supports the following input types:

- [Wavefront OBJ (`.obj`)][obj]
- [PLY][ply]
- [STL][stl]

Example for encoding a Wavefront OBJ (`input.obj`) into a Draco file (`output.drc`):

```shell
draco_encoder -i input.obj -o output.drc
```

See the [`draco_encoder`][dracoEncoder] documentation for usage details.

Unfortunately there's no binary distribution, so you have to build the executable yourself following Draco's [build instructions][dracoBuild].

> **Tip:** Similar to the Encoding Tool there's a [glTF Transcoding Tool][dracoGltfTranscoder] that it able to apply Draco compression on glTF files. See [Draco and glTF](do-more.md#draco-and-gltf) for more information.

## Within Unity

In the [*Scene/GameObject Encoding/Decoding via Menu* sample](sample-scene-encode-decode.md) you'll find an Editor script that lets you encode meshes to Draco within the Editor.

## Trademarks

*Unity* is a registered trademark of [*Unity Technologies*][unity].

*Draco&trade;* is a trademark of [*Google LLC*][GoogleLLC].

*Khronos&reg;* is a registered trademark and [glTF&trade;][gltf] is a trademark of [The Khronos Group Inc][khronos].

[dracoBuild]: https://github.com/google/draco/blob/master/BUILDING.md
[dracoEncoder]: https://github.com/google/draco#encoding-tool
[dracoGltfTranscoder]: https://github.com/google/draco#gltf-transcoding-tool
[gltf]: https://www.khronos.org/gltf/
[GoogleLLC]: https://about.google/
[khronos]: https://www.khronos.org
[obj]: https://en.wikipedia.org/wiki/Wavefront_.obj_file
[ply]: https://en.wikipedia.org/wiki/PLY_(file_format)
[stl]: https://en.wikipedia.org/wiki/STL_(file_format)
[unity]: https://unity.com
