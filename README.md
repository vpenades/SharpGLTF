<p align="center">
<img src="build/Icons/glTF2Sharp.png" height=128 />
</p>

---

### Overview

SharpGLTF is a NetStandard 2.0, .NET library designed to support
[Khronos Group glTF 2.0](https://github.com/KhronosGroup/glTF) file format.

The current status of the library is preview alpha, but, for some use cases it is already usable.

#### Design

The framework is divided in two packages:

- __SharpGLTF.Core__ provides the core glTF2 schema implementation, read & write operations,
and low level API access to let direct document manipulation.
- __SharpGLTF.Toolkit__ provides an abstraction layer over the Core package, adding convenient
extensions and utilities to help creating meshes, materials and scenes.

#### Examples

- [Load and save glTF and GLB files.](https://github.com/vpenades/SharpGLTF/blob/3dfe005ba7210c8327867127681a2b39aa567412/tests/SharpGLTF.Tests/Schema2/LoadAndSave/LoadSampleTests.cs#L32)
- [Create a simple triangle.](https://github.com/vpenades/SharpGLTF/blob/3dfe005ba7210c8327867127681a2b39aa567412/tests/SharpGLTF.Tests/Schema2/Authoring/BasicSceneCreationTests.cs#L95)
- [Create a textured triangle.](https://github.com/vpenades/SharpGLTF/blob/3dfe005ba7210c8327867127681a2b39aa567412/tests/SharpGLTF.Tests/Schema2/Authoring/BasicSceneCreationTests.cs#L139)

### Features

#### Source Code
- [x] glTF 2.0 code API generated from Schema.
- [x] Helper classes for encoding / decoding buffers
- [x] Logical Data Access.
- [x] Visual Tree Access.

#### Read / Write
- [x] Reading and writing of *.gltf* files.
- [x] Reading and writing of *.glb* files.
- [x] Reading Base64 encoded buffers.
- [x] Support of merging buffers to write one buffer *.glb* files.

#### To do:
- [ ] Writing Base64 encoded buffers.
- [ ] Scene Evaluation.
- [ ] GPU Evaluation.
- [ ] Mesh Building utilities.
- [ ] Animation utilities.
- [ ] Material utilities
- [ ] Skinning utilities
- [ ] Morphing utilities
- [ ] [Mikktspace](https://github.com/tcoppex/ext-mikktspace) Tangent space calculation. *Help Need*

### Supported extensions

- [x] [KHR_materials_pbrSpecularGlossiness](https://github.com/KhronosGroup/glTF/tree/master/extensions/2.0/Khronos/KHR_materials_pbrSpecularGlossiness)
- [x] [KHR_materials_unlit](https://github.com/KhronosGroup/glTF/tree/master/extensions/2.0/Khronos/KHR_materials_unlit)
- [x] [KHR_lights_punctual](https://github.com/KhronosGroup/glTF/tree/master/extensions/2.0/Khronos/KHR_lights_punctual)
- [x] [KHR_texture_transform](https://github.com/KhronosGroup/glTF/tree/master/extensions/2.0/Khronos/KHR_texture_transform) (WIP)

### Unsupported extensions

- [ ] [KHR_draco_mesh_compression](https://github.com/KhronosGroup/glTF/tree/master/extensions/2.0/Khronos/KHR_draco_mesh_compression)
  - Depends on [Google's Draco Project](https://github.com/google/draco) which is C++ ; *Help Needed*

- [ ] [KHR_techniques_webgl](https://github.com/KhronosGroup/glTF/tree/master/extensions/2.0/Khronos/KHR_techniques_webgl)

- [ ] [MSFT_texture_dds](https://github.com/KhronosGroup/glTF/tree/master/extensions/2.0/Vendor/MSFT_texture_dds)
- [ ] [MSFT_lod](https://github.com/KhronosGroup/glTF/tree/master/extensions/2.0/Vendor/MSFT_lod)
  - When this extension is used, the model's visual tree needs to be abstracted, which requires an extensive API rework, or a full API layer.
- [ ] [MSFT_packing_normalRoughnessMetallic](https://github.com/KhronosGroup/glTF/tree/master/extensions/2.0/Vendor/MSFT_packing_normalRoughnessMetallic)
- [ ] [MSFT_packing_occlusionRoughnessMetallic](https://github.com/KhronosGroup/glTF/tree/master/extensions/2.0/Vendor/MSFT_packing_occlusionRoughnessMetallic)
- [ ] [AGI_articulations](https://github.com/KhronosGroup/glTF/tree/master/extensions/2.0/Vendor/AGI_articulations)
- [ ] [AGI_stk_metadata](https://github.com/KhronosGroup/glTF/tree/master/extensions/2.0/Vendor/AGI_stk_metadata)
- [ ] [EXT_lights_image_based](https://github.com/KhronosGroup/glTF/tree/master/extensions/2.0/Vendor/EXT_lights_image_based)
- [ ] [EXT_texture_webp](https://github.com/KhronosGroup/glTF/tree/master/extensions/2.0/Vendor/EXT_texture_webp)
- [ ] [ADOBE_materials_thin_transparency](https://github.com/KhronosGroup/glTF/tree/master/extensions/2.0/Vendor/ADOBE_materials_thin_transparency)

#### Alternative glTF2 c# libraries
- [Khronos Group glTF-CSharp-Loader](https://github.com/KhronosGroup/glTF-CSharp-Loader)
- [Khronos Group UnityGLTF](https://github.com/KhronosGroup/UnityGLTF)
