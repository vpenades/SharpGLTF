<!--
SPDX-FileCopyrightText: 2026 Bentley Systems, Incorporated

SPDX-License-Identifier: CC-BY-4.0
-->

# EXT_node_visibility_conditions

## Contributors

- Marco Hutter, Cesium
- Sean Lilley, Cesium
- Björn Blissing, Vantor

## Status

Draft

## Dependencies

Written against the glTF 2.1 specification.

Depends on [KHR_node_visibility](../../../2.0/Khronos/KHR_node_visibility).

## Optional

This extension is optional.

Implementations that do not support this extension (but only the `KHR_node_visibility` extension) will simply use the value of the `visible` flag that is defined in the `KHR_node_visibility` extension.

## Overview

This extension adds support for conditions for the `KHR_node_visibility` extension. These conditions allow the client to derive a value for the `visible` property at runtime.

## Extending the Visibility Extension

The `KHR_node_visibility` extension is associated with a node, and defines the `visible` flag that indicates whether a node and all its descendants. The `EXT_node_visibility_conditions` extension extends this extension, and consists of two parts:

- The main extension object is contained in the `KHR_node_visibility` extension object, and defines the conditions for the `visible` flag to be `true`
- A top-level extension object defines the condition variables that may appear in the extension object, and their possible values.

The following example is a glTF asset that uses the extension to define the condition for a node with an external asset to be visible:

```jsonc
{
  "asset": {
    "version": 2.1,
  },
  "extensions": {
    "EXT_node_visibility_conditions": {
      "dimensions": [
        {
          "name": "exampleTimeStamp",
          "domain": ["2025-09-25", "2025-09-26"],
        },
        {
          "name": "exampleRevision",
          "domain": ["revision0", "revision1"],
        },
      ],
    },
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
  "nodes": [
    {
      "externalAsset": 0,
      "extensions": {
        "KHR_node_visibility": {
          "visible": false,
          "extensions": {
            "EXT_node_visibility_conditions": {
              "conditions": {
                "exampleTimeStamp": "2025-09-25",
                "exampleRevision": "revision0",
              },
            },
          },
        },
      },
    },
  ],
  "scene": 0,
  "scenes": [{ "nodes": [0] }],
}
```

The top-level extension object defines the `dimensions` for the conditions. These are given as a dictionary that maps the name of the condition to the `domain`, which is an array containing all possible values for the condition variables.

The `EXT_node_visibility_conditions` object that is associated with the `KHR_node_visibility` object of the only node defines the set of conditions based on which the `visible` flag of the `KHR_node_visibility` extension object should be `true` or `false`.

## Runtime Behavior

The process for determining the value of the `visible` flag based on the `EXT_node_visibility_conditions` object is left to the application. A common implementation could be that the client allows the user to select one value of each `domain` of the top-level extension object, and checks whether the corresponding values from the `EXT_node_visibility_conditions` object are equal to the selected values. This extension only defines the _structure_ for the conditions, but not their _interpretation_, so the exact interpretation of the conditions for setting the `visible` flag are left to the implementation or may be defined by future extensions.

## Schema

- [EXT_node_visibility_conditions.schema.json](schema/EXT_node_visibility_conditions.schema.json)
- [glTF.EXT_node_visibility_conditions.schema.json](schema/glTF.EXT_node_visibility_conditions.schema.json)
