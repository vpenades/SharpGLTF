<!--
SPDX-FileCopyrightText: 2026 Bentley Systems, Incorporated

SPDX-License-Identifier: CC-BY-4.0
-->

# EXT_node_visibility_volume

## Contributors

- Adam Morris, Cesium
- Jeshurun Hembd, Cesium
- Marco Hutter, Cesium
- Sean Lilley, Cesium

## Status

Draft

## Dependencies

Written against the glTF 2.1 specification.

Depends on [KHR_node_visibility](../../../2.0/Khronos/KHR_node_visibility).

## Optional

This extension is optional.

Implementations that do not support this extension (but only the `KHR_node_visibility` extension) will simply use the value of the `visible` flag that is defined in the `KHR_node_visibility` extension.

## Overview

This extension adds support for a visibility bounding volume for `EXT_node_visibility`. The visibility bounding volume allows a node's `visible` flag to be set to `true` when a viewer's camera origin point is within the bounding volume.

## Extending the visibility extension

The `KHR_node_visibility` extension is associated with a node, and defines the `visible` flag that indicates whether a node and all its descendants are visible. `EXT_node_visibility_volume` extends this extension by allowing a bounding volume to be associated to the visibility object. This bounding volume fits the same rules and criteria for node bounding volumes as defined in [§3.5.4. of the glTF 2.1 specification](/specification/2.1/Specification.adoc).

The following example is a glTF asset that uses the extension to define a visibility bounding volume:

```json
{
  "asset": {
    "version": 2.1,
  },
  "files": [
    {
      "mimeType": "model/gltf-binary",
      "uri": "example.glb",
    },
  ],
  "externalAssets": [
    {
      "file": 0,
    },
  ],
  "shapes": [
    {
      "type": "box",
      "box": {
        "size": [7.476, 7.44, 26.804]
      }
    }
  ],
  "nodes": [
    {
      "externalAsset": 0,
      "extensions": {
        "KHR_node_visibility": {
          "visible": false,
          "extensions": {
            "EXT_node_visibility_volume": {
              "boundingVolume": {
                "shape": 0
              }
            }
          },
        },
      },
    },
  ],
  "scene": 0,
  "scenes": [{ "nodes": [0] }],
}
```

The `EXT_node_visibility_volume` object that is associated with `KHR_node_visibility` object of the only node defines the bounding volume who's extents are used to determine if the node and it's hierarchy is visible.

## Runtime Behavior

_This section is non-normative._

This extension's primary goal is to show a node and its hierarchy when the camera is within its bounding volume. Once the hierarchy is made visible, its contents may still be subject to other Level of Detail (LoD) methods. Implementations may add to this behavior, but should not add stricter conditions for visibility. For example, requiring the camera to be within a certain distance from the bounding volume's center is discouraged.

## Schema

- [EXT_node_visibility_volume.schema.json](schema/EXT_node_visibility_volume.schema.json)
