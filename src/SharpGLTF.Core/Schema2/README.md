# SharpGLTF.Schema2

This namespace contains the collection of classes, structures and enumerations that
represent the bulk of the API to access directly to glTF v2 documents.

Part of the code has been generated directly from the glTF v2 schema using
[SharpGLTF.CodeGen tool](../../../build/SharpGLTF.CodeGen).

Not all the objects of glTF v2 are exposed directly in the public API, some remain private and
can be accessed indirectly with helper classes, for example, materials use this approach.

The main object that represent a glTF document in memory is `ModelRoot`. Internally, it stores
almost all the model elements in plain lists, and cross referencing is done by integer indices.
The public API implentation tries to simplify document access by resolving the references and
offering a more C# friendly API.

There's two ways to traverse a glTF document; you can directly access every individual element
using the `ModelRoot.Logical*` collections, which gives you direct access to almost all
the individual building blocks as they where originally stored in the document.

But if you want to traverse the document as a visual tree graph, you start with `ModelRoot.DefaultScene`
and from there you navigate throught the different nodes and properties using the `.Visual*` properties.

`ModelRoot` also contains the methods to create, load and save a glTF document:

Creating a new glTF document:
```c#
var model = SharpGLTF.Schema2.ModelRoot.CreateModel();
var root = model.UseScene(0).CreateNode("root node");
root.CreateNode("child node");
model.SaveGLB("model.glb");
```

Loading a glTF document:
```c#
var model = Schema2.ModelRoot.Load("model.gltf");
```

Loading a gltf and saving it as glb:
```c#
var model = Schema2.ModelRoot.Load("model.gltf");
model.SaveGLB("model.glb");
```

Editing existing glTF models has very limited support, because glTF models are not
designed to be edited. In particular, removing elements is essentially impossible
because in many cases data can be shared between elements and would require expensive
internal data reshuffle.

For glTF edition purposes, refer to [__SharpGLTF.Toolkit__](../../SharpGLTF.Toolkit/README.md)