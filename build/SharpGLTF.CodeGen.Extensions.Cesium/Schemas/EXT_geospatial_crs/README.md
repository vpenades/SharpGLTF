<!--
SPDX-FileCopyrightText: 2026 Bentley Systems, Incorporated

SPDX-License-Identifier: CC-BY-4.0
-->

# EXT\_geospatial\_crs

## Contributors

- Sean Lilley, Cesium
- Don McCurdy, Cesium
- Björn Blissing, Vantor
- Tamrat Belayneh, ESRI

## Status

Draft

## Dependencies

Written against the glTF 2.1 spec.

## Optional

This extension is optional, meaning it should be placed in the glTF root's `extensionsUsed` list, but not in the `extensionsRequired` list.

## Overview

Assets with this extension are declared to have been authored for geospecific usage, with a particular coordinate reference system (CRS) that overrides the [coordinate system](https://www.khronos.org/registry/glTF/specs/2.0/glTF-2.0.html#coordinate-system-and-units) of the base glTF specification.

A CRS may be one of the following types:

- **Geographic** - longitude, latitude, height
- **Geocentric** - global Cartesian coordinates - x, y, z
- **Projected** - local Cartesian coordinates in a 2D projected space - x, y, height
- **Compound** - combination of a horizontal + vertical CRS
- **Local** - local Cartesian coordinates - x, y, z


For example, an asset may declare a projected CRS that is +X east, +Y north, +Z up and U.S. Survey Feet. This overrides the default glTF coordinate system of -X right, +Y up, +Z forward, and meters.

There are several standard formats used to define coordinate reference systems. This extension doesn't adhere to a specific format,  instead defines a `"format"` property whose value **MUST** be defined by additional extensions:

- [EXT_geospatial_crs_wkid](../EXT_geospatial_crs_wkid/README.md) - Well-Known ID, commonly used to represent EPSG codes. Defines format `"wkid"`.
- [EXT_geospatial_crs_wkt2](../EXT_geospatial_crs_wkt2/README.md) - Well-Known Text version 2. Defines format `"wkt2"`.

Additional extensions may define additional formats.

The following example shows an asset annotated to indicate a [WGS 84](https://epsg.org/ellipsoid_7030/WGS-84.html) Earth-centered, Earth-fixed (ECEF) geocentric coordinate reference system ([EPSG 4978](https://epsg.org/crs_4978/WGS-84.html)) with `EXT_geospatial_crs` and `EXT_geospatial_crs_wkid`.

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

The following example shows an asset defined in UTM Zone 11N coordinates using `EXT_geospatial_crs` and `EXT_geospatial_crs_wkt2`.

```json
{
  "asset": {
    "version": "2.1"
  },
  "extensions": {
    "EXT_geospatial_crs": {
      "format": "wkt2",
      "extensions": {
        "EXT_geospatial_crs_wkt2": {
          "wkt2": "PROJCRS[\"WGS 84 / UTM zone 11N\",BASEGEOGCRS[\"WGS 84\",ENSEMBLE[\"World Geodetic System 1984 ensemble\",MEMBER[\"World Geodetic System 1984 (Transit)\"],MEMBER[\"World Geodetic System 1984 (G730)\"],MEMBER[\"World Geodetic System 1984 (G873)\"],MEMBER[\"World Geodetic System 1984 (G1150)\"],MEMBER[\"World Geodetic System 1984 (G1674)\"],MEMBER[\"World Geodetic System 1984 (G1762)\"],MEMBER[\"World Geodetic System 1984 (G2139)\"],MEMBER[\"World Geodetic System 1984 (G2296)\"],ELLIPSOID[\"WGS 84\",6378137,298.257223563,LENGTHUNIT[\"metre\",1]],ENSEMBLEACCURACY[2.0]],PRIMEM[\"Greenwich\",0,ANGLEUNIT[\"degree\",0.0174532925199433]],ID[\"EPSG\",4326]],CONVERSION[\"UTM zone 11N\",METHOD[\"Transverse Mercator\",ID[\"EPSG\",9807]],PARAMETER[\"Latitude of natural origin\",0,ANGLEUNIT[\"degree\",0.0174532925199433],ID[\"EPSG\",8801]],PARAMETER[\"Longitude of natural origin\",-117,ANGLEUNIT[\"degree\",0.0174532925199433],ID[\"EPSG\",8802]],PARAMETER[\"Scale factor at natural origin\",0.9996,SCALEUNIT[\"unity\",1],ID[\"EPSG\",8805]],PARAMETER[\"False easting\",500000,LENGTHUNIT[\"metre\",1],ID[\"EPSG\",8806]],PARAMETER[\"False northing\",0,LENGTHUNIT[\"metre\",1],ID[\"EPSG\",8807]]],CS[Cartesian,2],AXIS[\"(E)\",east,ORDER[1],LENGTHUNIT[\"metre\",1]],AXIS[\"(N)\",north,ORDER[2],LENGTHUNIT[\"metre\",1]],USAGE[SCOPE[\"Navigation and medium accuracy spatial referencing.\"],AREA[\"Between 120°W and 114°W, northern hemisphere between equator and 84°N, onshore and offshore. Canada - Alberta; British Columbia (BC); Northwest Territories (NWT); Nunavut. Mexico. United States (USA).\"],BBOX[0,-120,84,-114]],ID[\"EPSG\",32611]]"
        }
      }
    }
  }
```

## Schema

- [EXT_geospatial_crs.schema.json](schema/EXT_geospatial_crs.schema.json)
