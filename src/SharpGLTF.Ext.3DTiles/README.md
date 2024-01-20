# SharpGLTF.Ext.3DTiles

This project contains the implementation of 3D Tiles support for SharpGLTF.

The following extensions are supported:


- EXT_Mesh_Features

Specs: https://github.com/CesiumGS/glTF/tree/proposal-EXT_mesh_features/extensions/2.0/Vendor/EXT_mesh_features

Samples: https://github.com/CesiumGS/3d-tiles-samples/blob/main/glTF/EXT_mesh_features

- EXT_Instance_Features

Specs: https://github.com/CesiumGS/glTF/tree/3d-tiles-next/extensions/2.0/Vendor/EXT_instance_features

Samples: https://github.com/CesiumGS/3d-tiles-samples/tree/main/glTF/GpuInstancesMetadata

- Ext_Structural_Metadata

Specs: https://github.com/CesiumGS/glTF/tree/proposal-EXT_structural_metadata/extensions/2.0/Vendor/EXT_structural_metadata

Samples: https://github.com/CesiumGS/3d-tiles-samples/tree/main/glTF/EXT_structural_metadata

Not supported: 

- External schema 

- min, max, scale and offset properties for StructuralMetadataClassProperty and PropertyAttributeProperty

## Unit testing 

## Reading 3D Tiles glTF files

The unit test project contains a set of glTF files that are used to test the implementation of the extensions. The glTF files 
are obtained from https://github.com/CesiumGS/3d-tiles-validator/tree/main/specs/data/gltfExtensions.

## Writing 3D Tiles glTF files

See the unit test project for examples of how to write glTF files with the extensions.

The unit tests writes glTF files like the samples from https://github.com/CesiumGS/3d-tiles-samples/tree/main/glTF


## Sample code 

### Reading a 3D Tiles glTF file with metadata

```csharp
    var model = ModelRoot.Load("sample.gltf");
    var structuralMetadataExtension = model.GetExtension<EXTStructuralMetadataRoot>();
    var meshFeaturesExtension = model.LogicalMeshes[0].Primitives[0].GetExtension<MeshExtMeshFeatures>();
```

## Writing a 3D Tiles glTF file with metadata

In the following sample a glTF with 1 triangle is created. The triangle contains metadata with
a name column. The name column is set to "this is featureId0". The triangle is assigned featureId 0.

```csharp
    int featureId = 0;
    var material = MaterialBuilder.CreateDefault().WithDoubleSide(true);

    var mesh = new MeshBuilder<VertexPositionNormal, VertexWithFeatureId, VertexEmpty>("mesh");
    var prim = mesh.UsePrimitive(material);

    var vt0 = VertexBuilder.GetVertexWithFeatureId(new Vector3(0, 0, 0), new Vector3(0, 0, 1), featureId);
    var vt1 = VertexBuilder.GetVertexWithFeatureId(new Vector3(1, 0, 0), new Vector3(0, 0, 1), featureId);
    var vt2 = VertexBuilder.GetVertexWithFeatureId(new Vector3(0, 1, 0), new Vector3(0, 0, 1), featureId);

    prim.AddTriangle(vt0, vt1, vt2);
    var scene = new SceneBuilder();
    scene.AddRigidMesh(mesh, Matrix4x4.Identity);
    var model = scene.ToGltf2();

    var rootMetadata = model.UseStructuralMetadata();
    var schema = rootMetadata.UseEmbeddedSchema("schema_001");

    var schemaClass = schema.UseClassMetadata("triangles");

    var nameProperty = schemaClass
        .UseProperty("name")
        .WithStringType();

    var propertyTable = schemaClass
        .AddPropertyTable(1);

    propertyTable
        .UseProperty(nameProperty)
        .SetValues("this is featureId0");

    foreach (var primitive in model.LogicalMeshes[0].Primitives)
    {
        var featureIdAttribute = new FeatureIDBuilder(1, 0, propertyTable);
        primitive.AddMeshFeatureIds(featureIdAttribute);
    }

    model.SaveGLTF(@"sample.gltf");
```

3D Tiles specific parts in the resulting glTF:

```
 "extensions": {
    "EXT_structural_metadata": {
      "propertyTables": [
        {
          "class": "triangles",
          "count": 1,
          "properties": {
            "name": {
              "stringOffsets": 3,
              "values": 2
            }
          }
        }
      ],
      "schema": {
        "classes": {
          "triangles": {
            "properties": {
              "name": {
                "type": "STRING"
              }
            }
          }
        },
        "id": "schema_001"
      }
    }
  },
  "extensionsUsed": [
    "EXT_structural_metadata",
    "EXT_mesh_features"
  ],
  "meshes": [
    {
      "name": "mesh",
      "primitives": [
        {
          "extensions": {
            "EXT_mesh_features": {
              "featureIds": [
                {
                  "attribute": 0,
                  "featureCount": 1,
                  "propertyTable": 0
                }
              ]
            }
          },
          "attributes": {
            "POSITION": 0,
            "NORMAL": 1,
            "_FEATURE_ID_0": 2
          },
          "indices": 3,
          "material": 0
        }
      ]
    }
  ],

  ```

  Sample loaded in Cesium:


  ![alt text](cesium_sample.png)

