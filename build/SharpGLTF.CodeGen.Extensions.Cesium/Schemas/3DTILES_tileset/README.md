<!--
SPDX-FileCopyrightText: 2026 Bentley Systems, Incorporated

SPDX-License-Identifier: CC-BY-4.0
-->

# 3DTILES\_tileset

## Contributors

- Sean Lilley, Cesium
- Adam Morris, Cesium
- Tamrat Belayneh, ESRI

## Status

Draft

## Dependencies

Written against the glTF 2.1 spec.

## Required

This extension is required, meaning it **MUST** be placed in both `extensionsRequired` and `extensionsUsed`.

## Contents

- [Overview](#overview)
- [File Extensions](#file-extensions)
- [Constraints](#constraints)
- [Concepts](#concepts)
  - [3D Tiles](#3d-tiles)
  - [Tileset](#tileset)
  - [Tile](#tile)
  - [Content](#content)
  - [External Tilesets](#external-tilesets)
  - [Geometric Error](#geometric-error)
  - [Refinement](#refinement)
  - [Bounding Volumes](#bounding-volumes)
  - [Transforms](#transforms)
  - [Spatial Coherence](#spatial-coherence)
  - [Spatial Data Structures](#spatial-data-structures)
  - [Coordinate Reference System](#coordinate-reference-system-crs)
  - [Implicit Tiling](#implicit-tiling)
- [Supporting Extensions](#supporting-extensions)
  - [Metadata](#metadata)
  - [Layers](#layers)
  - [Horizon Occlusion Point](#horizon-occlusion-point)
  - [Visibility Bounding Volumes](#visibility-bounding-volumes)
  - [Declarative Styling](#declarative-styling)
- [Appendix A: Spatial Data Structures](#appendix-a-spatial-data-structures)

## Overview

This extension specifies a subset of glTF 2.1 for representing a tileset in [3D Tiles](https://github.com/CesiumGS/3d-tiles/tree/main/specification/). It extends the glTF node hierarchy to support Hierarchical Level of Detail (HLOD) for streaming massive 3D scenes. Additionally, it depends on core glTF 2.1 features like external assets and bounding volumes.

## File Extensions

Assets that use the `3DTILES_tileset` extension **SHOULD** use the `.tileset.gltf` or `.tileset.glb` file extensions. This convention helps differentiate tileset files from content files.

The entry tileset **SHOULD** be named `root.tileset.gltf` or `root.tileset.glb` to differentiate the entry tileset from [External Tilesets](#external-tilesets).

## Constraints

The following constraints apply when using the `3DTILES_tileset` extension:

- The document **MUST** have exactly one scene with exactly one node, the single root node.
- The `3DTILES_tileset` extension **MUST** be defined on all nodes.

## Concepts

### 3D Tiles

In 3D Tiles, a *tileset* is a set of *tiles* organized in a spatial data structure, the *tree*. Each tile may reference renderable *content*.

The content references a set of *features*, such as 3D models representing buildings or trees, or points in a point cloud. Each feature has position and appearance properties and additional application-specific properties. A client may choose to select features at runtime and retrieve their properties for visualization or analysis.

Tiles are organized in a tree which incorporates the concept of Hierarchical Level of Detail (HLOD) for optimal rendering of spatial data. Each tile has a *bounding volume*, an object defining a spatial extent completely enclosing its content. The tree has [spatial coherence](#spatial-coherence); the content for child tiles are completely inside the parent's bounding volume.

> ![](./figures/tree.png)
>
> A tree of tiles.

A tileset may use a 2D spatial tiling scheme similar to raster and vector tiling schemes (like a Web Map Tile Service (WMTS) or XYZ scheme) that serve predefined tiles at several levels of detail (or zoom levels). However since the content of a tileset is often non-uniform or may not easily be organized in only two dimensions, the tree can be any spatial data structure with spatial coherence, including k-d trees, quadtrees, octrees, and grids. [Implicit tiling](#implicit-tiling) defines a concise representation of quadtrees and octrees.

Application-specific *properties* may be provided at multiple granularities within a tileset. Properties may be associated with high-level entities like tilesets, tiles, contents, or features, or with individual vertices and texels. Properties conforms to a well-defined type system described by the [3D Metadata Specification](https://github.com/CesiumGS/3d-tiles/blob/main/specification/Metadata/), which may be extended with application- or domain-specific semantics.

Optionally a [3D Tiles Style](https://github.com/CesiumGS/3d-tiles/blob/main/specification/Styling/), or *style*, may be applied to a tileset. A style defines expressions to be evaluated which modify how each feature is displayed.

### Tileset

A tileset is a set of tiles organized in a spatial data structure, the tree. The tree has a single root tile. Each tile has zero or more children tiles. Tiles are represented as nodes in the glTF node hierarchy, or defined implicitly with [Implicit Tiling](#implicit-tiling).

3D Tiles uses one main tileset file as the entry point to define a tileset. To create a tree of trees, a tileset may also reference [external tilesets](#external-tilesets).

The following example shows a tree with a root tile and a child tile.

```json
{
  "extensionsUsed": ["3DTILES_tileset"],
  "extensionsRequired": ["3DTILES_tileset"],
  "asset": {
    "version": "2.1"
  },
  "scenes": [
    {
      "nodes": [0]
    }
  ],
  "scene": 0,
  "shapes": [
    {
      "type": "box",
      "box": {
        "size": [1.0, 1.0, 1.0]
      }
    },
    {
      "type": "box",
      "box": {
        "size": [0.9, 0.3, 1.0]
      }
    }
  ],
  "files": [
    {
      "mimeType": "model/gltf-binary",
      "uri": "root.glb"
    },
    {
      "mimeType": "model/gltf-binary",
      "uri": "child.glb"
    }
  ],
  "externalAssets": [
    {
      "file": 0
    },
    {
      "file": 1
    }
  ],
  "extensions": {
    "3DTILES_tileset": {
      "geometricError": 240.0
    }
  },
  "nodes": [
    {
      "extensions": {
        "3DTILES_tileset": {
          "geometricError": 70.0,
          "refine": "ADD"
        }
      },
      "boundingVolume": {
        "shape": 0
      },
      "externalAsset": 0,
      "children": [1]
    },
    {
      "extensions": {
        "3DTILES_tileset": {
          "geometricError": 0.0
        }
      },
      "boundingVolume": {
        "shape": 1
      },
      "externalAsset": 1
    }
  ]
}
```

The top-level `3DTILES_tileset` extension has the following properties:

- `geometricError` is a nonnegative number that defines the error, in meters, that determines if the tileset is rendered. At runtime, the geometric error is used to compute *Screen-Space Error* (SSE), the error measured in pixels. If the SSE does not exceed a required minimum, the tileset should not be rendered, and none of its tiles should be considered for rendering. See [Geometric error](#geometric-error).

> **Note:** Tileset geometric error is different than tile geometric error. The former (which only appears once in the tileset) is used to determine whether the root tile is considered for rendering whereas the latter is used to determine whether a tile's children are considered for rendering.

Application-specific properties may be assigned to a tileset with [`EXT_structural_metadata`](https://github.com/CesiumGS/glTF/tree/3d-tiles-2.0/extensions/2.0/Vendor/EXT_structural_metadata). See [Metadata](#metadata).

### Tile

Tiles are represented as nodes in the glTF node hierarchy. A tile consist of metadata used to determine if a tile is rendered, a reference to the renderable content, and an array of any children tiles.

> ![](./figures/tile.png)
>
> Elements of a tile JSON object

The following example shows the root tile from before:

```json
{
  "extensions": {
    "3DTILES_tileset": {
      "geometricError": 70.0,
      "refine": "ADD"
    }
  },
  "boundingVolume": {
    "shape": 0
  },
  "externalAsset": 0,
  "children": [1],
}
```

The following glTF node properties contribute to the tile definition:

- `boundingVolume` defines a volume enclosing the tile, and is used to determine which tiles to render at runtime. See [Bounding Volumes](#bounding-volumes) for the full list of supported shape types. The `boundingVolume` property **MUST** be provided when the node uses the `3DTILES_tileset` extension.
- `externalAsset` provides a reference to the tile's content. When `externalAsset` is not defined the tile is considered an *empty tile*. Empty tiles may be used for culling or other purposes.
- `children` is an array of node indices to child tiles. Each child tile's content is fully enclosed by its parent tile's `boundingVolume`. For *leaf tiles*, there are no children, and `children` **MUST** be omitted.
- `matrix` or `translation`, `rotation`, `scale` (not shown) define an optional local space transform for the tile, see [Transforms](#transforms).

The `3DTILES_tileset` node extension has the following properties that further contribute to the tile definition:

- `geometricError` is a nonnegative number that defines the error, in meters, introduced if this tile is rendered and its children are not. At runtime, the geometric error is used to compute *Screen-Space Error* (SSE), the error measured in pixels. The SSE determines if a tile is sufficiently detailed for the current view or if its children should be considered. See [Geometric error](#geometric-error).
- `refine` is a string that is either `"REPLACE"` for replacement refinement or `"ADD"` for additive refinement. It is required for the root tile of a tileset; it is optional for all other tiles. A tileset can use any combination of additive and replacement refinement. When the `refine` property is omitted, it is inherited from the parent tile. See [Refinement](#refinement).
- `content` is an object that provides additional information about the tile's content, see [Content](#content). If the tile does not have content (i.e. the node's `externalAsset` property is not defined) then `content` **MUST** be omitted.

The `mesh` property **MUST** be omitted.

Application-specific properties may be assigned to a tile with [`EXT_structural_metadata`](https://github.com/CesiumGS/glTF/tree/3d-tiles-2.0/extensions/2.0/Vendor/EXT_structural_metadata). See [Metadata](#metadata).

### Content

A tile can be associated with renderable content, typically an external glTF asset that includes geometry and texture information, and may be extended to include metadata, model instancing, and compression.

The following example shows the root tile from before with a `content` object.

```json
{
  "extensions": {
    "3DTILES_tileset": {
      "geometricError": 70.0,
      "refine": "ADD",
      "content": {
        "boundingVolume": {
          "shape": 2
        }
      }
    }
  },
  "boundingVolume": {
    "shape": 0
  },
  "externalAsset": 0,
  "children": [1],
}
```

The `content` object includes the following optional properties:

- `boundingVolume` is a tightly fitting bounding volume enclosing just the tile's content. See [Bounding Volumes](#bounding-volumes).

Application-specific properties may be assigned to content with [`EXT_structural_metadata`](https://github.com/CesiumGS/glTF/tree/3d-tiles-2.0/extensions/2.0/Vendor/EXT_structural_metadata). See [Metadata](#metadata).

### External Tilesets

To create a tree of trees, a tile can point to an external tileset (a glTF using the `3DTILES_tileset` extension). This enables, for example, storing each city in a tileset and then having a global tileset of tilesets.

> ![](./figures/tilesets.png)
>
> A tileset that refers to other tilesets

```json
{
  "extensions": {
    "3DTILES_tileset": {
      "geometricError": 70.0,
      "refine": "ADD"
    }
  },
  "boundingVolume": {
    "shape": 0
  },
  "externalAsset": 0
}
```

When a tile points to an external tileset, the tile:

- Cannot have any children; `node.children` **MUST** be omitted.
- Is [unconditionally refinable](#unconditional-refinement) regardless of its geometric error.

### Geometric Error

Tiles are structured into a tree incorporating *Hierarchical Level of Detail* (HLOD) so that at runtime a client implementation will need to determine if a tile is sufficiently detailed for rendering and if the content of tiles should be successively refined by children tiles of higher resolution. An implementation will consider a maximum allowed *Screen-Space Error* (SSE), the error measured in pixels.

A tile's geometric error defines the selection metric for that tile. Its value is a nonnegative number that defines the error, in meters, introduced if this tile is rendered and its children are not.

> ![](./figures/3d-tiles-overview-page0005-GE-drawing.png)
>
> Geometric Error and Screen Space Error (SSE)

Generally, the root tile will have the largest geometric error, and each successive level of children will have a smaller geometric error than its parent, with leaf tiles having a geometric error of or close to 0. If a child tile's geometric error is greater than or equal to its parent's geometric error, that child tile is considered [unconditionally refinable](#unconditional-refinement).

In a client implementation, geometric error is used with other screen space metrics—e.g., distance from the tile to the camera, screen size, and resolution—to calculate the SSE introduced if this tile is rendered and its children are not. If the introduced SSE exceeds the maximum allowed, then the tile is refined and its children are considered for rendering.

The geometric error is formulated based on a metric like point density, mesh or texture decimation, or another factor specific to that tileset. In general, a higher geometric error means a tile will refine more aggressively, and children tiles will be loaded and rendered sooner.

### Refinement

Refinement determines the process by which a lower resolution parent tile renders when its higher resolution children are selected to be rendered. Permitted refinement types are replacement (`"REPLACE"`) and additive (`"ADD"`). If the tile has replacement refinement, the children tiles are rendered in place of the parent, that is, the parent tile is no longer rendered. If the tile has additive refinement, the children are rendered in addition to the parent tile.

A tileset can use replacement refinement exclusively, additive refinement exclusively, or any combination of additive and replacement refinement.

A refinement type is required for the root tile of a tileset; it is optional for all other tiles. When omitted, a tile inherits the refinement type of its parent.

#### Replacement

If a tile uses replacement refinement, when refined it renders its children in place of itself.

> |Parent Tile|Refined|
> |---|---|
> |![](./figures/replacement_1.jpg)|![](./figures/replacement_2.jpg)|
>
> A tile and a refined tile using replacement refinement

#### Additive

If a tile uses additive refinement, when refined it renders itself and its children simultaneously.

> |Parent Tile|Refined|
> |---|---|
> |![](./figures/additive_1.jpg)|![](./figures/additive_2.jpg)|
>
> A tile and a refined tile using additive refinement

#### Unconditional Refinement

A tile that is "unconditionally refined" will always be REFINED, it will never be RENDERED. Such tiles are identified by having a geometric error greater than or equal to its parent's geometric error.

One example of an unconditionally refined tile is a tile referencing an [external tileset](#external-tilesets). This tile has no renderable content itself, but may have child tiles with renderable content.

### Bounding Volumes

A bounding volume defines the spatial extent enclosing a tile or a tile's content. The bounding volume shape type **MUST** be `"box"` or `"sphere"` unless additional shape types are enabled through extensions.

A list of extensions that enable additional shape types:

- [3DTILES_shape_ellipsoid_region](https://github.com/CesiumGS/glTF/tree/3d-tiles-2.0/extensions/2.1/Vendor/3DTILES_shape_ellipsoid_region/README.md)
- [3DTILES_shape_cylinder_region](https://github.com/CesiumGS/glTF/tree/3d-tiles-2.0/extensions/2.1/Vendor/3DTILES_shape_cylinder_region/README.md)
- [3DTILES_shape_s2](https://github.com/CesiumGS/glTF/tree/3d-tiles-2.0/extensions/2.1/Vendor/3DTILES_shape_s2/README.md)

The content may have an optional `boundingVolume`, the content bounding volume. Unlike the tile bounding volume, the content bounding volume is a tightly fitting bounding volume enclosing just the tile's content. This enables tight view frustum culling, excluding from rendering any content not in the volume of what is potentially in view. When it is not defined, the tile's bounding volume is still used for culling.

The screenshot below shows the bounding volumes for the root tile for Canary Wharf. The `node.boundingVolume`, shown in red, encloses the entire area of the tileset; `content.boundingVolume` shown in blue, encloses just the four features (models) in the root tile.

> ![](./figures/contentsBox.png)
>
> Bounding volumes for the root tile of a tileset. Building data from [CyberCity3D](http://www.cybercity3d.com/). Imagery data from [Bing Maps](https://www.microsoft.com/maps/).

#### Bounding Box

The following example shows an oriented bounding box that is created by transforming an axis-aligned box shape.

```json
{
  "shapes": [
    {
      "type": "box",
      "box": {
        "size": [1.0, 1.0, 1.0]
      }
    }
  ],
  "nodes": [
    {
      "boundingVolume": {
        "shape": 0,
        "translation": [-3923021.73, -931070.70, 4925458.17],
        "rotation": [0.26262, -0.20758, -0.58433, 0.73925],
        "scale": [100.0, 100.0, 100.0]
      },
      ...
    }
  ]
}
```

#### Bounding Sphere

The following example shows a bounding sphere.

```json
{
  "shapes": [
    {
      "type": "sphere",
      "sphere": {
        "radius": 6378137.0
      }
    }
  ],
  "nodes": [
    {
      "boundingVolume": {
        "shape": 0
      },
      ...
    }
  ]
}
```

### Transforms

A tile may define a local space transform using the `matrix` or `translation`, `rotation`, `scale` glTF node properties.

The transformation from each tile's local coordinate system to the tileset's global coordinate system is computed by a top-down traversal of the tileset and by post-multiplying a child's transform with its parent's transform, as covered in [glTF Transformations](https://registry.khronos.org/glTF/specs/2.0/glTF-2.0.html#transformations).

The transform applies to the tile's bounding volume, content (if present), and content bounding volume (if present).

The transform does not apply to `geometricError`—i.e., the scale defined by `transform` does not scale the geometric error—the geometric error is always defined in meters.

Certain bounding volume types, such as `3DTILES_shape_ellipsoid_region` and `3DTILES_shape_s2`, are defined in a geospatial coordinate system and cannot be reasonably transformed. In such cases, the transform does not apply to these bounding volumes. Additionally, the transform does not apply to `3DTILES_horizon_occlusion_point`.

[EXT_georeference](https://github.com/CesiumGS/glTF/tree/3d-tiles-2.0/extensions/2.1/Vendor/EXT_georeference/README.md) may be used to transform a tile from its local coordinate system to a geocentric coordinate system by placing it at a specific longitude, latitude, height. The georeference transform is applied after the node transform (pre-multiplied).

### Spatial Coherence

As described above, the tree has spatial coherence; each tile has a bounding volume completely enclosing its content, and the content for child tiles are completely inside the parent's bounding volume. This does not imply that a child's bounding volume is completely inside its parent's bounding volume. For example:

> ![](./figures/parentBoundingSphere.jpg)
>
> Bounding sphere for a terrain tile.

> ![](./figures/childBoundingSphere.jpg)
>
> Bounding spheres for the four child tiles. The children's content is completely inside the parent's bounding volume, but the children's bounding volumes are not since they are not tightly fit.

### Spatial Data Structures

3D Tiles incorporates the concept of Hierarchical Level of Detail (HLOD) for optimal rendering of spatial data. A tileset is composed of a tree, defined by a root tile and, recursively, its children tiles, which can be organized by different types of spatial data structures.

A runtime engine is generic and will render any tree defined by a tileset. Any combination of tile formats and refinement approaches can be used, enabling flexibility in supporting heterogeneous datasets, see [Refinement](#refinement).

A tileset may use a 2D spatial tiling scheme similar to raster and vector tiling schemes (like a Web Map Tile Service (WMTS) or XYZ scheme) that serve predefined tiles at several levels of detail (or zoom levels). However since the content of a tileset is often non-uniform or may not easily be organized in only two dimensions, other spatial data structures may be more optimal.

[Appendix A: Spatial data structures](#appendix-a-spatial-data-structures) gives a brief description of how 3D Tiles can represent various spatial data structures.

### Coordinate Reference System (CRS)

A tileset may be defined in either a **global** or **local** coordinate system. A tileset's global coordinate system will often be in a [WGS 84](https://epsg.org/ellipsoid_7030/WGS-84.html) Earth-centered, Earth-fixed (ECEF) reference frame ([EPSG 4978](https://epsg.org/crs_4978/WGS-84.html)), but it doesn't have to be, e.g., a power plant may be defined fully in its local coordinate system.

A tileset defined in a local coordinate system follows the standard glTF coordinate system conventions: right-handed, +Y up, and linear units in meters. It **MUST NOT** use the [EXT_geospatial_crs](https://github.com/CesiumGS/glTF/tree/3d-tiles-2.0/extensions/2.1/Vendor/EXT_geospatial_crs/README.md) extension.

A tileset defined in a global coordinate system **MUST** specify its coordinate reference system (CRS) with [EXT_geospatial_crs](https://github.com/CesiumGS/glTF/tree/3d-tiles-2.0/extensions/2.1/Vendor/EXT_geospatial_crs/README.md).

A tileset defined in a global coordinate system differs from the standard glTF coordinate system conventions – there is no single "up" vector on the surface of a globe. Instead, the tileset uses a **geocentric (planetocentric)** coordinate reference system such as [EPSG 4978](https://epsg.org/crs_4978/WGS-84.html).

> ![](./figures/ecef.png)
>
> An illustration of [EPSG 4978](https://epsg.org/crs_4978/WGS-84.html), an Earth-centered, Earth-fixed (ECEF) geocentric coordinate reference system. It defines 0,0,0 as the center of mass on Earth, where +Z extends through true north (i.e. the geodetic North Pole) and +X intersects the sphere of the earth at 0° latitude (the equator) and 0° longitude (the prime meridian which passes through Greenwich). As a result, no discrete "Up" vector exists relative to a ground plane.

The example below shows a tileset using [EPSG 4978](https://epsg.org/crs_4978/WGS-84.html).

```json
{
  "asset": {
    "version": "2.1"
  },
  "extensions": {
    "EXT_geospatial_crs": {
      "format": "wkid",
      "extensions": {
        "EXT_geospatial_crs_wkid": {
          "authority": "EPSG",
          "wkid": 4978
        }
      }
    }
  }
}
```

> [!NOTE]
> 3D Tiles only allows local and geocentric (planetocentric) coordinate reference systems. Other coordinate reference system types, such as geographic and projected coordinate reference systems, are not allowed as they often require dedicated coordinate transformation libraries and ancillary data, such as grid shift files, in order to be rendered in 3D globe engines.

Tilesets may reference [external tilesets](#external-tilesets) in different coordinate systems. For example, a tileset could start in a geocentric coordinate reference system and then transition to a local engineering reference frame for higher precision.

The following rules apply for CRS transitions:

- Tilesets in a local coordinate system **MUST** only reference other tilesets in a local coordinate system
- Tilesets in a geocentric coordinate system **MUST** only reference tilesets in a local coordinate system or tilesets in the same geocentric coordinate system.

A [tile transform](#transforms) may be applied to transform a tile's local coordinate system to the parent tile's geocentric coordinate system.

The example below shows a tileset using [EXT_georeference](https://github.com/CesiumGS/glTF/tree/3d-tiles-2.0/extensions/2.1/Vendor/EXT_georeference/README.md). Note that when `EXT_georeference` is used the tileset is in a global coordinate system and `EXT_geospatial_crs` must also be used.

```json
{
  "asset": {
    "version": "2.1"
  },
  "extensions": {
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
      "extensions": {
        "3DTILES_tileset": {
          "geometricError": 240.0,
          "refine": "ADD"
        },
        "EXT_georeference": {
          "longitude": -75.15836368768382,
          "latitude": 39.95090650840344,
          "height": -21.668226434267066
        }
      },
      "boundingVolume": {
        "shape": 0
      },
      "externalAsset": 0
    }
  ]
}
```

Certain bounding volume types, such as `3DTILES_shape_ellipsoid_region` and `3DTILES_shape_s2`, are defined in a geospatial coordinate system and **CANNOT** be used by tiles defined in a local coordinate system.

Additionally, `3DTILES_horizon_occlusion_point` **CANNOT** be used by tiles defined in a local coordinate system.

### Implicit Tiling

The bounding volume hierarchy may be defined explicitly — as shown previously — which enables a wide variety of spatial data structures. Certain common data structures such as quadtrees and octrees may be defined implicitly without providing bounding volumes for every tile. This regular pattern allows for random access of tiles based on their tile coordinates which enables accelerated spatial queries, new traversal algorithms, and efficient updates of tile content, among other use cases.

Implicit tiling is enabled by using the [3DTILES_implicit_tiling](https://github.com/CesiumGS/glTF/tree/3d-tiles-2.0/extensions/2.1/Vendor/3DTILES_implicit_tiling/README.md) extension. In order to support sparse datasets, the tree is partitioned into fixed-size *subtrees*. See [3DTILES_subtree](https://github.com/CesiumGS/glTF/tree/3d-tiles-2.0/extensions/2.1/Vendor/3DTILES_subtree/README.md).

> ![](./figures/implicit-tiling-small.png)
>
> Quadtree with tile coordinates

## Supporting Extensions

### Metadata

Application-specific *properties* may be provided at multiple granularities within a tileset. Properties may be associated with tilesets, tiles, and contents within a tileset file using the `EXT_structural_metadata` extension. Properties may also be associated with features or with individual vertices and texels within content files.

The following example shows a tileset with tileset properties, tile properties, and content properties. The referenced asset `root.glb` may also use `EXT_structural_metadata`, for example, for storing per-feature properties.

```json
{
  "extensionsUsed": ["3DTILES_tileset", "EXT_structural_metadata"],
  "extensionsRequired": ["3DTILES_tileset"],
  "extensions": {
    "3DTILES_tileset": {
      "geometricError": 240,
      "extensions": {
        "EXT_structural_metadata": {
          "class": "city",
          "properties": {
            "name": "New York City",
            "country": "United States",
            "population": 8804190
          }
        }
      }
    },
    "EXT_structural_metadata": {
      "schema": {
        "id": "schema",
        "classes": {
          "city": {
            "properties": {
              "name": {
                "type": "STRING"
              },
              "country": {
                "type": "STRING"
              },
              "population": {
                "type": "SCALAR",
                "componentType": "UINT64"
              }
            }
          },
          "block": {
            "properties": {
              "borough": {
                "type": "STRING"
              },
              "zipCode": {
                "type": "SCALAR",
                "componentType": "UINT32"
              },
              "population": {
                "type": "SCALAR",
                "componentType": "UINT64"
              }
            }
          },
          "geometryData": {
            "properties": {
              "vertices": {
                "type": "SCALAR",
                "componentType": "UINT32"
              },
              "primitives": {
                "type": "SCALAR",
                "componentType": "UINT32"
              }
            }
          }
        }
      }
    }
  },
  "asset": {
    "version": "2.1"
  },
  "nodes": [
    {
      "extensions": {
        "3DTILES_tileset": {
          "geometricError": 0.0,
          "refine": "ADD",
          "content": {
            "extensions": {
              "EXT_structural_metadata": {
                "class": "geometryData",
                "properties": {
                  "vertices": 49534,
                  "primitives": 2
                }
              }
            }
          }
        },
        "EXT_structural_metadata": {
          "class": "block",
          "properties": {
            "borough": "Manhattan",
            "zipCode": 10024,
            "population": 52428
          }
        }
      },
      "boundingVolume": {
        "shape": 0
      },
      "externalAsset": 0
    }
  ]
}
```

#### Statistics

A tileset may also provide summary statistics with [`EXT_structural_metadata`](https://github.com/CesiumGS/glTF/tree/3d-tiles-2.0/extensions/2.0/Vendor/EXT_structural_metadata#statistics) that allow applications to analyze or display metadata—e.g. with [declarative styling](#declarative-styling)—without first having to process the complete dataset to identify bounds for color ramps and histograms. Statistics are provided on a per-class basis, so that applications can provide styling or context based on the tileset as a whole, while only needing to download and process a subset of its tiles.

> ![](./figures/statistics.png)
>
> Metadata statistics using _EXT_structural_metadata_

### Layers

Layers are a common concept when working with geospatial data, representing semantic or functional groups of geometries requiring common handling by the application. The [3DTILES_layers](https://github.com/CesiumGS/glTF/tree/3d-tiles-2.0/extensions/2.1/Vendor/3DTILES_layers/README.md) extension adds support for layers. This can be useful for a variety of applications including vector data. See the [3DTILES_layers](https://github.com/CesiumGS/glTF/tree/3d-tiles-2.0/extensions/2.1/Vendor/3DTILES_layers/README.md) extension specification for further details.

### Horizon Occlusion Point

To allow for fast horizon culling at runtime, the [3DTILES_horizon_occlusion_point](https://github.com/CesiumGS/glTF/tree/3d-tiles-2.0/extensions/2.1/Vendor/3DTILES_horizon_occlusion_point/README.md) extension can be used to set a precomputed horizon occlusion point to a tile or contents. See the [3DTILES_horizon_occlusion_point](https://github.com/CesiumGS/glTF/tree/3d-tiles-2.0/extensions/2.1/Vendor/3DTILES_horizon_occlusion_point/README.md) extension specification for further details.

### Visibility Bounding Volumes

Visibility bounding volumes allow specifying a space where specialized geometry is only visible when a camera is present. The [EXT_node_visibility_volume](https://github.com/CesiumGS/glTF/tree/3d-tiles-2.0/extensions/2.1/Vendor/EXT_node_visibility_volume) extension allows specifying a bounding volume where an associated node containing a [KHR_node_visibility](https://github.com/CesiumGS/glTF/tree/3d-tiles-2.0/extensions/2.0/Khronos/KHR_node_visibility/README.md) definition is set to become visible. See the [EXT_node_visibility_volume](https://github.com/CesiumGS/glTF/tree/3d-tiles-2.0/extensions/2.1/Vendor/EXT_node_visibility_volume) extension specification for further details.

### Declarative Styling

3D Tiles includes concise declarative styling defined with JSON and expressions written in a small subset of JavaScript augmented for styling.

For complete details, see the [Declarative Styling](https://github.com/CesiumGS/3d-tiles/tree/main/specification/Styling/) specification.

## Appendix A: Spatial Data Structures

### Quadtrees

A quadtree is created when each tile has four uniformly subdivided children, similar to typical 2D geospatial tiling schemes. Empty child tiles can be omitted.

> ![](./figures/quadtree.png)
>
> Classic quadtree subdivision.

3D Tiles enable quadtree variations such as non-uniform subdivision and tight bounding volumes (as opposed to bounding, for example, the full 25% of the parent tile, which is wasteful for sparse datasets).

> ![](./figures/quadtree-tight.png)
>
> Quadtree with tight bounding volumes around each child.

For example, here is the root tile and its children for Canary Wharf. Note the bottom left, where the bounding volume does not include the water on the left where no buildings will appear:

> ![](./figures/nonUniformQuadtree.png)
>
> Building data from [CyberCity3D](http://www.cybercity3d.com/). Imagery data from [Bing Maps](https://www.microsoft.com/maps/).

3D Tiles also enable other quadtree variations such as [loose quadtrees](http://www.tulrich.com/geekstuff/partitioning.html), where child tiles overlap but spatial coherence is still preserved, i.e., a parent tile completely encloses all of its children. This approach can be useful to avoid splitting features, such as 3D models, across tiles.

> ![](./figures/quadtree-overlap.png)
>
> Quadtree with non-uniform and overlapping tiles.

Below, the green buildings are in the left child and the purple buildings are in the right child. Note that the tiles overlap so the two green and one purple building in the center are not split.

> ![](./figures/looseQuadtree.png)
>
> Building data from [CyberCity3D](http://www.cybercity3d.com/). Imagery data from [Bing Maps](https://www.microsoft.com/maps/).


#### K-d trees

A k-d tree is created when each tile has two children separated by a *splitting plane* parallel to the *x*, *y*, or *z* axis (or latitude, longitude, height). The split axis is often round-robin rotated as levels increase down the tree, and the splitting plane may be selected using the median split, surface area heuristics, or other approaches.

> ![](./figures/kdtree.png)
>
> Example k-d tree. Note the non-uniform subdivision.

Note that a k-d tree does not have uniform subdivision like typical 2D geospatial tiling schemes and, therefore, can create a more balanced tree for sparse and non-uniformly distributed datasets.

3D Tiles enables variations on k-d trees such as [multi-way k-d trees](http://www.crs4.it/vic/cgi-bin/bib-page.cgi?id=%27Goswami:2013:EMF%27) where, at each leaf of the tree, there are multiple splits along an axis. Instead of having two children per tile, there are `n` children.

#### Octrees

An octree extends a quadtree by using three orthogonal splitting planes to subdivide a tile into eight children. Like quadtrees, 3D Tiles allows variations to octrees such as non-uniform subdivision, tight bounding volumes, and overlapping children.

> ![](./figures/octree.png)
>
> Traditional octree subdivision.

> ![](./figures/pointcloud-octree.png)
>
> Non-uniform octree subdivision for a point cloud using additive refinement. Point Cloud of [the Church of St Marie at Chappes, France](http://robotics.cs.columbia.edu/~atroccol/ijcv/chappes.html) by Prof. Peter Allen, Columbia University Robotics Lab. Scanning by Alejandro Troccoli and Matei Ciocarlie.

#### Grids

3D Tiles enables uniform, non-uniform, and overlapping grids by supporting an arbitrary number of child tiles. For example, here is a top-down view of a non-uniform overlapping grid of Cambridge:


> ![](./figures/grid.png)
>
> Building data from [CyberCity3D](http://www.cybercity3d.com/). Imagery data from [Bing Maps](https://www.microsoft.com/maps/).


3D Tiles takes advantage of empty tiles: those tiles that have a bounding volume, but no content. Since a tile's `externalAsset` property does not need to be defined, empty non-leaf tiles can be used to accelerate non-uniform grids with hierarchical culling. This essentially creates a quadtree or octree without hierarchical levels of detail (HLOD).

## Schema

- [content.schema.json](schema/content.schema.json)
- [glTF.3DTILES_tileset.schema.json](schema/glTF.3DTILES_tileset.schema.json)
- [node.3DTILES_tileset.schema.json](schema/node.3DTILES_tileset.schema.json)
