using System;
using System.Collections.Generic;
using System.Text;

namespace SharpGLTF.Validation
{
    static partial class ErrorCodes
    {
        // INFORMATION

        public const string MESH_PRIMITIVE_UNUSED_TEXCOORD = "Material does not use texture coordinates sets with indices ('%a', '%b', '%c').";
        public const string UNUSED_OBJECT = "This object may be unused.";

        // WARNINGS

        public const string MESH_PRIMITIVE_INCOMPATIBLE_MODE = "Number of vertices or indices({0}) is not compatible with used drawing mode('{1}').";
        public const string NODE_SKINNED_MESH_WITHOUT_SKIN = "Node uses skinned mesh, but has no skin defined.";
        public const string UNSUPPORTED_EXTENSION = "Cannot validate an extension as it is not supported by the validator: '{0}'.";

        // ERRORS

        public const string ACCESSOR_SMALL_BYTESTRIDE = "Referenced bufferView's byteStride value {0} is less than accessor element's length {1}.";
        public const string ACCESSOR_TOO_LONG = "Accessor(offset: {0}, length: {1}) does not fit referenced bufferView[% 3] length %4.";
        public const string ACCESSOR_TOTAL_OFFSET_ALIGNMENT = "Accessor's total byteOffset {0} isn't a multiple of componentType length {1}.";
        public const string ACCESSOR_USAGE_OVERRIDE = "Override of previously set accessor usage.Initial: '{0}', new: '{1}'.";
        public const string ANIMATION_CHANNEL_TARGET_NODE_MATRIX = "Animation channel cannot target TRS properties of node with defined matrix.";
        public const string ANIMATION_CHANNEL_TARGET_NODE_WEIGHTS_NO_MORPHS = "Animation channel cannot target WEIGHTS when mesh does not have morph targets.";
        public const string ANIMATION_DUPLICATE_TARGETS = "Animation channel has the same target as channel {0}. 	Error";
        public const string ANIMATION_SAMPLER_INPUT_ACCESSOR_INVALID_FORMAT = "Invalid Animation sampler input accessor format '{0}'. Must be one of ('%a', '%b', '%c').";
        public const string ANIMATION_SAMPLER_INPUT_ACCESSOR_TOO_FEW_ELEMENTS = "Animation sampler output accessor with '{0}' interpolation must have at least {1} elements.Got {2}.";
        public const string ANIMATION_SAMPLER_INPUT_ACCESSOR_WITHOUT_BOUNDS = "accessor.min and accessor.max must be defined for animation input accessor.";
        public const string ANIMATION_SAMPLER_OUTPUT_ACCESSOR_INVALID_COUNT = "Animation sampler output accessor of count {0} expected.Found {1}.";
        public const string ANIMATION_SAMPLER_OUTPUT_ACCESSOR_INVALID_FORMAT = "Invalid animation sampler output accessor format '{0}' for path '{2}'. Must be one of('%a', '%b', '%c').";
        public const string ANIMATION_SAMPLER_OUTPUT_INTERPOLATION = "The same output accessor cannot be used both for spline and linear data.";
        public const string BUFFER_MISSING_GLB_DATA = "Buffer refers to an unresolved GLB binary chunk.";
        public const string BUFFER_NON_FIRST_GLB = "Buffer referring to GLB binary chunk must be the first.";
        public const string BUFFER_VIEW_TARGET_OVERRIDE = "Override of previously set bufferView target or usage. Initial: '{0}', new: '{1}'.";
        public const string BUFFER_VIEW_TOO_LONG = "BufferView does not fit buffer({0}) byteLength({1}).";
        public const string INVALID_IBM_ACCESSOR_COUNT = "Accessor of count {0} expected.Found {1}.";
        public const string MESH_PRIMITIVE_ACCESSOR_UNALIGNED = "Vertex attribute data must be aligned to 4-byte boundaries.";
        public const string MESH_PRIMITIVE_ACCESSOR_WITHOUT_BYTESTRIDE = "bufferView.byteStride must be defined when two or more accessors use the same buffer view.";
        public const string MESH_PRIMITIVE_ATTRIBUTES_ACCESSOR_INVALID_FORMAT = "Invalid accessor format '{0}' for this attribute semantic. Must be one of ('%a', '%b', '%c').";
        public const string MESH_PRIMITIVE_INDICES_ACCESSOR_INVALID_FORMAT = "Invalid indices accessor format '{0}'. Must be one of('{1}', '{2}', '{3}').";
        public const string MESH_PRIMITIVE_INDICES_ACCESSOR_WITH_BYTESTRIDE = "bufferView.byteStride must not be defined for indices accessor.";
        public const string MESH_PRIMITIVE_MORPH_TARGET_INVALID_ATTRIBUTE_COUNT = "Base accessor has different count.";
        public const string MESH_PRIMITIVE_MORPH_TARGET_NO_BASE_ACCESSOR = "No base accessor for this attribute semantic.";
        public const string MESH_PRIMITIVE_POSITION_ACCESSOR_WITHOUT_BOUNDS = "accessor.min and accessor.max must be defined for POSITION attribute accessor.";
        public const string MESH_PRIMITIVE_TOO_FEW_TEXCOORDS = "Material is incompatible with mesh primitive: Texture binding '{0}' needs 'TEXCOORD_{1}' attribute.";
        public const string MESH_PRIMITIVE_UNEQUAL_ACCESSOR_COUNT = "All accessors of the same primitive must have the same count.";
        public const string NODE_LOOP = "Node is a part of a node loop.";
        public const string NODE_PARENT_OVERRIDE = "Value overrides parent of node {0}.";
        public const string NODE_SKIN_WITH_NON_SKINNED_MESH = "Node has skin defined, but mesh has no joints data.";
        public const string NODE_WEIGHTS_INVALID = "The length of weights array ({0}) does not match the number of morph targets({1}).";
        public const string SCENE_NON_ROOT_NODE = "Node {0} is not a root node.";
        public const string SKIN_IBM_INVALID_FORMAT = "Invalid IBM accessor format '{0}'. Must be one of ('%a', '%b', '%c').";
        public const string UNDECLARED_EXTENSION = "Extension was not declared in extensionsUsed.";
        public const string UNEXPECTED_EXTENSION_OBJECT = "Unexpected location for this extension.";
        public const string UNRESOLVED_REFERENCE = "Unresolved reference: {0}.";
    }
}
