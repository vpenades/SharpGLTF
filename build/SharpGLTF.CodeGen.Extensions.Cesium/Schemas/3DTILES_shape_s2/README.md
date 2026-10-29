<!--
SPDX-FileCopyrightText: 2026 Bentley Systems, Incorporated

SPDX-License-Identifier: CC-BY-4.0
-->

# 3DTILES\_shape\_s2

## Contributors

- Sam Suhag, Cesium
- Sean Lilley, Cesium
- Peter Gagliardi, Cesium
- Marco Hutter, Cesium
- Adam Morris, Cesium

## Status

Draft

## Dependencies

Written against the glTF 2.1 spec.

Depends on [3DTILES_tileset](../3DTILES_tileset/README.md) and [EXT_geospatial_crs](../EXT_geospatial_crs/README.md).

Optionally, this extension may be used in conjunction with [3DTILES_implicit_tiling](../3DTILES_implicit_tiling/README.md).

## Required

This extension is required, meaning it **MUST** be placed in both `extensionsRequired` and `extensionsUsed`.

## Contents

- [Overview](#overview)
- [Hierarchy](#hierarchy)
- [Cell IDs](#cell-ids)
- [Tokens](#tokens)
- [S2 Shape](#s2-shape)
- [Implicit Subdivision](#implicit-subdivision)
  - [Availability](#availability)
- [Subtree Attributes](#subtree-attributes)
- [Schema](#schema)
- [Implementation Examples](#implementation-examples)

## Overview

[S2](https://s2geometry.io/) is a geometry library that defines a framework for decomposing the unit sphere into a hierarchy of cells. A cell is a quadrilateral bounded by four geodesics. The S2 cell hierarchy has 6 root cells, that are obtained by projecting the six faces of a cube onto the unit sphere.

Typically, traditional GIS libraries rely on projecting map data from an ellipsoid onto a plane. For example, the Mercator projection is a cylindrical projection, where the ellipsoid is mapped onto a cylinder that is then unrolled as a plane. This method leads to increasingly large distortion in areas as you move further away from the equator. S2 makes it possible to split the Earth into tiles that have no singularities, have relatively low distortion and are nearly equal in size.

This extension defines S2 cells as an additional shape type for glTF 2.1 shapes. Due to the properties of S2 described above, this shape is well suited for tilesets that span the whole globe.

This extension **SHOULD** only be used by tilesets in a [global coordinate system](../3DTILES_tileset/README.md#coordinate-reference-system-crs). This extension uses the ellipsoid specified by [EXT_geospatial_crs](../EXT_geospatial_crs/README.md). This shape **MAY** be used as an additional shape type for bounding volumes or by extensions.

Tile transforms do not apply to bounding volumes referencing S2 shapes. Tiles using this extension must maintain [spatial coherence](../3DTILES_tileset/README.md#spatial-coherence). This extension may be applied to tile or content bounding volumes. See [`3DTILES_tileset`](../3DTILES_tileset/README.md#transforms) for more details.

## Hierarchy

The [cell hierarchy of S2](https://s2geometry.io/devguide/s2cell_hierarchy) is rooted in the 6 faces of a cube, which are projected onto the unit sphere. In S2, each face of the unit cube can be subdivided into 30 levels using a quadtree structure, in which each cell on the grid subdivides into 4 equal cells at the subsequent level.

|Level 0|Level 1|
|:---:|:---:|
| ![](figures/plane_parent.png) S2 cell (`"1"`) on the Earth cube| ![](figures/plane_children.png) Children of S2 cell (`"1"`) on the Earth cube |
| ![](figures/ellipsoid_parent.png)  S2 cell (`"1"`) on the ellipsoid| ![](figures/ellipsoid_children.png) Children of S2 cell (`"1"`) on the ellipsoid|

The S2 library uses a modified Hilbert curve to provide a one dimensional ordering of cells on the S2 Earth cube. This provides each cell, from level 1 to level 30, with a unique 64-bit identifier. Using S2 cell IDs, centimeter scale areas be uniquely identified.

| S2 Curve on Earth cube |  S2 Curve on ellipsoid |
|:---:|:---:|
| ![](figures/plane.png)  | ![](figures/ellipsoid.png)  |

## Cell IDs

The 64-bit [S2 cell ID](https://s2geometry.io/devguide/s2cell_hierarchy#s2cellid-numbering) is constructed as follows:

1. Use 3 bits to encode the index of the root cell it belongs to. Valid values are in the range `[0-5]`.
2. For a cell at level `k`, for each of the `k` "child" values, add 2 bits to the right, indicating the selection of one of 4 children during subdivision.
3. Set the bit following the last child to `1`.
4. Set the remaining bits to `0`.

For example:

```
0011000000000...000   Root cell 1
0010110000000...000   2nd child of root cell 1
0010111100000...000   4th child of 2nd child of root cell 1
0010111001000...000   1st child of 4th child of 2nd child of root cell 1
```

In their decimal forms, the cell IDs above are represented as follows:

```
3458764513820540928   Root cell 1
3170534137668829184   2nd child of root cell 1
3386706919782612992   4th child of 2nd child of root cell 1
3332663724254167040   1st child of 4th child of 2nd child of root cell 1
```

## Tokens

To provide a more concise representation of the cell ID, as well as to provide a better indication of the level of the cell, we can use the hexadecimal form of the cell ID and remove any trailing zeros to obtain the cell's token.

For the cell IDs in the example above, the tokens are:

```
3     Root cell 1
2c    2nd child of root cell 1
2f    4th child of 2nd child of root cell 1
2e4   1st child of 4th child of 2nd child of root cell 1
```

## S2 Shape

An S2 cell shape is defined by adding the `3DTILES_shape_s2` extension to a `shape` object of type `"s2"`.

An S2 cell describes 4 positions on the surface of the ellipsoid forming the corners of a geodesic quadrilateral. To form a shape, the quadrilateral is extruded along normals to the ellipsoid. `minimumHeight` determines the height of the bottom surface of the shape, while `maximumHeight` determines the height of the top surface. Both `minimumHeight` and `maximumHeight` are expressed in meters above (or below) the ellipsoid.

### Properties

| Property | Type | Description | Required |
|---|---|---|---|
| **token** | `string` | A hexadecimal representation of the S2CellId. Tokens shall be lower-case, shall not contain whitespace and shall have trailing zeros stripped. | Yes |
| **minimumHeight** | `number` | The minimum height of the shape, specified in meters above (or below) the ellipsoid. | Yes |
| **maximumHeight** | `number` | The maximum height of the shape, specified in meters above (or below) the ellipsoid. | Yes |

> [!NOTE]
>
> When mapping the sphere to the cube, S2 provides three projection methods: linear, quadratic and tangential. This extension assumes an implementation uses the quadratic projection, since it is reasonably accurate and efficient.

> ![](figures/volume.jpg)
>
> S2 cell of the Philadelphia City Center area

S2 cell (`"89c6c7"`) covering the Philadelphia Center City area, with minimum height set to `0` meters and maximum height set to `1000` meters.

```json
"shapes": [
  {
    "name": "Philadelphia Center City S2 Cell Shape (Token: 89c6c7)",
    "type": "s2",
    "extensions": {
      "3DTILES_shape_s2": {
        "token": "89c6c7",
        "minimumHeight": 0,
        "maximumHeight": 1000
      }
    }
  }
]
```

The following example illustrates usage of `3DTILES_shape_s2` to represent all 6 faces of S2, representing a tileset with global coverage:

> ![](figures/globe.png)
>
> The 6 faces of S2 provide global coverage

```json
{
  "asset": {
    "version": "2.1"
  },
  "extensionsUsed": [
    "3DTILES_tileset",
    "3DTILES_shape_s2",
    "3DTILES_shape_ellipsoid_region",
    "EXT_geospatial_crs",
    "EXT_geospatial_crs_wkid"
  ],
  "extensionsRequired": [
    "3DTILES_tileset",
    "3DTILES_shape_s2",
    "3DTILES_shape_ellipsoid_region"
  ],
  "scenes": [
    {
      "name": "Global Terrain Coverage Scene",
      "nodes": [0]
    }
  ],
  "scene": 0,
  "shapes": [
    {
      "name": "WGS84 Ellipsoid Region Shape (Global Extent Reference)",
      "type": "ellipsoid region",
      "extensions": {
        "3DTILES_shape_ellipsoid_region": {
          "minimumHeight": 0.0,
          "maximumHeight": 250000.0,
          "minimumLongitude": -3.141592653589793,
          "maximumLongitude": 3.141592653589793,
          "minimumLatitude": -1.5707963267948966,
          "maximumLatitude": 1.5707963267948966
        }
      }
    },
    {
      "name": "S2 Cube Face 0 (Token: 1)",
      "type": "s2",
      "extensions": {
        "3DTILES_shape_s2": {
          "token": "1",
          "minimumHeight": 0,
          "maximumHeight": 1000000
        }
      }
    },
    {
      "name": "S2 Cube Face 1 (Token: 3)",
      "type": "s2",
      "extensions": {
        "3DTILES_shape_s2": {
          "token": "3",
          "minimumHeight": 0,
          "maximumHeight": 1000000
        }
      }
    },
    {
      "name": "S2 Cube Face 2 (Token: 5)",
      "type": "s2",
      "extensions": {
        "3DTILES_shape_s2": {
          "token": "5",
          "minimumHeight": 0,
          "maximumHeight": 1000000
        }
      }
    },
    {
      "name": "S2 Cube Face 3 (Token: 7)",
      "type": "s2",
      "extensions": {
        "3DTILES_shape_s2": {
          "token": "7",
          "minimumHeight": 0,
          "maximumHeight": 1000000
        }
      }
    },
    {
      "name": "S2 Cube Face 4 (Token: 9)",
      "type": "s2",
      "extensions": {
        "3DTILES_shape_s2": {
          "token": "9",
          "minimumHeight": 0,
          "maximumHeight": 1000000
        }
      }
    },
    {
      "name": "S2 Cube Face 5 (Token: b)",
      "type": "s2",
      "extensions": {
        "3DTILES_shape_s2": {
          "token": "b",
          "minimumHeight": 0,
          "maximumHeight": 1000000
        }
      }
    }
  ],
  "extensions": {
    "3DTILES_tileset": {
      "geometricError": 10000
    },
    "EXT_geospatial_crs": {
      "format": "wkid",
      "extensions": {
        "EXT_geospatial_crs_wkid": {
          "authority": "EPSG",
          "wkid": 4978
        }
      }
    }
  },
  "nodes": [
    {
      "name": "Global Region Root Tile Node",
      "boundingVolume": {
        "shape": 0
      },
      "extensions": {
        "3DTILES_tileset": {
          "geometricError": 10000,
          "refine": "REPLACE"
        }
      },
      "children": [1, 2, 3, 4, 5, 6]
    },
    {
      "name": "Cube Face 0 (Tile Node)",
      "boundingVolume": {
        "shape": 1
      },
      "extensions": {
        "3DTILES_tileset": {
          "geometricError": 5000
        }
      }
    },
    {
      "name": "Cube Face 1 (Tile Node)",
      "boundingVolume": {
        "shape": 2
      },
      "extensions": {
        "3DTILES_tileset": {
          "geometricError": 5000
        }
      }
    },
    {
      "name": "Cube Face 2 (Tile Node)",
      "boundingVolume": {
        "shape": 3
      },
      "extensions": {
        "3DTILES_tileset": {
          "geometricError": 5000
        }
      }
    },
    {
      "name": "Cube Face 3 (Tile Node)",
      "boundingVolume": {
        "shape": 4
      },
      "extensions": {
        "3DTILES_tileset": {
          "geometricError": 5000
        }
      }
    },
    {
      "name": "Cube Face 4 (Tile Node)",
      "boundingVolume": {
        "shape": 5
      },
      "extensions": {
        "3DTILES_tileset": {
          "geometricError": 5000
        }
      }
    },
    {
      "name": "Cube Face 5 (Tile Node)",
      "boundingVolume": {
        "shape": 6
      },
      "extensions": {
        "3DTILES_tileset": {
          "geometricError": 5000
        }
      }
    }
  ]
}
```

## Implicit Subdivision

When used with [`3DTILES_implicit_tiling`](../3DTILES_implicit_tiling/README.md), the implicit tile coordinates are interpreted as `(x, y, height)`.

A `QUADTREE` subdivision scheme will follow the rules for subdivision as defined by the S2 cell hierarchy. When an `OCTREE` subdivision scheme is used, the split in the vertical dimension occurs at the midpoint of the `minimumHeight` and `maximumHeight` of the parent tile.

| Cell  | Quadtree Subdivision | Octree Subdivision |
|---|---|---|
| ![](figures/parent.png)  | ![](figures/quadtree.png)  | ![](figures/octree.png)  |

To ensure continuity of the Hilbert curve, the faces of the cube are rotated as shown in the diagram below. This must be carefully considered when interpreting the tile coordinates in implicit tiling, since the traversal order in the odd-numbered faces is the mirror of the order in even numbered faces.

> ![](figures/s2-face-winding.png)
>
> S2 winding directions

### Availability

When using this extension with [`3DTILES_implicit_tiling`](../3DTILES_implicit_tiling/README.md), the availability bitstreams must be indexed in Morton order, as illustrated by the following diagram:

> ![](figures/availability.jpg)
>
> Comparison of S2, Hilbert order, and Morton order indexing. Implicit tiling requires Morton order.

The following example illustrates usage of `3DTILES_shape_s2` with [`3DTILES_implicit_tiling`](../3DTILES_implicit_tiling/README.md):

```json
{
  "asset": {
    "version": "2.1"
  },
  "extensionsUsed": [
    "3DTILES_tileset",
    "3DTILES_implicit_tiling",
    "3DTILES_shape_s2",
    "EXT_geospatial_crs",
    "EXT_geospatial_crs_wkid"
  ],
  "extensionsRequired": [
    "3DTILES_tileset",
    "3DTILES_implicit_tiling",
    "3DTILES_shape_s2"
  ],
  "scenes": [
    {
      "name": "Implicitly Tiled Scene",
      "nodes": [0]
    }
  ],
  "scene": 0,
  "shapes": [
    {
      "name": "Implicit Root Cell Volume - Level 0 (Token: 04)",
      "type": "s2",
      "extensions": {
        "3DTILES_shape_s2": {
          "token": "04",
          "minimumHeight": 0,
          "maximumHeight": 500000
        }
      }
    }
  ],
  "extensions": {
    "3DTILES_tileset": {
      "geometricError": 10000
    },
    "EXT_geospatial_crs": {
      "format": "wkid",
      "extensions": {
        "EXT_geospatial_crs_wkid": {
          "authority": "EPSG",
          "wkid": 4978
        }
      }
    }
  },
  "nodes": [
    {
      "name": "S2 Implicit Root Tile Node",
      "boundingVolume": {
        "shape": 0
      },
      "extensions": {
        "3DTILES_tileset": {
          "geometricError": 5000,
          "refine": "REPLACE"
        },
        "3DTILES_implicit_tiling": {
          "subdivisionScheme": "QUADTREE",
          "subtreeLevels": 4,
          "availableLevels": 8,
          "contentUri": "content/{level}/{x}/{y}.glb",
          "subtreeUri": "subtrees/{level}/{x}/{y}.subtree"
        }
      }
    }
  ]
}
```

## Subtree Attributes

This extension defines the following [subtree tile attribute semantics](../3DTILES_subtree/README.md#tile-attributes):

|Attribute Semantic|Accessor Type|Component Type|Description|
|---|---|---|---|
|`"TILE_BOUNDING_S2_CELL"`|`"SCALAR"`|`5135` (UNSIGNED INT64)|The bounding volume of the tile, expressed as an [S2 Cell ID](#cell-ids) using the 64-bit representation instead of the hexadecimal representation.|
|`"TILE_MINIMUM_HEIGHT"`|`"SCALAR"`|`5130` (DOUBLE)|The minimum height of the tile above (or below) the ellipsoid.|
|`"TILE_MAXIMUM_HEIGHT"`|`"SCALAR"`|`5130` (DOUBLE)|The maximum height of the tile above (or below) the ellipsoid.|

This extension defines the following [subtree content attribute semantics](../3DTILES_subtree/README.md#content-attributes):

|Attribute Semantic|Accessor Type|Component Type|Description|
|---|---|---|---|
|`"CONTENT_BOUNDING_S2_CELL"`|`"SCALAR"`|`5135` (UNSIGNED INT64)|The bounding volume of the content, expressed as an [S2 Cell ID](#cell-ids) using the 64-bit representation instead of the hexadecimal representation.|
|`"CONTENT_MINIMUM_HEIGHT"`|`"SCALAR"`|`5130` (DOUBLE)|The minimum height of the content above (or below) the ellipsoid.|
|`"CONTENT_MAXIMUM_HEIGHT"`|`"SCALAR"`|`5130` (DOUBLE)|The maximum height of the content above (or below) the ellipsoid.|

## Schema

- [shape.3DTILES_shape_s2.schema.json](schema/shape.3DTILES_shape_s2.schema.json)

## Implementation Examples

_This section is non-normative_

- [S2Geometry Reference C++ Implementation](https://github.com/google/s2geometry/tree/master/src/s2)
- [S2Geometry Reference Java Implementation](https://github.com/google/s2-geometry-library-java/tree/master/library/src/com/google/common/geometry)
- [S2Cell.js in CesiumJS](https://github.com/CesiumGS/cesium/blob/main/packages/engine/Source/Core/S2Cell.js)
