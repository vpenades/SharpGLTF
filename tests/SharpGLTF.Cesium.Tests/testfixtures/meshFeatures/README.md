

The files in this directory are used for the specs for the `EXT_mesh_features`
validation.

The valid files have been taken from
https://github.com/CesiumGS/3d-tiles-samples/tree/a256d9f68df15bbfc75ea3891f52c72a36d04202/glTF/EXT_mesh_features

The `ValidFeatureIdTexture.glb` and `ValidFeatureIdAttributeDefault/` are 
intended for basic tests of binary- and default (non-embedded) glTF assets. 

The `ValidFeatureIdAttributeWithByteStride.glb` was created from the original
`ValidFeatureIdTexture.gltf` by passing it through https://gltf.report/ , which 
happens to write all attributes in an interleaved way, causing a byte stride 
to be inserted. 

The other files (starting with `FeatureIdTexture*` or `FeatureIdAttribute*`)
have been edited to cause validation errors (with the error indicated by 
their file name, as far as reasonably possible). 