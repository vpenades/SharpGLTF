# Toolkit Scene API

#### Overview

Creating scenes with the toolkit API is slightly different than creating them
directly with the glTF Schema2 API.

In the glTF Schema2 API, you create a `Scene`, then you add some `Node` children to the
`Scene`, and then you fill some `Node` instances them with `Mesh` and `Skin` references.

```c#
scene = model.UseScene(0);
var n1 = scene.CreateNode();
n1.Mesh = ...
var n2 = scene.CreateNode();
n2.Mesh = ...
var n3 = scene.CreateNode();
n3.Mesh = ...
n3.Skin = ...
scene.SaveGLB("scene.glb");

```

The Toolkit API uses a more visual approach; you just add what you want to render
and how you want to render it, so every AddMesh method adds an mesh instance to render.

```c#
scene = new SceneBuilder();
scene.AddRigidMesh(...);
scene.AddRigidMesh(...);
scene.AddSkinnedMesh(...);
scene.SaveGLB("scene.glb");
```

In order to have a hierarchical tree of nodes, you use the NodeBuilder object, with
which you can create standalone nodes, or whole skeleton armatures.

```c#
var root = new NodeBuilder("root");
var child = root.CreateNode("child");

```

In this way, NodeBuilder armatures become just another asset, like a mesh or a material,
and a scene is just a collection of instances to be rendered.

When you save the scene, all assets are gathered and matched, including meshes, materials
and nodes, and the appropiate glTF Schema2 Scenes and Nodes are created under the hood to
match the SceneBuilder rendering intent.

Additionally, `NodeBuilder` instances support defining animation curves, which are contained
internally inside every `NodeBuilder` instance.