# Schema2 namespace

This namespace contains the collection of classes, structures and enumerations that
represent the bulk of the API to access directly to glTF2 documents.

Part of the code has been generated directly from the glTF2 schema using
[SharpGLTF.CodeGen tool](../../build/SharpGLTF.CodeGen).

Not all the objects of glTF2 are exposed directly in the public API, some remain private and
can be accessed indirectly with helper classes, for example, materials use this approach.

The main object that represent a glTF document in memory is `ModelRoot`. Internally, it stores
almost all the model elements in plain lists, and cross referencing is done by integer indices.
The public API implentation tries to simplify document access by resolving the references and
offering a more C# friendly API.

There's two ways of traverse a glTF document: you can directly access every individual element
using the `ModelRoot.Logical*` collections, which gives you direct access to almost all
the individual building blocks of the document.

But if you want to traverse the document as a visual graph, you start with `ModelRoot.DefaultScene`
and from there you navigate throught the different properties and collections.

`ModelRoot` also contains the methods to create, load and save a glTF document:

Creating a new glTF document:
```c#
var model = ModelRoot.CreateModel();
var scene = model.UseScene("Default Scene");
var node = scene.CreateNode("Node1");
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

Notice that GLB format has a huge restriction over glTF format, which is the requirement
to have just one binary buffer file. SharpGLTF detects this case, and if the glTF model
has multiple buffers, under the hood it clones the whole document, and adjusts the internal
buffers so there's always one buffer.