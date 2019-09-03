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

        private readonly List<Exception> _Errors = new List<Exception>();
        private readonly List<Exception> _Warnings = new List<Exception>();

        public IEnumerable<Exception> Errors => _Errors;

        public bool HasErrors => _Errors.Count > 0;

        #endregion

        #region errors

        private void _AddError(Exception ex)
        {
            throw ex;
            _Errors.Add(ex);
        }

        public void AddError(TARGET target, String message)
        {
            _AddError(new ModelException(target, message));
        }

        #endregion

        #region schema errors

        public void CheckIsDefined<T>(TARGET target, string property, T? value)
            where T : struct
        {
            if (!value.HasValue) AddSchemaError(target, property, ErrorCodes.UNDEFINED_PROPERTY);
        }

        public void CheckIndex(TARGET target, string property, int? index, int maxExclusive)
        {
            if (index.HasValue)
            {
                if (index.Value < 0) AddSchemaError(target, property, ErrorCodes.INVALID_INDEX, index);
                if (index.Value >= maxExclusive) AddSchemaError(target, property, ErrorCodes.VALUE_NOT_IN_RANGE, index);
            }
        }

        public void CheckMultipleOf(TARGET target, string property,  int value, int multiple)
        {
            if ((value % multiple) != 0) AddSchemaError(target, property, ErrorCodes.VALUE_MULTIPLE_OF, value, multiple);
        }

        public void CheckJsonSerializable(TARGET target, string property, Object value)
        {
            if (!IO.JsonUtils.IsSerializable(value)) AddSchemaError(target, property, ErrorCodes.INVALID_JSON, string.Empty);
        }

        public void AddSchemaError(TARGET target, string property, String format, params object[] args)
        {
            _AddError(new SchemaException(target, property + " " + String.Format(format, args)));
        }

        #endregion

        #region semantic errors

        public void AddSemanticError(TARGET target, String message)
        {
            _Errors.Add(new SemanticException(target, message));
        }

        public void AddSemanticWarning(TARGET target, String format, params object[] args)
        {
            _Warnings.Add(new SemanticException(target, String.Format(format, args)));
        }

        public void AddSemanticError(TARGET target, String format, params object[] args)
        {
            _AddError(new SemanticException(target, String.Format(format, args)));
        }

        #endregion

        #region data errors

        public void CheckVertexIndex(Schema2.Accessor target, int index, UInt32 vertexIndex, UInt32 vertexCount, UInt32 vertexRestart)
        {
            if (vertexIndex == vertexRestart) AddDataError(target, ErrorCodes.ACCESSOR_INDEX_PRIMITIVE_RESTART, index, vertexIndex);
            else if (vertexIndex >= vertexCount) AddDataError(target, ErrorCodes.ACCESSOR_INDEX_OOB, index, vertexIndex, vertexCount);
        }

        public void CheckDataIsFinite(Schema2.Accessor target, int index, System.Numerics.Vector3 v)
        {
            if (!v._IsFinite()) AddDataError(target, ErrorCodes.ACCESSOR_INVALID_FLOAT, index);
        }

        public void CheckDataIsFinite(Schema2.Accessor target, int index, System.Numerics.Vector4 v)
        {
            if (!v._IsFinite()) AddDataError(target, ErrorCodes.ACCESSOR_INVALID_FLOAT, index);
        }

        public void CheckDataIsUnitLength(Schema2.Accessor target, int index, System.Numerics.Vector3 v)
        {
            if (!v.IsValidNormal()) AddDataError(target, ErrorCodes.ACCESSOR_NON_UNIT, index, v.Length());
        }

        public void CheckDataIsInRange(Schema2.Accessor target, int index, System.Numerics.Vector4 v, float minInclusive, float maxInclusive)
        {
            CheckDataIsInRange(target, index, v.X, minInclusive, maxInclusive);
            CheckDataIsInRange(target, index, v.Y, minInclusive, maxInclusive);
            CheckDataIsInRange(target, index, v.Z, minInclusive, maxInclusive);
            CheckDataIsInRange(target, index, v.W, minInclusive, maxInclusive);
        }

        public void CheckDataIsInRange(Schema2.Accessor target, int index, float v, float minInclusive, float maxInclusive)
        {
            if (v < minInclusive) AddDataError(target, ErrorCodes.ACCESSOR_ELEMENT_OUT_OF_MIN_BOUND, index, v);
            if (v > maxInclusive) AddDataError(target, ErrorCodes.ACCESSOR_ELEMENT_OUT_OF_MAX_BOUND, index, v);
        }

        public void CheckDataIsValidSign(Schema2.Accessor target, int index, float w)
        {
            if (w != 1 && w != -1) AddDataError(target, ErrorCodes.ACCESSOR_INVALID_SIGN, index, w);
        }

        public void AddDataError(TARGET target, String format, params object[] args)
        {
            _AddError(new DataException(target, String.Format(format, args)));
        }

        #endregion

        #region link errors

        public void UnsupportedExtensionError(TARGET target, String message)
        {
            AddLinkError(target, message);
        }

        public void AddLinkError(TARGET target, String format, params object[] args)
        {
            _AddError(new LinkException(target, String.Format(format, args)));
        }

        #endregion
    }
}
