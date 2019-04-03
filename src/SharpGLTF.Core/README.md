# SharpGLTF Core


#### Namespaces

##### Namespace Schema2

This namespace contains the collection of classes, structures and enumerations that
represent the bulk of the API to access directly to glTF2 documents.

It also contains the main entry point Object that represents a glTF2 model: `ModelRoot`

[Documentation](Schema2/README.md)

##### Namespace IO

This namespace contains the helper classes to support JSon serialization.

##### Namespace Memory

glTF2 stores array structures as encoded byte buffers that are not easy to handle directly.

[Documentation](Memory/README.md)

#### Extensions

- [x] [KHR_materials_pbrSpecularGlossiness](https://github.com/KhronosGroup/glTF/tree/master/extensions/2.0/Khronos/KHR_materials_pbrSpecularGlossiness)
- [x] [KHR_materials_unlit](https://github.com/KhronosGroup/glTF/tree/master/extensions/2.0/Khronos/KHR_materials_unlit)
- [x] [KHR_lights_punctual](https://github.com/KhronosGroup/glTF/tree/master/extensions/2.0/Khronos/KHR_lights_punctual)
- [x] [KHR_texture_transform](https://github.com/KhronosGroup/glTF/tree/master/extensions/2.0/Khronos/KHR_texture_transform) (WIP)
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




