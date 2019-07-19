# SharpGLTF Toolkit

#### Overview

SharpGLTF.Toolkit is a collection of classes and utilities aimed to help
the developer in creating and editing glTF files in the easiest way possible.

Althought the Schema2 namespace API does support some editing capabilities, in
practice it stands for the word _"The JPEG of 3D"_ , which essentially means
that its internal structure is not designed to be easily editable.

so, although you can build glTF scenes directly with the Schema2 namespace API,
doing so is not trivial, and you will need to do a lot of trickery on your own,
specially for complex scenes and optimizing assets.

So the Toolkit API comes to cover the gap, and make things a bit easier to
create glTF assets programatically.

A lof ot the classes in the toolkit follow the `StringBuilder` API paradigm, which
is, you start from scratch and you keep adding elements to it until you finish.


#### Toolkit Namespaces

- [MeshBuilder.](Geometry/readme.md)
  - [Vertex formats.](Geometry/VertexTypes/readme.md)
- [MaterialBuilder.](Materials/readme.md)
- [SceneBuilder](Scenes/readme.md)

#### Roadmap

- Morphing support.
- [Mikktspace](https://github.com/tcoppex/ext-mikktspace) Tangent space calculation. *Help Need*
- GPU Evaluation.

