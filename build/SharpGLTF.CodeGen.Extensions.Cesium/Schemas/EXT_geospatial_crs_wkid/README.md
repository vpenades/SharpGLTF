<!--
SPDX-FileCopyrightText: 2026 Bentley Systems, Incorporated

SPDX-License-Identifier: CC-BY-4.0
-->

# EXT\_geospatial\_crs\_wkid

## Contributors

- Sean Lilley, Cesium
- Björn Blissing, Vantor
- Tamrat Belayneh, ESRI

## Status

Draft

## Dependencies

Written against the glTF 2.1 spec.

Depends on [EXT_geospatial_crs](../EXT_geospatial_crs/README.md).

## Optional

This extension is optional, meaning it should be placed in the glTF root's `extensionsUsed` list, but not in the `extensionsRequired` list.

## Overview

This extension allows a coordinate reference system (CRS) to be provided as a Well-Known ID, commonly an [EPSG](https://epsg.org/home.html) code.

`EXT_geospatial_crs_wkid` extends the [`EXT_geospatial_crs`](../EXT_geospatial_crs/README.md) object. The "format" property **MUST** be `"wkid"`. The extension defines the following properties:

- `authority` is the organization that defines and manages a specific CRS identifier.
- `wkid` is an integer that identifies the CRS.
- `vcsWkid` (optional) is an integer that identifies the vertical CRS if `wkid` identifies a horizontal CRS.
- `epoch` (optional) is an epoch for coordinates that are referenced to a dynamic CRS such as WGS 84. Expressed as a decimal year (e.g. `"2019.81"`). See [WKT representation of coordinate epoch and coordinate metadata](http://docs.opengeospatial.org/is/18-010r7/18-010r7.html#128) for more details.

The following example shows an asset annotated to indicate a [WGS 84](https://epsg.org/ellipsoid_7030/WGS-84.html) Earth-centered, Earth-fixed (ECEF) geocentric coordinate reference system ([EPSG 4978](https://epsg.org/crs_4978/WGS-84.html)).

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
          "wkid": 4978,
          "epoch": "2019.81"
        }
      }
    }
  }
}
```

An asset defined in Moon planetocentric coordinates:

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
          "authority": "IAU_2015",
          "wkid": 30100
        }
      }
    }
  }
}
```

An asset defined with a horizontal CRS (UTM Zone 11N) and vertical CRS (NAVD 88):

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
          "wkid": 26911,
          "vcsWkid": 5703
        }
      }
    }
  }
```

## Schema

- [EXT_geospatial_crs_wkid.schema.json](schema/EXT_geospatial_crs_wkid.schema.json)
