# Sample: Scene Encode and Decode

One question many, who consider using 3D data compression want to get answered is how Draco&trade; impacts the transmission size (in bytes) and the loading/decoding time (time to first render).

This sample provides rudimentary tools and scripts to test this on your own scene or GameObject. The rough workflow is as follows.

- Collect all meshes referenced in a scene (limited to reference by [MeshFilter](xref:UnityEngine.MeshFilter))
- Encode them into Draco [TextAsset](xref:UnityEngine.TextAsset)s
- Remove the original Mesh references (emptying the scene)
- Setup a `DracoSceneDecoder` component, which decodes the Draco [TextAssets](xref:UnityEngine.TextAsset) at runtime and re-assigns them to their original [MeshFilter](xref:UnityEngine.MeshFilter)

## Before you start

> **Important**: Backup your project before trying the sample. The changes the tools/scripts in this sample perform on your scene/project are destructive and can lead to data loss.

### Test scene

You need to have a test scene that contains at least one mesh, referenced by a [MeshFilter](xref:UnityEngine.MeshFilter). This can be an existing scene of yours.

A quick way to create a test scene yourself is to create an empty scene and throw a couple of stock 3D Objects, like *Sphere*, *Capsule* or *Cylinder* for example (from the *GameObject* > *3D Object* menu).

Make sure you saved your scene.

## Install the sample

When you select *Draco for Unity* in the Package Manager, you'll be able to click on the *Samples* tab to see the list of samples. Find the *Scene/GameObject Encoding/Decoding via Menu* sample and click its *Import* button.

## Run the sample

### Encode the scene

Click on the *Tools* > *Draco* > *Setup Draco Decoder for Active Scene* menu item. It will encode all meshes referenced by the scene and save the into [TextAssets](xref:UnityEngine.TextAsset), stored in a folder besides your scene file with the same name as the scene. It'll then remove the references to the original meshes, leaving the scene empty.
In order to restore the scene at run-time, a GameObject named *MeshDecoder* with a *DracoSceneDecoder* component will be added. Its `instances` property contains a bunch of `DecodeInstance`, one per Mesh.

### Enter Play Mode

To test if the setup works, enter Play Mode. The Draco data is decoded and the resulting meshes will get re-assigned to the original MeshFilters. The scene should not have significant visual differences to the original.

Draco compression might come at a loss of precision, so there might be compression artifacts visible (e.g. in vertex positions).

Depending on the size of your scene, decoding might take some time and in that case some meshes might not be available for rendering within the first frames. They should appear as soon as they're decoded.

### Profile

Now that you've got your scene setup, you can create a development build, connect the Profiler to it and measure the loading time and impact of Draco decoding. The size of the compressed data can be observed by looking at the scene's TextAssets' file sizes.

## Trademarks

*Unity* is a registered trademark of [*Unity Technologies*][unity].

*Draco&trade;* is a trademark of [*Google LLC*][GoogleLLC].

[GoogleLLC]: https://about.google/
[unity]: https://unity.com
