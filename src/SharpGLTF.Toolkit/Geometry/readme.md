# Toolkit Mesh API

#### Overview

Creating meshes from scratch accessing directly to `SharpGLTF.Schema2`
namespace API is quite complicated, since it implies creating Buffers,
Accessors, Primitives and Meshes very carefully.

It's even more complicated if we want to optimize the meshes by arrange
the vertices in an interleaved layout, or even allowing multiple meshes
to share a Vertex and Index buffer.

As an alternative to creating meshes directly into a glTF document,
SharpGLTF Toolkit provides a MeshBuilder class to simplify mesh creation.

#### Implementation

The most useful class for mesh creation is `MeshBuilder<TvP, TvM, TvS>`
where:

- TvP is a Position Vertex Fragment structure.
- TvM is a Material Vertex Fragment structure.
- TvS is a Skinning Vertex Fragment structure.

Vertex fragment types are described in [here](VertexTypes/readme.md).

```c#
// define a Mesh Builder with a Position and a Color.
var mesh = new MeshBuilder<VertexPosition, VertexColor1, VertexEmpty>("mesh");
```

`MeshBuilder` groups primitives using a material as a dictionary key.
The base implementation of `MeshBuilder` allows to pass a templated type as a
material key:

```c#
// define a MeshBuilder with "string" material and a vertex with Position and a Color.
var mesh = new MeshBuilder<String, VertexPosition, VertexColor1, VertexEmpty>("mesh");

mesh.UsePrimitive("Material1").AddTriangle((V1,C1), (V2,C2), (V3,C3));
mesh.UsePrimitive("Material2").AddTriangle((V1,C1), (V3,C3), (V4,C4));
```

But the default implementation of `MeshBuilder` uses `MaterialBuilder` as described
[here](../Materials/readme.md).

```c#
// define a material:
var material = new MaterialBuilder("material");

// define a MeshBuilder with a vertex with Position and a Color.
var mesh = new MeshBuilder<VertexPosition, VertexColor1, VertexEmpty>("mesh");

mesh.UsePrimitive(material).AddTriangle((V1,C1), (V2,C2), (V3,C3));
mesh.UsePrimitive(material).AddTriangle((V1,C1), (V3,C3), (V4,C4));
```

Finally, in order to convert a MeshBuilder to a glTF mesh:

```c#
var mesh1 = new MeshBuilder<VertexPosition, VertexColor1, VertexEmpty>("mesh1");
// fill mesh1 with geometry
var mesh2 = new MeshBuilder<VertexPosition, VertexColor1, VertexEmpty>("mesh2");
// fill mesh2 with geometry

var model = Schema2.ModelRoot.CreateModel();

model.CreateMeshes(mesh1, mesh2);
```

The interesting thing about `CreateMeshes` is that it is able to batch all the meshes and
its primitives in a single vertex and index buffer, provided they use equivalent vertex
structures.

#### Skinning support

Skinning attributes are provided by the third vertex fragment in the `MeshBuilder` definition.

If no skinning is required, a `VertexEmpty` placeholder is used instead.


#### Morphing support

Not supported yet.

#### Appendix

- [Vertex Fragments documentation](VertexTypes/readme.md)
- [Material Builder documentation](../Materials/readme.md)






