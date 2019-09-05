using System;
using System.Collections.Generic;
using System.Text;

using TARGET = SharpGLTF.IO.JsonSerializable;

namespace SharpGLTF.Validation
{
    /// <summary>
    /// Utility class used in the process of model validation.
    /// </summary>
    [System.Diagnostics.DebuggerStepThrough]
    public struct ValidationContext
    {
        #region constructor

        public ValidationContext(ValidationResult result, TARGET target)
        {
            _Result = result;
            _Target = target;
        }

        #endregion

        #region data

        private readonly TARGET _Target;
        private readonly ValidationResult _Result;

        #endregion

        #region properties

        public ValidationResult Result => _Result;

        #endregion

        #region API

        public ValidationContext GetContext(TARGET target) { return _Result.GetContext(target); }

        #endregion

        #region schema errors

        public bool CheckIsDefined<T>(string property, T? value)
            where T : struct
        {
            if (value.HasValue) return true;

            AddSchemaError(property, ErrorCodes.UNDEFINED_PROPERTY, property);

            return false;
        }

        public void CheckIndex(string property, int? index, int maxExclusive)
        {
            if (!index.HasValue) return;

            if (index.Value < 0) AddSchemaError(property, ErrorCodes.INVALID_INDEX, index);
            if (index.Value >= maxExclusive) AddSchemaError(property, ErrorCodes.VALUE_NOT_IN_RANGE, index);
        }

        public void CheckMultipleOf(string property,  int value, int multiple)
        {
            if ((value % multiple) == 0) return;

            AddSchemaError(property, ErrorCodes.VALUE_MULTIPLE_OF, value, multiple);
        }

        public void CheckJsonSerializable(string property, Object value)
        {
            if (IO.JsonUtils.IsSerializable(value)) return;

            AddSchemaError(property, ErrorCodes.INVALID_JSON, string.Empty);
        }

        public void AddSchemaError(string property, String format, params object[] args)
        {
            var message = property + " " + String.Format(format, args);

            var ex = new SchemaException(_Target, message);

            _Result.AddError(ex);
        }

        #endregion

        #region semantic errors

        public void AddSemanticError(String message)
        {
            var ex = new SemanticException(_Target, message);

            _Result.AddError(ex);
        }

        public void AddSemanticWarning(String format, params object[] args)
        {
            var message = String.Format(format, args);

            var ex = new SemanticException(_Target, message);

            _Result.AddWarning(ex);
        }

        public void AddSemanticError(String format, params object[] args)
        {
            var message = String.Format(format, args);

            var ex = new SemanticException(_Target, message);

            _Result.AddError(ex);
        }

        #endregion

        #region data errors

        public void CheckVertexIndex(int index, UInt32 vertexIndex, UInt32 vertexCount, UInt32 vertexRestart)
        {
            if (vertexIndex == vertexRestart) AddDataError(ErrorCodes.ACCESSOR_INDEX_PRIMITIVE_RESTART, index, vertexIndex);
            else if (vertexIndex >= vertexCount) AddDataError(ErrorCodes.ACCESSOR_INDEX_OOB, index, vertexIndex, vertexCount);
        }

        public void CheckDataIsFinite(int index, System.Numerics.Vector3 v)
        {
            if (v._IsFinite()) return;

            AddDataError(ErrorCodes.ACCESSOR_INVALID_FLOAT, index);
        }

        public void CheckDataIsFinite(int index, System.Numerics.Vector4 v)
        {
            if (v._IsFinite()) return;

            AddDataError(ErrorCodes.ACCESSOR_INVALID_FLOAT, index);
        }

        public void CheckDataIsUnitLength(int index, System.Numerics.Vector3 v)
        {
            if (v.IsValidNormal()) return;

            AddDataError(ErrorCodes.ACCESSOR_NON_UNIT, index, v.Length());
        }

        public void CheckDataIsInRange(int index, System.Numerics.Vector4 v, float minInclusive, float maxInclusive)
        {
            CheckDataIsInRange(index, v.X, minInclusive, maxInclusive);
            CheckDataIsInRange(index, v.Y, minInclusive, maxInclusive);
            CheckDataIsInRange(index, v.Z, minInclusive, maxInclusive);
            CheckDataIsInRange(index, v.W, minInclusive, maxInclusive);
        }

        public void CheckDataIsInRange(int index, float v, float minInclusive, float maxInclusive)
        {
            if (v < minInclusive) AddDataError(ErrorCodes.ACCESSOR_ELEMENT_OUT_OF_MIN_BOUND, index, v);
            if (v > maxInclusive) AddDataError(ErrorCodes.ACCESSOR_ELEMENT_OUT_OF_MAX_BOUND, index, v);
        }

        public void CheckDataIsValidSign(int index, float w)
        {
            if (w == 1 || w == -1) return;

            AddDataError(ErrorCodes.ACCESSOR_INVALID_SIGN, index, w);
        }

        public void AddDataError(String format, params object[] args)
        {
            var message = String.Format(format, args);

            var ex = new DataException(_Target, message);

            _Result.AddError(ex);
        }

        #endregion

        #region link errors

        public bool CheckReferenceIndex<T>(string property, int? index, IReadOnlyList<T> collection)
        {
            if (!index.HasValue) return true;
            if (index.Value >= 0 && index.Value < collection.Count) return true;

            AddLinkError(ErrorCodes.UNRESOLVED_REFERENCE, property);

            return false;
        }

        public void UnsupportedExtensionError(String message)
        {
            AddLinkError(message);
        }

        public void AddLinkError(String format, params object[] args)
        {
            var message = String.Format(format, args);

            var ex = new LinkException(_Target, message);

            _Result.AddError(ex);
        }

        public void AddLinkWarning(String format, params object[] args)
        {
            var message = String.Format(format, args);

            var ex = new LinkException(_Target, message);

            _Result.AddWarning(ex);
        }

        #endregion
    }

    [System.Diagnostics.DebuggerStepThrough]
    public sealed class ValidationResult
    {
        #region data

        private readonly List<Exception> _Errors = new List<Exception>();
        private readonly List<Exception> _Warnings = new List<Exception>();

        #endregion

        #region properties

        public IEnumerable<Exception> Errors => _Errors;

        public bool HasErrors => _Errors.Count > 0;

        #endregion

        #region API

        public ValidationContext GetContext(TARGET target) { return new ValidationContext(this, target); }

        public void AddWarning(ModelException ex)
        {
            _Warnings.Add(ex);
        }

        public void AddError(ModelException ex)
        {
            #if DEBUG
            throw ex;
            #else
            _Errors.Add(ex);
            #endif
        }

        #endregion
    }
}
