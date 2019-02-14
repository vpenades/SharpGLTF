using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace glTF2Sharp
{
    using IO;

    /// <summary>
    /// Represents an exception produced by the serialization or validation of a gltf model
    /// </summary>
    public class ModelException : Exception
    {
        #region lifecycle

        internal ModelException(JsonSerializable target, String message)
            : base(_CreateBaseMessage(target, message))
        {
            _Target = target;
        }

        internal ModelException(JsonSerializable target, String message, Action fix, String fixDesc)
            : base(message)
        {
            _Target = target;
            _ProposedFix = fix;
            _ProposedFixDescription = fixDesc;
        }

        private static string _CreateBaseMessage(JsonSerializable target, String message)
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

        private readonly JsonSerializable _Target;

        private String _ProposedFixDescription;
        private Action _ProposedFix;

        #endregion

        #region properties

        public bool HasFix => _ProposedFix != null;
        public String FixDescription => _ProposedFixDescription;
        public void ApplyFix() { _ProposedFix.Invoke(); }

        #endregion
    }

    /// <summary>
    /// Represents an exception produced when a required extension is missing
    /// </summary>
    public class UnsupportedExtensionException : ModelException
    {
        #region lifecycle

        internal UnsupportedExtensionException(JsonSerializable target, String message)
            : base(target, message)
        {
        }

        internal UnsupportedExtensionException(JsonSerializable target, String message, Action fix, String fixDesc)
            : base(target, message, fix, fixDesc)
        {
        }

        #endregion
    }
}
