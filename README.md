<p align="center">
<img src="build/Icons/glTF2Sharp.png" height=128 />
</p>

![GitHub](https://img.shields.io/github/license/vpenades/SharpGLTF)
[![Nuget (with prereleases)](https://img.shields.io/nuget/vpre/SharpGLTF.Core)](https://www.nuget.org/packages?q=sharpgltf)
[![Join the chat at https://discord.gg/ecGAbsUDbB](https://img.shields.io/discord/750776153681166467?color=%237289DA&label=SharpGLTF&logo=discord&logoColor=white)](https://discord.gg/ecGAbsUDbB)
---

### Overview

__SharpGLTF__ is a 100% .NET Standard library designed to support [Khronos Group glTF 2.0](https://github.com/KhronosGroup/glTF) file format.

The library is divided into these main packages:

|Library|Nuget|Function|
|-|-|
|[__SharpGLTF.Core__](src/SharpGLTF.Core/README.md)|[![Nuget (with prereleases)](https://img.shields.io/nuget/vpre/SharpGLTF.Core)](https://www.nuget.org/packages/SharpGLTF.Core)|Read/Write file support, and low level access to the glTF models.|
|[__SharpGLTF.Runtime__](src/SharpGLTF.Runtime/README.md)|[![Nuget (with prereleases)](https://img.shields.io/nuget/vpre/SharpGLTF.Runtime)](https://www.nuget.org/packages/SharpGLTF.Runtime)|Helper classes to simplify gltf model rendering.|
|[__SharpGLTF.Toolkit__](src/SharpGLTF.Toolkit/README.md)|[![Nuget (with prereleases)](https://img.shields.io/nuget/vpre/SharpGLTF.Toolkit)](https://www.nuget.org/packages/SharpGLTF.Toolkit)|Convenience utilities to help create, manipulate and evaluate glTF models.|


Additionally, there's some optional extension libraries available:

- __SharpGLTF.Ext.Agi__
- [__SharpGLTF.Ext.3DTiles__](src/SharpGLTF.Ext.3DTiles/README.md)


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
