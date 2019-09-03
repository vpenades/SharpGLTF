using System;
using System.Collections.Generic;
using System.Text;

namespace SharpGLTF.Validation
{
    static partial class ErrorCodes
    {
        // INFORMATION

        public const string ACCESSOR_INDEX_TRIANGLE_DEGENERATE = "Indices accessor contains {0} degenerate triangles.";
        public const string DATA_URI_GLB = "Data URI is used in GLB container.";
        public const string IMAGE_NPOT_DIMENSIONS = "Image has non-power-of-two dimensions: {0}x{1}.";

        // WARNINGS

        public const string BUFFER_GLB_CHUNK_TOO_BIG = "GLB-stored BIN chunk contains {0} extra padding byte (s).";
        public const string IMAGE_UNRECOGNIZED_FORMAT = "Image format not recognized.";

        // ERRORS

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
    }
}
