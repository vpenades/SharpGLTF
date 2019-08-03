# SharpGLTF Toolkit

#### Overview

__SharpGLTF.Toolkit__ is a collection of classes and utilities aimed to help
the developer in creating and editing glTF files in the easiest way possible.

Althought the Schema2 namespace API does support some editing capabilities, in
practice it stands for the word _"The JPEG of 3D"_ , which essentially means
that its internal structure is not designed to be easily editable. In general,
the Schema2 API can be considered as "_append only_" with very limited modification
capabilities.

So the Toolkit API comes to cover the gap, and make things a bit easier to
create glTF assets programatically.

#### Toolkit Namespaces

- [.Scenes](Scenes/readme.md)
- [.Geometry](Geometry/readme.md)
  - [.VertexTypes](Geometry/VertexTypes/readme.md)
- [.Materials](Materials/readme.md)

#### Roadmap

- Morphing support.
- [Mikktspace](https://github.com/tcoppex/ext-mikktspace) Tangent space calculation. *Help Need*
- GPU Evaluation.

