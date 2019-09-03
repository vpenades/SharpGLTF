using System;
using System.Collections.Generic;
using System.Text;

using TARGET = SharpGLTF.IO.JsonSerializable;

namespace SharpGLTF.Validation
{
    /// <summary>
    /// Utility class used in the process of model validation.
    /// </summary>
    class ValidationContext
    {
        // we should try to align validation errors to these issues:
        // https://github.com/KhronosGroup/glTF-Validator/blob/master/ISSUES.md

        #region data

        private readonly List<Exception> _Exceptions = new List<Exception>();

        public IEnumerable<Exception> Exceptions => _Exceptions;

        public bool HasErrors => _Exceptions.Count > 0;

        #endregion

        #region errors

        public void AddError(TARGET target, String message)
        {
            _Exceptions.Add(new ModelException(target, message));
        }

        #endregion

        #region schema errors

        public void AddSchemaError(TARGET target, String message)
        {
            _Exceptions.Add(new SchemaException(target, message));
        }

        public void InvalidJson(TARGET target, string message)
        {
            AddSchemaError(target, $"Invalid JSON data. Parser output: {message}");
        }

        #endregion

        #region semantic errors

        public void AddSemanticError(TARGET target, String message)
        {
            _Exceptions.Add(new SemanticException(target, message));
        }

        public void AddSemanticError(TARGET target, String format, params object[] args)
        {
            _Exceptions.Add(new SemanticException(target, String.Format(format, args)));
        }

        #endregion

        #region data errors

        public void AddDataError(TARGET target, String message)
        {
            _Exceptions.Add(new DataException(target, message));
        }

        #endregion

        #region link errors

        public void AddLinkError(TARGET target, String message)
        {
            _Exceptions.Add(new LinkException(target, message));
        }

        public void UnsupportedExtensionError(TARGET target, String message)
        {
            AddLinkError(target, message);
        }

        #endregion
    }
}
