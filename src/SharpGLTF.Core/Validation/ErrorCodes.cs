using System;
using System.Collections.Generic;
using System.Text;

namespace SharpGLTF.Validation
{
    static class ErrorCodes
    {
        #region DATA

        public const string ACCESSOR_ANIMATION_INPUT_NEGATIVE = "Animation input accessor element at index {0} is negative: {1}.";
        public const string ACCESSOR_ANIMATION_INPUT_NON_INCREASING = "Animation input accessor element at index {0} is less than or equal to previous: {1} <= {2}.";
        public const string ACCESSOR_ELEMENT_OUT_OF_MAX_BOUND = "Accessor contains {0} element(s) greater than declared maximum value {1}.";
        public const string ACCESSOR_ELEMENT_OUT_OF_MIN_BOUND = "Accessor contains {0} element(s) less than declared minimum value {1}.";
        public const string ACCESSOR_INDECOMPOSABLE_MATRIX = "Matrix element at index {0} is not decomposable to TRS.";
        public const string ACCESSOR_INDEX_OOB = "Indices accessor element at index {0} has vertex index {1} that exceeds number of available vertices {2}.";
        public const string ACCESSOR_INDEX_PRIMITIVE_RESTART = "Indices accessor contains primitive restart value ({0}) at index {1}.";
        public const string ACCESSOR_INVALID_FLOAT = "Accessor element at index {0} is NaN or Infinity.";
        public const string ACCESSOR_INVALID_SIGN = "Accessor element at index {0} has invalid w component: {1}. Must be 1.0 or -1.0.";
        public const string ACCESSOR_MAX_MISMATCH = "Declared maximum value for this component ({0}) does not match actual maximum({1}).";
        public const string ACCESSOR_MIN_MISMATCH = "Declared minimum value for this component({0}) does not match actual minimum({1}).";
        public const string ACCESSOR_NON_UNIT = "Accessor element at index {0} is not of unit length: {1}.";
        public const string ACCESSOR_SPARSE_INDEX_OOB = "Accessor sparse indices element at index {0} is greater than or equal to the number of accessor elements: {1} >= {2}.";
        public const string ACCESSOR_SPARSE_INDICES_NON_INCREASING = "Accessor sparse indices element at index {0} is less than or equal to previous: {1} <= {2}.";
        public const string BUFFER_EMBEDDED_BYTELENGTH_MISMATCH = "Actual data length {0} is not equal to the declared buffer byteLength {1}.";
        public const string BUFFER_EXTERNAL_BYTELENGTH_MISMATCH = "Actual data length {0} is less than the declared buffer byteLength {1}.";
        public const string IMAGE_DATA_INVALID = "Image data is invalid. {0}";
        public const string IMAGE_MIME_TYPE_INVALID = "Recognized image format '{0}' does not match declared image format '{1}'.";
        public const string IMAGE_UNEXPECTED_EOS = "Unexpected end of image stream.";

        #endregion

        #region LINK

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

        #endregion

        #region SCHEMA

        public const string ARRAY_LENGTH_NOT_IN_LIST = "Invalid array length {0}. Valid lengths are: ('%a', '%b', '%c').";
        public const string ARRAY_TYPE_MISMATCH = "Type mismatch. Array element '{0}' is not a '{1}'.";
        public const string DUPLICATE_ELEMENTS = "Duplicate element.";
        public const string EMPTY_ENTITY = "Entity cannot be empty.";
        public const string INVALID_INDEX = "Index must be a non-negative integer.";
        public const string INVALID_JSON = "Invalid JSON data.Parser output: {0}";
        public const string INVALID_URI = "Invalid URI '{0}'. Parser output: {1}";
        public const string ONE_OF_MISMATCH = "Exactly one of ('{0}', '{1}', '{2}', '{3}') properties must be defined.";
        public const string PATTERN_MISMATCH = "Value '{0}' does not match regexp pattern '{1}'.";
        public const string TYPE_MISMATCH = "Type mismatch. Property value '{0}' is not a '{1}'.";
        public const string UNDEFINED_PROPERTY = "Property '{0}' must be defined.";
        public const string UNSATISFIED_DEPENDENCY = "Dependency failed. '{0}' must be defined.";
        public const string VALUE_MULTIPLE_OF = "Value {0} is not a multiple of {1}.";
        public const string VALUE_NOT_IN_RANGE = "Value {0} is out of range.";

        #endregion

        #region SEMANTIC

        public const string ACCESSOR_MATRIX_ALIGNMENT = "Matrix accessors must be aligned to 4-byte boundaries.";
        public const string ACCESSOR_NORMALIZED_INVALID = "Only (u)byte and (u)short accessors can be normalized.";
        public const string ACCESSOR_OFFSET_ALIGNMENT = "Offset {0} is not a multiple of componentType length {1}.";
        public const string ACCESSOR_SPARSE_COUNT_OUT_OF_RANGE = "Sparse accessor overrides more elements ({0}) than the base accessor contains({1}).";
        public const string BUFFER_DATA_URI_MIME_TYPE_INVALID = "Buffer's Data URI MIME-Type must be 'application/octet-stream' or 'application/gltf-buffer'. Found '{0}' instead.";
        public const string BUFFER_VIEW_INVALID_BYTE_STRIDE = "Only buffer views with raw vertex data can have byteStride.";
        public const string BUFFER_VIEW_TOO_BIG_BYTE_STRIDE = "Buffer view's byteStride ({0}) is smaller than byteLength ({1}).";
        public const string CAMERA_ZFAR_LEQUAL_ZNEAR = "zfar must be greater than znear.";
        public const string INVALID_GL_VALUE = "Invalid value {0} for GL type '{1}'.";
        public const string KHR_LIGHTS_PUNCTUAL_LIGHT_SPOT_ANGLES = "outerConeAngle ({1}) is less than or equal to innerConeAngle({0}).";
        public const string MESH_INVALID_WEIGHTS_COUNT = "The length of weights array ({0}) does not match the number of morph targets({1}).";
        public const string MESH_PRIMITIVES_UNEQUAL_TARGETS_COUNT = "All primitives must have the same number of morph targets.";
        public const string MESH_PRIMITIVE_INDEXED_SEMANTIC_CONTINUITY = "Indices for indexed attribute semantic '{0}' must start with 0 and be continuous.Total expected indices: {1}, total provided indices: {2}.";
        public const string MESH_PRIMITIVE_INVALID_ATTRIBUTE = "Invalid attribute name.";
        public const string MESH_PRIMITIVE_JOINTS_WEIGHTS_MISMATCH = "Number of JOINTS attribute semantics must match number of WEIGHTS.";
        public const string NODE_MATRIX_NON_TRS = "Matrix must be decomposable to TRS.";
        public const string NODE_MATRIX_TRS = "A node can have either a matrix or any combination of translation/rotation/scale (TRS) properties.";
        public const string ROTATION_NON_UNIT = "Rotation quaternion must be normalized.";
        public const string UNKNOWN_ASSET_MAJOR_VERSION = "Unknown glTF major asset version: {0}.";
        public const string UNUSED_EXTENSION_REQUIRED = "Unused extension '{0}' cannot be required.";

        #endregion
    }
}
