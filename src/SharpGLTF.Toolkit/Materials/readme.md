# Toolkit material API

#### Overview

glTF materials specification has a rather complex architecture,
with a number of specialized objects, plus a good number of extensions.

In order to streamline and simplify access to materials, a Material Build
API is provided.

By default, SharpGLTF supports the default PBRMetallicRoughness shader style,
plus Unlit and PBRSpecularGlossiness extensions. Each shader style has its
own unique list of channels.

Each channel has a Texture, and a Vector4<X,Y,Z,W> parameter, where every
element has a different meaning, depending on the kind of channel:

|Channel|Shader Style|X|Y|Z|W|
|-|-|-|-|-|-|
|Normal|All|Scale
|Occlussion|All|Strength
|Emissive|All|Red|Green|Blue
|BaseColor|Metallic Roughness & Unlit|Red|Green|Blue|Alpha
|MetallicRoughness|Metallic Roughness|Metallic Factor|Roughness Factor
|Diffuse|Specular Glossiness|Diffuse Red|Diffuse Green|Diffuse Blue|Alpha
|SpecularGlossiness|Specular Glossiness|Specular Red|Specular Green|Specular Blue|Glossiness

#### Implementation

To create new materials, there's two ways of doing it; accessing directly to the glTF
materials namespace, or using the MaterialBuilder class.

The advantage of MaterialBuilder is that it allows to create stand alone materials
that can be easily edited and used to create glTF materials at any time.

Creating a standard material can be done like this:

```c#
var material = new Materials.MaterialBuilder("material1")
                .WithDoubleSide(true)
                .WithMetallicRoughnessShader()
                .WithChannelImage("Normal", "WaterBottle_normal.png")
                .WithChannelImage("Emissive", "WaterBottle_emissive.png")
                .WithChannelImage("Occlusion", "WaterBottle_occlusion.png")
                .WithChannelImage("BaseColor", "WaterBottle_baseColor.png")
                .WithChannelImage("MetallicRoughness", "WaterBottle_roughnessMetallic.png");
```

MaterialBuilder also supports a fallback material that will be used in case the main material is not
supported by the rendering engine. But due to glTF limitations, this feature is restricted only to
a main material using SpecularGlossiness shader, and the fallback material using MetallicRoughness
shader.
