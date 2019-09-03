using System;
using System.Collections.Generic;
using System.Text;

namespace SharpGLTF.Validation
{
    static partial class ErrorCodes
    {
        // WARNINGS

        public const string UNEXPECTED_PROPERTY = "Unexpected property.";
        public const string VALUE_NOT_IN_LIST = "Invalid value '{0}'. Valid values are ('%a', '%b', '%c').";

        // ERRRORS

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
    }
}
