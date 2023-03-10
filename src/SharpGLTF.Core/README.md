# SharpGLTF Core


#### Namespaces

##### .Animations

Contains classes to help decode and interpolate animation curves.

##### .IO

Code related to JSon serialization.

##### .Schema2

This namespace contains the collection of classes, structures and enumerations that
represent the bulk of the low level API to access glTF2 documents.

It also contains the main entry point Object that represents a glTF2 model: `ModelRoot`

[Additional info](Schema2/README.md)

##### .Runtime

Contains classes and types that can help evaluating a model.

Model evaluation can be useful for these tasks:
- Dumping a raw list of triangles of the whole scene, in their final positions.
- Rendering the model on a graphics engine.

##### .Memory

glTF2 stores structured arrays as encoded byte buffers that are not easy to read directly.

To facilitate buffered array IO, the __.Memory__ namespace provides a number of helper
classes and structures that let accessing the data seamlessly.

[Additional info](Memory/README.md)

##### .Transforms

A glTF model usually consist of a scene graph of nodes connected as a visual tree.
The relationship between nodes is defined with transforms, usually with 4x4 matrices.

It also handles the way a mesh is brought from its local space to world space, including skinning and morphing.


#### Extensions support

- [x] [KHR_materials_pbrSpecularGlossiness](https://github.com/KhronosGroup/glTF/tree/master/extensions/2.0/Khronos/KHR_materials_pbrSpecularGlossiness)
- [x] [KHR_materials_unlit](https://github.com/KhronosGroup/glTF/tree/master/extensions/2.0/Khronos/KHR_materials_unlit)
- [x] [KHR_lights_punctual](https://github.com/KhronosGroup/glTF/tree/master/extensions/2.0/Khronos/KHR_lights_punctual)
- [x] [KHR_texture_transform](https://github.com/KhronosGroup/glTF/tree/master/extensions/2.0/Khronos/KHR_texture_transform)
- [ ] [KHR_draco_mesh_compression](https://github.com/KhronosGroup/glTF/tree/master/extensions/2.0/Khronos/KHR_draco_mesh_compression)
  - Depends on [Google's Draco Project](https://github.com/google/draco) which is C++ ; *Help Needed*

- [ ] [KHR_techniques_webgl](https://github.com/KhronosGroup/glTF/tree/master/extensions/2.0/Khronos/KHR_techniques_webgl)

- [x] [MSFT_texture_dds](https://github.com/KhronosGroup/glTF/tree/master/extensions/2.0/Vendor/MSFT_texture_dds)
- [ ] [MSFT_lod](https://github.com/KhronosGroup/glTF/tree/master/extensions/2.0/Vendor/MSFT_lod)
  - When this extension is used, the model's visual tree needs to be abstracted, which requires an extensive API rework, or a full API layer.
- [ ] [MSFT_packing_normalRoughnessMetallic](https://github.com/KhronosGroup/glTF/tree/master/extensions/2.0/Vendor/MSFT_packing_normalRoughnessMetallic)
- [ ] [MSFT_packing_occlusionRoughnessMetallic](https://github.com/KhronosGroup/glTF/tree/master/extensions/2.0/Vendor/MSFT_packing_occlusionRoughnessMetallic)
- [ ] [AGI_articulations](https://github.com/KhronosGroup/glTF/tree/master/extensions/2.0/Vendor/AGI_articulations)
- [ ] [AGI_stk_metadata](https://github.com/KhronosGroup/glTF/tree/master/extensions/2.0/Vendor/AGI_stk_metadata)
- [ ] [EXT_lights_image_based](https://github.com/KhronosGroup/glTF/tree/master/extensions/2.0/Vendor/EXT_lights_image_based)
- [x] [EXT_texture_webp](https://github.com/KhronosGroup/glTF/tree/master/extensions/2.0/Vendor/EXT_texture_webp)
- [ ] [ADOBE_materials_thin_transparency](https://github.com/KhronosGroup/glTF/tree/master/extensions/2.0/Vendor/ADOBE_materials_thin_transparency)
- [x] [CESIUM_primitive_outline](https://github.com/KhronosGroup/glTF/tree/main/extensions/2.0/Vendor/CESIUM_primitive_outline)





