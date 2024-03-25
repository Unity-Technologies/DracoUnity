# Draco for Unity

[![codecov](https://codecov.unity3d.com/ghe/unity/com.unity.cloud.draco/graph/badge.svg?token=1ZD3ZN3WRC)](https://codecov.unity3d.com/ghe/unity/com.unity.cloud.draco)

Unity package that integrates the [Draco 3D data compression library][draco] within Unity.

## Installing

[Installation instructions](./Documentation~/installation.md)

> **NOTE:** This package originally had the identifier `com.atteneder.draco`. Consult the [upgrade guide](./Documentation~/upgrade-guide.md#unity-fork) to learn how to switch to the Unity version (`com.unity.cloud.draco`) or [install the original package](./Documentation~/original.md).

### Build Draco library

The native libraries are built via CI in this [GitHub action](https://github.com/Unity-Technologies/draco/actions/workflows/unity.yml)

Look into the YAML file to see how the project is built with CMake.

## License

Copyright 2023 Unity Technologies and the *Draco for Unity* authors

Licensed under the Apache License, Version 2.0 (the "License");
you may not use files in this repository except in compliance with the License.
You may obtain a copy of the License at

   <http://www.apache.org/licenses/LICENSE-2.0>

Unless required by applicable law or agreed to in writing, software
distributed under the License is distributed on an "AS IS" BASIS,
WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
See the License for the specific language governing permissions and
limitations under the License.

## Third party notice

See [THIRD PARTY NOTICES.md](THIRD PARTY NOTICES.md)

## Trademarks

*Unity* is a registered trademark of [*Unity Technologies*][unity].

*Draco&trade;* is a trademark of [*Google LLC*][GoogleLLC].

[draco]: https://google.github.io/draco
[GoogleLLC]: https://about.google/
[unity]: https://unity.com
