# glTF2 Sharp

This is my personal attempt to create a c# library to parse and also build
Khronos glTF2 files.

#### Development

The bulk of the code is generated from the schema using the glTF2Sharp.CodeGen tools.

All generated classes are declared by default as non public, and its fields private.
The public API functionality is provided with hand coded partial classes. Since this
is a work in progress, not all the features might be publicly available yet.

Internally, the ModelRoot object store almost all the model elements in plain lists,
and cross referencing is done by integer indices. The public API implentation tries
to simplify model access by resolving the references and offering a more C# friendly
API.

#### Extensions

Extensions support is experimental, and at best it will be implemented on extensions that
can be included seamlessly.

Then, there's extensions like Draco, which relies on [Google's DRACO](https://github.com/google/draco)
library which is a highly optimized C++ Library.

#### Examples

Many examples can be found in the Tests project, but in essence, loading a model
is as easy as this:

```c#
var model = Schema2.ModelRoot.Load("model.gltf");
```

#### Alternative glTF2 c# libraries
- [Khronos Group glTF-CSharp-Loader](https://github.com/KhronosGroup/glTF-CSharp-Loader)
- [Khronos Group UnityGLTF](https://github.com/KhronosGroup/UnityGLTF)
- [glTF viewer using SharpDX](https://github.com/ousttrue/DXGLTF)
