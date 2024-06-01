<p align="center">
<img src="build/Icons/glTF2Sharp.png" height=128 />
</p>

![GitHub](https://img.shields.io/github/license/vpenades/SharpGLTF)
[![Nuget (with prereleases)](https://img.shields.io/nuget/vpre/SharpGLTF.Core)](https://www.nuget.org/packages?q=sharpgltf)
[![Join the chat at https://discord.gg/ecGAbsUDbB](https://img.shields.io/discord/750776153681166467?color=%237289DA&label=SharpGLTF&logo=discord&logoColor=white)](https://discord.gg/ecGAbsUDbB)
---

### Overview

__SharpGLTF__ is a 100% .NET Standard library designed to support
[Khronos Group glTF 2.0](https://github.com/KhronosGroup/glTF) file format.

The library is divided into three main packages:

- [__SharpGLTF.Core__](src/SharpGLTF.Core/README.md) provides read/write file support, and low level access to the glTF models.
- [__SharpGLTF.Runtime__](src/SharpGLTF.Runtime/README.md) helper classes to simplify gltf model rendering.
- [__SharpGLTF.Toolkit__](src/SharpGLTF.Toolkit/README.md) provides convenient utilities to help create, manipulate and evaluate glTF models.

#### Nuget Packages

|Package|Version|
|-|-|
|[__SharpGLTF.Core__](https://www.nuget.org/packages/SharpGLTF.Core)|![Nuget (with prereleases)](https://img.shields.io/nuget/vpre/SharpGLTF.Core)|
|[__SharpGLTF.Runtime__](https://www.nuget.org/packages/SharpGLTF.Runtime)|![Nuget (with prereleases)](https://img.shields.io/nuget/vpre/SharpGLTF.Runtime)|
|[__SharpGLTF.Toolkit__](https://www.nuget.org/packages/SharpGLTF.Toolkit)|![Nuget (with prereleases)](https://img.shields.io/nuget/vpre/SharpGLTF.Toolkit)|

#### Quickstart

A simple example of loading a glTF file and saving it as GLB:

```c#
var model = SharpGLTF.Schema2.ModelRoot.Load("model.gltf");
model.SaveGLB("model.glb");
```

More examples can be found [here](examples) and in the Test project.

#### Appendix

- [Khronos Group glTF-CSharp-Loader](https://github.com/KhronosGroup/glTF-CSharp-Loader)
- [Khronos Group UnityGLTF](https://github.com/KhronosGroup/UnityGLTF)
