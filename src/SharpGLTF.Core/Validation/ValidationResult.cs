using System;
using System.Collections.Generic;
using System.Text;

using TARGET = SharpGLTF.IO.JsonSerializable;

namespace SharpGLTF.Validation
{
    [System.Diagnostics.DebuggerStepThrough]
    public sealed class ValidationResult
    {
        #region lifecycle

        public ValidationResult(Schema2.ModelRoot root, ValidationMode mode, bool instantThrow = false)
        {
            _Root = root;
            _Mode = mode;
            _InstantThrow = instantThrow;
        }

        #endregion

        #region data

        private readonly Schema2.ModelRoot _Root;
        private readonly ValidationMode _Mode;
        private readonly bool _InstantThrow;

        private readonly List<Exception> _Errors = new List<Exception>();

        #endregion

        #region properties

        public Schema2.ModelRoot Root => _Root;

        public ValidationMode Mode => _Mode;

        public IEnumerable<Exception> Errors => _Errors;

        public bool HasErrors => _Errors.Count > 0;

        #endregion

        #region API

        public ValidationContext GetContext() { return new ValidationContext(this); }

        public void SetError(ModelException ex)
        {
            if (_InstantThrow) throw ex;

            _Errors.Add(ex);
        }

        #endregion
    }
}
