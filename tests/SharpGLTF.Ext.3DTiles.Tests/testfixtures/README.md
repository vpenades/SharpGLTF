# Test fixtures

This directory contains glTF files used for testing 3D Tiles functionality.

3D Tiles Test fixtures are obtained from https://github.com/CesiumGS/3d-tiles-validator/tree/main/specs/data/gltfExtensions

## Validating

The files can be validated using the 3D Tiles Validator:

```
$ git clone https://github.com/CesiumGS/3d-tiles-validator
$ npx ts-node  ./3d-tiles-validator/src/main.ts --tileContentFile test.gltf
```
