# glTF2Sharp

glTF2Sharp is NetStandard 2.0, C# library designed to parse and create [Khronos Group glTF2](https://github.com/KhronosGroup/glTF) files.

The current status of the library is preview alpha, but, for some cases it is probably fully usable.

### Features

- [x] glTF2 code API generated from Schema.
- [x] Reading and writing *.gltf* files.
- [x] Reading and writing *.glb* files.
- [x] Logical Data Access.
- [x] Visual Tree Access.
- [x] Vertex and Index buffer decoding.
- [ ] gltf.Extra field ; *Help Need*
- [ ] Scene Evaluation.
- [ ] Mesh Building utilities.
- [ ] Animation utilities.
- [ ] Material utilities

### Supported Extensions

- [x] [KHR_materials_pbrSpecularGlossiness](https://github.com/KhronosGroup/glTF/tree/master/extensions/2.0/Khronos/KHR_materials_pbrSpecularGlossiness)
- [x] [KHR_materials_unlit](https://github.com/KhronosGroup/glTF/tree/master/extensions/2.0/Khronos/KHR_materials_unlit)
- [ ] [KHR_draco_mesh_compression](https://github.com/KhronosGroup/glTF/tree/master/extensions/2.0/Khronos/KHR_draco_mesh_compression)
  - Depends on [Google's Draco Project](https://github.com/google/draco) which is C++ ; *Help Needed*
- [ ] [KHR_lights_punctual](https://github.com/KhronosGroup/glTF/tree/master/extensions/2.0/Khronos/KHR_lights_punctual)
- [ ] [KHR_techniques_webgl](https://github.com/KhronosGroup/glTF/tree/master/extensions/2.0/Khronos/KHR_techniques_webgl)
- [ ] [KHR_texture_transform](https://github.com/KhronosGroup/glTF/tree/master/extensions/2.0/Khronos/KHR_texture_transform)
- [ ] [MSFT_texture_dds](https://github.com/KhronosGroup/glTF/tree/master/extensions/2.0/Vendor/MSFT_texture_dds)
- [ ] [MSFT_lod](https://github.com/KhronosGroup/glTF/tree/master/extensions/2.0/Vendor/MSFT_lod)
  - When this extension is used, the model's visual tree needs to be abstracted, which requires an extensive API rework.
- [ ] [MSFT_packing_normalRoughnessMetallic](https://github.com/KhronosGroup/glTF/tree/master/extensions/2.0/Vendor/MSFT_packing_normalRoughnessMetallic)
- [ ] [MSFT_packing_occlusionRoughnessMetallic](https://github.com/KhronosGroup/glTF/tree/master/extensions/2.0/Vendor/MSFT_packing_occlusionRoughnessMetallic)
- [ ] [AGI_articulations](https://github.com/KhronosGroup/glTF/tree/master/extensions/2.0/Vendor/AGI_articulations)
- [ ] [AGI_stk_metadata](https://github.com/KhronosGroup/glTF/tree/master/extensions/2.0/Vendor/AGI_stk_metadata)
- [ ] [EXT_lights_image_based](https://github.com/KhronosGroup/glTF/tree/master/extensions/2.0/Vendor/EXT_lights_image_based)
- [ ] [EXT_texture_webp](https://github.com/KhronosGroup/glTF/tree/master/extensions/2.0/Vendor/EXT_texture_webp)
- [ ] [ADOBE_materials_thin_transparency](https://github.com/KhronosGroup/glTF/tree/master/extensions/2.0/Vendor/ADOBE_materials_thin_transparency)

