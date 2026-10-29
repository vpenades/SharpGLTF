<!--
SPDX-FileCopyrightText: 2026 Bentley Systems, Incorporated

SPDX-License-Identifier: CC-BY-4.0
-->

# EXT\_georeference

## Contributors

- Sean Lilley, Cesium
- Andreas Plesch, Earthstruct

## Status

Draft

## Dependencies

Written against the glTF 2.1 spec.

## Optional

This extension is optional, meaning it should be placed in the glTF root's `extensionsUsed` list, but not in the `extensionsRequired` list.

## Overview

This extension georeferences a node to the provided geographic coordinates.

```json
{
  "extensions": {
    "EXT_georeference": {
      "longitude": -75.15836368768382,
      "latitude": 39.95090650840344,
      "height": -21.668226434267066
    }
  },
  "mesh": 0
}
```

`longitude` and `latitude` are specified in degrees. `height` above (or below) the ellipsoid is specified in meters.

The extension is most useful when implementations use it to apply an additional transform on the node (see [Transformation Order](./README.md#transformation-order)). In this case local coordinates will be transformed to geocentric (planetocentric) coordinates.

This extension uses WGS84 ([EPSG:4979](https://epsg.org/crs_4979/WGS-84.html)) as the default coordinate reference system. A different coordinate reference system may be specified with [`EXT_geospatial_crs`](../EXT_geospatial_crs/README.md). In this case the longitude, latitude, and height values are geographic coordinates on the provided ellipsoid instead of the WGS84 ellipsoid.

The extension georeferences a node by attaching the local coordinate origin to the provided geospatial location by a translation. The extension also adjusts the orientation of the node. It will set the orientation by a rotation around the local origin to align the local coordinate system axes with the tangent plane on the selected ellipsoid at the specified location (see figure). The tangent plane uses the [geodetic normal](https://github.com/CesiumGS/community/blob/main/GeospatialGuide/README.md#whats-the-difference-between-geocentric-and-geodetic-latitude), not the geocentric normal.

This extension applies a rotation which has the following results:

- The `-x` axis (local right) faces east
- The `+y` axis (local up) faces up (normal to the tangent plane)
- The `+z` axis (local forward) faces north

> ![](./figures/enu-xyz.png)
> Alignment of local coordinates (right) to tangent plane of ellipsoid (left).

## Transformation Order

The georeference transform is applied **after** the node transform.

In this example the node has a local 20° heading that is applied before the georeference transform. The heading is converted into a rotation quaternion about the local y (up) axis in the example.

### Node without 20° heading

> ![](./figures/plane.jpg)
>
> The node of the aircraft heading 0°

```json
{
  "extensions": {
    "EXT_georeference": {
      "longitude": -75.15836368768382,
      "latitude": 39.95090650840344,
      "height": -21.668226434267066
    }
  },
  "mesh": 0
}
```


### Node with 20° heading

> ![](./figures/plane-heading.jpg)
>
> The node of the aircraft rotated clockwise to heading 20°


```json
{
  "extensions": {
    "EXT_georeference": {
      "longitude": -75.15836368768382,
      "latitude": 39.95090650840344,
      "height": -21.668226434267066
    }
  },
  "rotation": [0, -0.173648, 0, 0.984807],
  "mesh": 0,
}
```

## Schema

- [node.EXT_georeference.schema.json](schema/node.EXT_georeference.schema.json)
