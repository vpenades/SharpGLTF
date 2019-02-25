# glTF2 Sharp

#### Development

The bulk of the code is generated from the schema using the SharpGLTF.CodeGen tools.

All generated classes are declared by default as non public, and its fields private.
The public API functionality is provided with hand coded partial classes. Since this
is a work in progress, not all the features might be publicly available yet.

Internally, the ModelRoot object store almost all the model elements in plain lists,
and cross referencing is done by integer indices. The public API implentation tries
to simplify model access by resolving the references and offering a more C# friendly
API.

#### Examples

Many examples can be found in the Tests project, but in essence, loading a model
is as easy as this:

```c#
var model = Schema2.ModelRoot.Load("model.gltf");
```

Loading a gltf and saving it as glb:
```c#
var model = Schema2.ModelRoot.Load("model.gltf");
model.MergeBuffers(); // this is required since glb only supports a single binary buffer.
model.SaveGLB("model.glb");
```
