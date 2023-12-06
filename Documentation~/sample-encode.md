# Sample: Draco Encoding

This sample encodes a sphere mesh into Draco&trade; data at runtime. Its scripts serve as a blueprint to create your own encoding logic.

## Install the sample

When you select *Draco for Unity* in the Package Manager, you'll be able to click on the *Samples* tab to see the list of samples. Find the *Draco Encoding* sample and click its *Import* button.

Its content will get imported into the sample folder `Assets/Samples/Draco for Unity/<package version>/Draco Encoding`.

## Run the sample

Open the scene *DracoEncode* from the sample folder. You'll notice a GameObjects named *EncodeMeshToDraco* that holds a sphere mesh (a [MeshFilter](xref:UnityEngine.MeshFilter)/[MeshRenderer](xref:UnityEngine.MeshRenderer) combination) and a component of type *EncodeMeshToDraco*.

If you enter Play Mode, the sphere mesh gets encoded to Draco data and is saved to a file in the [persistent data path folder]. A log in the console will inform you about the exact location of the resulting file:

```txt
Saved submesh 0 to <some path>/Sphere-submesh-0.drc
```

## Trademarks

*Unity* is a registered trademark of [*Unity Technologies*][unity].

*Draco&trade;* is a trademark of [*Google LLC*][GoogleLLC].

[GoogleLLC]: https://about.google/
[unity]: https://unity.com
