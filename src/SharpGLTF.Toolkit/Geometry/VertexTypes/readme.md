# Toolkit Vertex API

#### Overview

glTF descrives a vertex buffer as a collection of accessors, where each accessor is described
with an attribute, a dimension and an encoding.

Also, glTF theoretically allows a huge range of combinations, but in practice, only a very
narrow number of combinations is valid. this means that glTF runtimes need to carefully
check the buffers layout.

In order to simplify mesh creation, SharpGLTF Toolkit introduces the concept of
vertex fragments, not in the sense of OpenGL shaders, but as components that can form
a full vertex in a vertex buffer.

The table below shows the possible combinations:

|Position|Material|Skinning _(maxBones×weights)_|
|-|
|Position|Empty|Empty|
|Position Normal|Color0|256 ×4
|Position Normal Tangent|Texture0|256 ×8
||Color0 Texture0|65536 ×4
||Color0 Texture0 Texture1|65536 ×8
||Color0 Color1 Texture0 Texture1|

By using these predefined building blocks, declaring a vertex structure can be
greatly simplified.

#### Implementation

SharpGLTF Toolkit defines these vertex fragment types
in `SharpGLTF.Geometry.VertexTypes` namespace:
  
- Position
  - `VertexPosition`
  - `VertexPositionNormal`
  - `VertexPositionNormalTangent`
- Material
  - `VertexEmpty`
  - `VertexColor1`
  - `VertexTexture1`
  - `VertexColor1Texture1`
  - `VertexColor1Texture2`
  - `VertexColor2Texture2`
- Skinning
  - `VertexEmpty`
  - `VertexJoints8x4`
  - `VertexJoints8x8`
  - `VertexJoints16x4`
  - `VertexJoints16x8`

#### Appendix

- [MeshBuilder documentation](../readme.md)

