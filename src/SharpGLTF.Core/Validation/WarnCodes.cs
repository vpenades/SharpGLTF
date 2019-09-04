using System;
using System.Collections.Generic;
using System.Text;

namespace SharpGLTF.Validation
{
    static class WarnCodes
    {
        #region DATA

        public const string BUFFER_GLB_CHUNK_TOO_BIG = "GLB-stored BIN chunk contains {0} extra padding byte (s).";
        public const string IMAGE_UNRECOGNIZED_FORMAT = "Image format not recognized.";

        #endregion

        #region LINK

        public const string MESH_PRIMITIVE_INCOMPATIBLE_MODE = "Number of vertices or indices({0}) is not compatible with used drawing mode('{1}').";
        public const string NODE_SKINNED_MESH_WITHOUT_SKIN = "Node uses skinned mesh, but has no skin defined.";
        public const string UNSUPPORTED_EXTENSION = "Cannot validate an extension as it is not supported by the validator: '{0}'.";

        #endregion

        #region SCHEMA

        public const string UNEXPECTED_PROPERTY = "Unexpected property.";
        public const string VALUE_NOT_IN_LIST = "Invalid value '{0}'. Valid values are ('%a', '%b', '%c').";

        #endregion

        #region SEMANTIC

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

        #endregion
    }
}
