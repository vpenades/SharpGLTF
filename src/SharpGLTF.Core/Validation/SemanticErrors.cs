using System;
using System.Collections.Generic;
using System.Text;

namespace SharpGLTF.Validation
{
    static class SemanticErrors
    {
        #pragma warning disable SA1310 // Field names should not contain underscore

        // INFORMATION

        public const string EXTRA_PROPERTY = "This property should not be defined as it will not be used.";
        public const string NODE_EMPTY = "Empty node encountered.";
        public const string NODE_MATRIX_DEFAULT = "Do not specify default transform matrix.";
        public const string NON_OBJECT_EXTRAS = "Prefer JSON Objects for extras.";

        // WARNINGS

        public const string ASSET_MIN_VERSION_GREATER_THAN_VERSION = "Asset minVersion '{0}' is greater than version '{1}'.";
        public const string CAMERA_XMAG_YMAG_ZERO = "xmag and ymag must not be zero.";
        public const string INTEGER_WRITTEN_AS_FLOAT = "Integer value is written with fractional part: {0}.";
        public const string MATERIAL_ALPHA_CUTOFF_INVALID_MODE = "Alpha cutoff is supported only for 'MASK' alpha mode.";
        public const string MESH_PRIMITIVES_UNEQUAL_JOINTS_COUNT = "All primitives should contain the same number of 'JOINTS' and 'WEIGHTS' attribute sets.";
        public const string MESH_PRIMITIVE_TANGENT_POINTS = "TANGENT attribute defined for POINTS rendering mode.";
        public const string MESH_PRIMITIVE_TANGENT_WITHOUT_NORMAL = "TANGENT attribute without NORMAL found.";
        public const string MULTIPLE_EXTENSIONS = "Multiple extensions are defined for this object: ('%a', '%b', '%c').";
        public const string MESH_PRIMITIVE_NO_POSITION = "No POSITION attribute found.";
        public const string NON_RELATIVE_URI = "Non-relative URI found: {0}.";
        public const string UNKNOWN_ASSET_MINOR_VERSION = "Unknown glTF minor asset version: {0}.";
        public const string UNRESERVED_EXTENSION_PREFIX = "Extension uses unreserved extension prefix '{0}'.";

        // ERRORS

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

        #pragma warning restore SA1310 // Field names should not contain underscore
    }
}
