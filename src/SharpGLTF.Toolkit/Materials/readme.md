# Toolkit material API

#### Overview

glTF materials specification has a rather complex architecture,
with a number of speciallised objects, plus a good number of extensions.

In order to streamline and simplify access to materials, a Material Build
API is provided.

By default, SharpGLTF supports the default PBRMetallicRoughness shader style,
plus Unlit and PBRSpecularGlossiness extensions. Each shader style has its
own unique list of channels.

Each channel has a Texture, and a Vector4<X,Y,Z,W> parameter, where every
element has a different meaning, depending on the kind of channel:

|Channel|Shader Style|X|Y|Z|W|
|-|
|Normal|All|Scale
|Occlussion|All|Strength
|Emissive|All|Red|Green|Blue
|BaseColor|Metallic Roughness & Unlit|Red|Green|Blue|Alpha
|MetallicRoughness|Metallic Roughness|Metallic Factor|Roughness Factor
|Diffuse|Specular Glossiness|Diffuse Red|Diffuse Green|Diffuse Blue|Alpha
|SpecularGlossiness|Specular Glossiness|Specular Red|Specular Green|Specular Blue|Glossiness