<!--
SPDX-FileCopyrightText: 2026 Bentley Systems, Incorporated

SPDX-License-Identifier: CC-BY-4.0
-->

# 3DTILES\_horizon\_occlusion\_point

## Contributors

- Sean Lilley, Cesium

## Status

Draft

## Dependencies

Written against the glTF 2.1 spec.

Depends on [3DTILES_tileset](../3DTILES_tileset/README.md) and [EXT_geospatial_crs](../EXT_geospatial_crs/README.md).

## Optional

This extension is optional, meaning it should be placed in the glTF root's `extensionsUsed` list, but not in the `extensionsRequired` list.

## Overview

This extension adds a pre-computed horizon occlusion point to a [tile](../3DTILES_tileset/README.md#tile) or [content](../3DTILES_tileset/README.md#content) in order to perform fast horizon culling at runtime. This enables feature parity with [quantized-mesh](https://github.com/CesiumGS/quantized-mesh).

`horizonOcclusionPoint` is expressed in an ellipsoid–scaled fixed frame. At runtime, if this point is below the horizon, the entire tile is below the horizon and may be culled.

This extension **MAY** only be used by tilesets in a [global coordinate system](../3DTILES_tileset/README.md#coordinate-reference-system-crs). This extension uses the ellipsoid specified by [EXT_geospatial_crs](../EXT_geospatial_crs/README.md).

[Tile transforms](../3DTILES_tileset/README.md#transforms) do not apply to horizon occlusion points.

```json
{
  "extensions": {
    "3DTILES_tileset": {
      "geometricError": 70.0,
      "refine": "ADD"
    },
    "3DTILES_horizon_occlusion_point": {
      "horizonOcclusionPoint": [
        0.16264342332904402,
        -0.39265595843176665,
        1.02261928332819
      ]
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
  "boundingVolume": {
    "shape": 0
  },
  "externalAsset": 0,
  "children": [1]
},
```

The example above shows the extension being added to a tile, but it may also be added to content for finer grained culling, similar to [content bounding volumes](../3DTILES_tileset/README.md#bounding-volumes).

For more information about horizon culling see [Horizon Culling](https://cesium.com/blog/2013/04/25/horizon-culling/).

## Subtree Attributes

This extension defines the following [subtree tile attribute semantics](../3DTILES_subtree/README.md#tile-attributes):

| Attribute Semantic | Accessor Type | Component Type | Description |
|---|---|---|---|
|`"TILE_HORIZON_OCCLUSION_POINT"`|`"VEC3"`|`5130` (DOUBLE)|The horizon occlusion point of the tile expressed in an ellipsoid-scaled fixed frame. If this point is below the horizon, the entire tile is below the horizon.|

This extension defines the following [subtree content attribute semantics](../3DTILES_subtree/README.md#content-attributes):

| Attribute Semantic | Accessor Type | Component Type | Description |
|---|---|---|---|
|`"CONTENT_HORIZON_OCCLUSION_POINT"`|`"VEC3"`|`5130` (DOUBLE)|The horizon occlusion point of the content expressed in an ellipsoid-scaled fixed frame. If this point is below the horizon, the entire content is below the horizon.|

## Schema

- [3DTILES_horizon_occlusion_point.schema.json](schema/3DTILES_horizon_occlusion_point.schema.json)
