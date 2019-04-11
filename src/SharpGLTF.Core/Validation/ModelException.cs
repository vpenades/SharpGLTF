using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace SharpGLTF.Validation
{
    using TARGET = IO.JsonSerializable;

    /// <summary>
    /// Represents an exception produced by the serialization or validation of a gltf model.
    /// </summary>
    public class ModelException : Exception
    {
        #region lifecycle

        internal ModelException(TARGET target, String message)
            : base(_CreateBaseMessage(target, message))
        {
            _Target = target;
        }

        private static string _CreateBaseMessage(TARGET target, String message)
        {
            if (target == null) return message;

            var targetTypeInfo = target.GetType().GetTypeInfo();

            var logicalIndexProp = targetTypeInfo.GetProperty("LogicalIndex");

            var logicalIndex = logicalIndexProp != null ? (int)logicalIndexProp.GetValue(target) : -1;

            if (logicalIndex >= 0) return $"{targetTypeInfo.Name}[{logicalIndex}] {message}";

            return $"{targetTypeInfo.Name} {message}";
        }

        #endregion

        #region data

        private readonly TARGET _Target;

        #endregion
    }

    /// <summary>
    /// Represents an exception produced by an invalid JSON document.
    /// </summary>
    public class SchemaException : ModelException
    {
        internal SchemaException(TARGET target, String message)
            : base(target, message) { }
    }

    /// <summary>
    /// Represents an esception produced by invalid values.
    /// </summary>
    public class SemanticException : ModelException
    {
        internal SemanticException(TARGET target, String message)
            : base(target, message) { }
    }

    /// <summary>
    /// Represents an exception produced by invalid objects relationships.
    /// </summary>
    public class LinkException : ModelException
    {
        internal LinkException(TARGET target, String message)
            : base(target, message) { }
    }

    /// <summary>
    /// Represents an exception produced by invalid data.
    /// </summary>
    public class DataException : ModelException
    {
        internal DataException(TARGET target, String message)
            : base(target, message) { }
    }
}