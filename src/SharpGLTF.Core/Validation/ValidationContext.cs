using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using TARGET = SharpGLTF.IO.JsonSerializable;

namespace SharpGLTF.Validation
{
    /// <summary>
    /// Utility class used in the process of model validation.
    /// </summary>
    public readonly partial struct ValidationContext
    {
        #region constructor

        public ValidationContext(ValidationResult result)
        {
            Guard.NotNull(result, nameof(result));
            _Root = result.Root;
            _Mode = result.Mode;
            _Current = null;
        }

        internal ValidationContext(ValidationContext context, TARGET target)
        {
            _Root = context._Root;
            _Mode = context._Mode;
            _Current = target;
        }

        #endregion

        #region data

        private readonly Schema2.ModelRoot _Root;
        private readonly ValidationMode _Mode;

        #endregion

        #region properties

        public Schema2.ModelRoot Root => _Root;

        public bool TryFix => _Mode == ValidationMode.TryFix;

        #endregion

        #region API

        public ValidationContext GetContext(TARGET target) { return new ValidationContext(this, target); }

        #endregion

    }

    public readonly struct ValueLocation
    {
        #region constructors

        public static implicit operator ValueLocation(int index) { return new ValueLocation(string.Empty, index); }

        public static implicit operator ValueLocation(int? index) { return new ValueLocation(string.Empty, index ?? 0); }

        public static implicit operator ValueLocation(string name) { return new ValueLocation(name); }

        public static implicit operator ValueLocation((string name, int index) tuple) { return new ValueLocation(tuple.name, tuple.index); }

        public static implicit operator ValueLocation((string name, int? index) tuple) { return new ValueLocation(tuple.name, tuple.index ?? 0); }

        public static implicit operator String(ValueLocation location) { return location.ToString(); }

        private ValueLocation(string name, int idx1 = -1)
        {
            _Name = name;
            _Index = idx1;
        }

        #endregion

        #region

        private readonly string _Name;
        private readonly int _Index;

        #endregion

        #region API

        public override string ToString()
        {
            if (_Index >= 0) return $"{_Name}[{_Index}]";
            return _Name;
        }

        public string ToString(TARGET target, string message)
        {
            return ToString(target) + " " + message;
        }

        public string ToString(TARGET target)
        {
            if (target == null) return this.ToString();

            var name = target.GetType().Name;

            var pinfo = target.GetType().GetProperty("LogicalIndex");

            if (pinfo != null)
            {
                var idx = pinfo.GetValue(target);

                name += $"[{idx}]";
            }

            return name + this.ToString();
        }

        #endregion
    }
}
