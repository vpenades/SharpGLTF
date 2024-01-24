The files in this directory are used for the specs for the 
`EXT_structural_metadata` validation.

The valid files have been taken from
https://github.com/CesiumGS/3d-tiles-samples/tree/a256d9f68df15bbfc75ea3891f52c72a36d04202/glTF/EXT_structural_metadata
except for the following ones, which have been created dedicatedly for these tests:

- `ValidPropertyAttributes.gltf`
- `ValidPropertyTextureEnums.gltf`

The files that cause issues have been created by modifying these files 
This mostly happened manually, with the exception of certain invalid values in 
property textures, that have been written out in this form by the generator.

Note that some parts of the validation specs are not covered here, because
they are covered by the main 3D Tiles Validator specs. Particularly:

- Validation of the metadata schema is covered with `./specs/data/schemas/`
- Validation of the property tables is covered with `./specs/data/subtrees/subtreePropertyTables*.json`


