
### Monogame Pipeline for loading glTF models at runtime

Before digging into the details, I want to clarify that loading a glTF model into an engine that fully supports
glTF capabilities is much easier than trying to load a glTF into an engine with limitations.

For example, technically it is possible to upload glTF vertex and index buffers straight away into the GPU, but that
means the shaders must support all the vertex formats you may encounter in a glTF's vertex buffer.

And because in this library I'm sticking to monoGame's default implementation, I am limited to use
BasicEffect and SkinnedEffect, so the vertices need to go through some extra processing that would not be required
otherwise.


### Requierements for a fully compliant glTF renderer:

For those willing to do the extra mile and add the missing bits, here's a list of things that need to be implemented in
MonoGame to fully support glTF

#### Image loading

PNG, Jpeg and DDS are already supported by MonoGame, but DDS is windows only. Ideally the solution would be to support
.KTX2 at runtime which would cover all platforms.

As a fallback solution, it could be possible to create a glTF extension to add support to reference .XNB textures; that
would align with MonoGame's pipeline philosophy.

### compression

SharpGLTF does not support draco compression because draco depends on a native library. But it could be interesting if
the a content pipeline could take uncompressed glTF files, output draco compressed glTF files and the runtime to load them.
This, of course, would require the draco compression libraries to be natively linked to monogame´s core.

#### Shaders.

glTF requires a quite powerful PBR shader, and not only that, it also requires the shader to support Skinning and morphing
simultaneously (skinning is applied after morphing)

Morphing is particularly tricky in monogame because it requires the morph targets to be stored in a separate buffer and
sampled by the vertex shader. This feature is beyond the capabilities of MonoGame's DX9 shader level.

