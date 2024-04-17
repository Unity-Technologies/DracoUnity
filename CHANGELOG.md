# Changelog
All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [5.1.3] - 2024-04-17

### Added
- Added Apple Privacy Manifest file to `/Plugins` directory.

### Fixed
- Non-web libraries are properly included/excluded from builds again.
- Apple iOS/tvOS/visionOS Device/Simulator SDK libraries are correctly included again, even when target SDK is switched during a session.

## [5.1.2] - 2024-03-25

### Fixed
- Error log message about missing meta file for `Runtime/Plugins/x86_64/draco_unity.bundle/Contents/MacOS/draco_unity`
- Incorrect error message when removing legacy WebGL sub-packages fails.
- Automatic legacy WebGL sub-package removal can be prevented via `DISABLE_SUB_PACKAGE_LOAD` environment variable in Unity 2020 (fixes timeouts in CI).
- Corrected vision OS support detection in tests, which fixes compilation across Unity versions.

## [5.1.1] - 2024-03-14

### Fixed
- Compiles on Unity 2023.2 again.

## [5.1.0] - 2024-03-12

### Added
- Windows ARM64 Editor support
- Apple tvOS support
- Apple visionOS support
- Web binaries

### Changed
- Unity version-specific web sub-packages are not required anymore and will be removed automatically from the project upon detection (to avoid linker conflicts during web builds).
- Removed dummy Editor tests.
- Updated Google Draco to version 1.5.7.
- Raised deployment target for iOS and tvOS to 11.0

### Removed
- Apple iOS 32-bit armv7 architectures

### Fixed
- Improved error message when using package on unsupported platform.

## [5.0.2] - 2024-01-30

### Fixed
- Apple iOS device and simulator SDK binaries are properly included/excluded, depending on target SDK.

## [5.0.1] - 2024-01-19

### Fixed
- Build compiler error about missing "ConditionalAttribute"

## [5.0.0] - 2024-01-13

### Added
- Support for efficient self-managed encoding of multiple meshes. Users may use the advanced Mesh API to acquire readable mesh data for multiple meshes at once and pass the data on to new `DracoEncoder.EncodeMesh` overloads that accept said `MeshData`.
- Vertex attributes information (draco identifier and dimensions) was added to `EncodeResult`
- Support for iOS simulator
- Support for Windows ARM64 architecture.
- Support for Android x86_64
- Package samples
  - *Draco Decoding*. Demonstrates how to decode Draco data at runtime.
  - *Draco Encoding*. Demonstrates how to encode Draco data at runtime.
  - *Scene/GameObject Encoding/Decoding via Menu*. Encode Meshes, GameObjects or entire Scenes via the Tools and Assets menu and have them decoded when the scene loads.
- Support for decoding generic Draco attributes into arbitrary Unity vertex attributes (e.g. tangents) via [`DecodeMesh`](xref:Draco.DracoDecoder.DecodeMesh*)'s `attributeIdMap` parameter.

### Changed
- Decoding API was refactored and harmonized with encoding. The main entry point now is [`DracoDecoder.DecodeMesh`](xref:Draco.DracoDecoder.DecodeMesh*).
  - [`decodeSettings`](xref:Draco.DecodeSettings) parameter encapsulates decode related settings.
  - `attributeIdMap` parameter allows Draco attribute identifier to Unity vertex attribute assignment.
- Encoding API was refactored and now utilizes [`QuantizationSettings`](xref:Draco.Encode.QuantizationSettings) and [`SpeedSettings`](xref:Draco.Encode.SpeedSettings).
- Much faster encoding due to the use of the C# Job System (threads)
- Faster encoding due to avoiding a full memory copy of the result
- All encoding methods are async now
- Readonly meshes now can be encoded in the Editor
- Removed Editor-only `sync` parameter from `DracoMeshLoader.ConvertDracoMeshToUnity` to make API stable (regardless of environment/scripting defines)
- WebGL native libraries are now installed via sub-packages
- Minimum required Unity version was decreased to 2020.3 (possible because the WebGL version restriction was lifted with the sub-packages)
- Automatic code formatting was applied to all source files
- Consolidated split libraries into a single library named `draco_unity`
- Updated Draco native library binaries to [3.1.0](https://github.com/Unity-Technologies/draco/releases/tag/unity%2F3.1.0)
- Bumped Burst dependency to version 1.8.11
- Renamed assembly definition `DracoEditor` to `Draco.Editor`
- Renamed assembly definition `DracoRuntimeTests` to `Draco.Tests`
- CI maintenance

### Deprecated
- `DracoMeshLoader` (in favor of [`DracoDecoder`](xref:Draco.DracoDecoder))
- `DracoEncoder.EncodeMesh` overloads that have many individual settings parameters instead of [`QuantizationSettings`](xref:Draco.Encode.QuantizationSettings)/[`SpeedSettings`](xref:Draco.Encode.SpeedSettings).

### Removed
- Obsolete console error about downgrading package for certain Unity versions
- Menu items under `Tools` -> `Draco`. They can be brought back by installing the *Draco Tools Menu* package sample.

### Fixed
- WebGL build with Unity 2022 and newer (due to WebGL sub-packages).
- Destroying temporary copy (instead of original) GameObject when encoding selected GameObject from the menu
- Reference assembly definitions in `DracoEncoder` by name instead of GUID to avoid package import errors.
- Decoded mesh's bounds are calculated and returned/set accordingly.
- Properly dispose NativeArrays in case of error (fixes atteneder/DracoUnity#53)
- Point clouds' index buffer is properly initialized (fixes atteneder/DracoUnity#64)
- Properly set root namespace on all assembly definitions
- Compilation succeeds on non-supported platforms
- Crash in async Editor import

## [4.1.0] - 2023-04-14

### Added
- Support for encoding point clouds (thanks [@camnewnham][camnewnham] for #46)
- Point cloud encoding unit test
- Component pad byte support enables things like 3 byte RGB color vertex attributes (thanks [@camnewnham][camnewnham] for #47)
- Encoding binaries for remaining platforms (Android, WSA, WebGL, iOS and Windows 32-bit)

### Changed
- Updated Draco native library binaries to [1.1.0](https://github.com/atteneder/draco/releases/tag/unity1.1.0)

### Removed
- 32-bit Linux binaries/support

### Fixed
- Unit Tests download URLs updated
- Editor imports now calculate the correct mesh bounds
- macOS binaries are now loaded on Apple Silicon properly

## [4.0.2] - 2022-01-20

### Fixed
- Theoretical crash on unsupported indices data type. Removes compiler warning about throwing exception in C# job.

## [4.0.1] - 2021-11-23

### Fixed
- Apple Silicon Unity Editor decoding (#34)
- Apple Silicon Runtime encoding

## [4.0.0] - 2021-10-27

### Changed
- WebGL library is built with Emscripten 2.0.19 now
- Minimum required version is Unity 2021.2

## [3.4.0] - 2023-04-14

### Added
- Support for encoding point clouds (thanks [@camnewnham][camnewnham] for #46)
- Point cloud encoding unit test
- Component pad byte support enables things like 3 byte RGB color vertex attributes (thanks [@camnewnham][camnewnham] for #47)
- Encoding binaries for remaining platforms (Android, WSA, WebGL, iOS and Windows 32-bit)

### Changed
- Minimum required Unity version is 2020.3 LTS now
- Updated Draco native library binaries to [1.1.0](https://github.com/atteneder/draco/releases/tag/unity1.1.0)

### Removed
- 32-bit Linux binaries/support
- WebGL native libraries

### Fixed
- Unit Tests download URLs updated
- Editor imports now calculate the correct mesh bounds
- macOS binaries are now loaded on Apple Silicon properly

## [3.3.2] - 2021-10-27

### Added
- Error message when users try to run DracoUnity 3.x Unity >=2021.2 combination targeting WebGL

## [3.3.1] - 2021-09-14

### Changed
- Data types SInt8, UInt8, SInt16 and UInt16 on normals, colors, texture coordinates and blend weights are treated as normalized values now

### Fixed
- Correct vertex colors (#27)

## [3.3.0] - 2021-09-11

### Added
- Point cloud support (thanks [@camnewnham][camnewnham] for #28)

## [3.2.0] - 2021-08-27

### Changed
- Improved render performance by reducing vertex streams for small meshes (see related [issue](https://github.com/atteneder/glTFast/issues/197))
- Less memory usage and better performance by creating 16-bit unsigned integer indices for small meshes
- Less memory usage by avoiding a temporary index buffer in native plug-in
- Raised version of Burst dependency to 1.4.11 (current verified)

## [3.1.0] - 2020-07-12

### Added
- `forceUnityLayout` parameter, to enforce a blend-shape and skinning compatible vertex buffer layout

## [3.0.3] - 2020-06-09

### Added
- Support for Lumin / Magic Leap

## [3.0.2] - 2020-05-26

### Fixed
- Resolved Burst compiler errors (unresolved symbols on macOS) by setting correct native library reference (fixes #18)

## [3.0.1] - 2021-05-21

### Fixed
- AOT Burst compilation errors

## [3.0.0] - 2021-05-18

### Changed
- `DracoMeshLoader`'s coordinate space conversion from right-hand (like in glTF) to left-hand (Unity) changed. Now this is performed by inverting the X-axis (before the Z-axis was inverted). Compared to the previous behaviour, meshes are rotated 180° along the up-axis (Y). This was done to better conform to the glTF specification.

## [2.0.1] - 2021-05-21

### Fixed
- AOT Burst compilation errors

## [2.0.0] - 2021-05-17

### Added
- Experimental encoding support (ability to convert Unity Meshes into compressed Draco)
- Performance improvements
  - Two-step decoding allows to do more work of step two in threaded Jobs
  - Utilizes Advanced Mesh API
  - Uses `MeshDataArray` to shift more work to Jobs (Unity 2020.2 and newer)
- Burst
- Unit tests
- Require Normals/Tangents parameter (necessity when using Advanced Mesh API). If true, even if the draco mesh does not have the required vertex attributes, buffers for them will get allocated and the values are calculated.
- Parameter for coordinate space conversion (was on by default before)

### Changes
- API is now async/await based
- Updated native Draco libraries (based on version 1.4.1)

## [1.4.0] - 2021-01-31

### Added
- Support for Apple Silicon on macOS
- Support for Universal Windows Platform (x86,x64,ARM and ARM64)

### Changed
- Re-built all libraries with updated environments (Xcode, Android NDK, Emscripten, etc.)
- WebAssembly lib is now built by draco CI as well

### Fixed
- macOS library is now excluded from other platform builds (thanks Cameron Newnham <cam@fologram.com>)

## [1.3.0] - 2020-09-17

### Added
- Support for bone weights and joints by providing attribute IDs. Needed for glTF skinning.

## [1.2.0] - 2020-02-24

### Changed
- Performance improvement: CreateMesh does not calculate missing normals or tangents anymore. Instead it provides its caller with all info necessary to decide for itself, if calculations are needed.

## [1.1.3] - 2020-02-22

### Added
- Support for Universal Windows Platform (not verified/tested myself)

## [1.1.2] - 2020-02-01

### Fixed
- Removed in-Editor error by adding missing Profiler.EndSample call

## [1.1.1] - 2019-11-22

### Fixed
- Calculate correct tangents if UVs are present (fixes normal mapping)

## [1.1.0] - 2019-11-21

### Changed
- Assume Draco mesh to be right-handed Y-up coordinates and convert the to Unity's left-handed Y-up by flipping the Z-axis.
- Unity 2018.2 backwards compatibility

### Fixed
- Reference assembly definition by name instead of GUID to avoid package import errors

## [1.0.1] - 2019-09-15

### Changed
- Updated Draco native library binaries
- iOS library is now ~15 MB instead of over 130 MB

## [1.0.0] - 2019-07-28

### Changed
- Recompiled dracodec_unity library with BUILD_FOR_GLTF flag set to true, which adds support for normal encoding and standard edge breaking.
- Opened up interface a bit, which enables custom mesh loading by users.

## [0.9.0] - 2019-07-11
- Initial release

[camnewnham]: https://github.com/camnewnham
