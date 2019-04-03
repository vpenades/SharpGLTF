<p align="center">
<img src="build/Icons/glTF2Sharp.png" height=128 />
</p>

---

### Overview

SharpGLTF is a NetStandard 2.0, .NET library designed to support
[Khronos Group glTF 2.0](https://github.com/KhronosGroup/glTF) file format.

The aim of this framework is:

- To be able to create, manipulate, load and save glTF2 documents.
- To provide a safe and easy to use high level API to produce 3D assets.

The current status of the library is preview alpha, but, for some use cases it is already usable.

#### Nuget Packages

- [SharpGLTF.Core](https://www.nuget.org/packages/SharpGLTF.Core)
- [SharpGLTF.Toolkit](https://www.nuget.org/packages/SharpGLTF.Toolkit)

Notice that SharpGLTF.1.0.0-Alpha4 has been deprecated.

#### Quickstart

A simple example of loading a glTF file and saving it as GLB:

```c#
var model = Schema2.ModelRoot.Load("model.gltf");
model.SaveGLB("model.glb");
```

#### Design

The framework is divided in two packages:

- __[SharpGLTF.Core](src/SharpGLTF.Core/README.md)__ provides the core glTF2 schema implementation,
read & write operations, and a low level API for direct document access.
- __[SharpGLTF.Toolkit](src/SharpGLTF.Toolkit/README.md)__ provides a high level API over the Core
package, adding convenient extensions and utilities to help creating meshes, materials and scenes.

#### Appendix

- [Khronos Group glTF-CSharp-Loader](https://github.com/KhronosGroup/glTF-CSharp-Loader)
- [Khronos Group UnityGLTF](https://github.com/KhronosGroup/UnityGLTF)
