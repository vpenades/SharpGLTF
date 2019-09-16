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

        public ValidationResult(Schema2.ModelRoot root, bool instantThrow = false)
        {
            _Root = root;
            _InstantThrow = instantThrow;
        }

        #endregion

        #region data

        private readonly Schema2.ModelRoot _Root;
        private readonly bool _InstantThrow;

        private readonly List<Exception> _Errors = new List<Exception>();
        private readonly List<Exception> _Warnings = new List<Exception>();

        #endregion

        #region properties

        public Schema2.ModelRoot Root => _Root;

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
            if (_InstantThrow) throw ex;

            _Errors.Add(ex);
        }

        #endregion
    }
}
