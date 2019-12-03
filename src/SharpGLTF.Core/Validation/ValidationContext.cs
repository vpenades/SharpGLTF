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

        public Schema2.ModelRoot Root => _Result.Root;

        public ValidationResult Result => _Result;

        public bool TryFix => Result.Mode == Validation.ValidationMode.TryFix;

        #endregion

        #region API

        public ValidationContext GetContext(TARGET target) { return _Result.GetContext(target); }

        public void AddSchemaError(ValueLocation location, string message) { AddSchemaError(location.ToString(_Target, message)); }

        public bool TryFixLinkOrError(ValueLocation location, string message)
        {
            if (TryFix) AddLinkWarning(location.ToString(_Target, message));
            else AddLinkError(location.ToString(_Target, message));

            return TryFix;
        }

        public bool TryFixDataOrError(ValueLocation location, string message)
        {
            if (TryFix) AddDataWarning(location.ToString(_Target, message));
            else AddDataError(location.ToString(_Target, message));

            return TryFix;
        }

        public void AddLinkError(ValueLocation location, string message) { AddLinkError(location.ToString(_Target, message)); }

        public void AddLinkWarning(String format, params object[] args) { AddLinkWarning(String.Format(format, args)); }

        public void AddDataError(ValueLocation location, string message) { AddDataError(location.ToString(_Target, message)); }

        public void AddDataWarning(ValueLocation location, string message) { AddDataWarning(location.ToString(_Target, message)); }

        public void AddSemanticWarning(String format, params object[] args) { AddSemanticWarning(String.Format(format, args)); }

        public void AddSemanticError(String format, params object[] args) { AddSemanticError(String.Format(format, args)); }

        public void AddLinkWarning(string message)
        {
            var ex = new LinkException(_Target, message);

            _Result.AddWarning(ex);
        }

        public void AddLinkError(string message)
        {
            var ex = new LinkException(_Target, message);

            _Result.AddError(ex);
        }

        public void AddSchemaError(string message)
        {
            var ex = new SchemaException(_Target, message);

            _Result.AddError(ex);
        }

        public void AddDataWarning(string message)
        {
            var ex = new DataException(_Target, message);
            _Result.AddWarning(ex);
        }

        public void AddDataError(string message)
        {
            var ex = new DataException(_Target, message);
            _Result.AddError(ex);
        }

        public void AddSemanticError(String message)
        {
            var ex = new SemanticException(_Target, message);

            _Result.AddError(ex);
        }

        public void AddSemanticWarning(String message)
        {
            var ex = new SemanticException(_Target, message);

            _Result.AddWarning(ex);
        }

        #endregion

        #region schema errors

        public bool CheckSchemaIsDefined<T>(ValueLocation location, T value)
            where T : class
        {
            if (value != null) return true;

            AddSchemaError(location, "must be defined.");

            return false;
        }

        public bool CheckSchemaIsDefined<T>(ValueLocation location, T? value)
            where T : struct
        {
            if (value.HasValue) return true;

            AddSchemaError(location, "must be defined.");

            return false;
        }

        public bool CheckSchemaNonNegative(ValueLocation location, int? value)
        {
            if ((value ?? 0) >= 0) return true;
            AddSchemaError(location, "must be a non-negative integer.");
            return false;
        }

        public void CheckSchemaIsInRange<T>(ValueLocation location, T value, T minInclusive, T maxInclusive)
            where T : IComparable<T>
        {
            if (value.CompareTo(minInclusive) == -1) AddSchemaError(location, $"is below minimum {minInclusive} value: {value}");
            if (value.CompareTo(maxInclusive) == +1) AddSchemaError(location, $"is above maximum {maxInclusive} value: {value}");
        }

        public void CheckSchemaIsMultipleOf(ValueLocation location,  int value, int multiple)
        {
            if ((value % multiple) == 0) return;

            AddSchemaError(location, $"Value {value} is not a multiple of {multiple}.");
        }

        public void CheckSchemaIsJsonSerializable(ValueLocation location, Object value)
        {
            if (IO.JsonUtils.IsSerializable(value)) return;

            AddSchemaError(location, "Invalid JSON data.");
        }

        #pragma warning disable CA1054 // Uri parameters should not be strings

        public void CheckSchemaIsValidURI(ValueLocation location, string gltfURI)
        {
            if (string.IsNullOrEmpty(gltfURI)) return;

            if (gltfURI.StartsWith("data:", StringComparison.Ordinal))
            {
                // check decoding
                return;
            }

            if (Uri.TryCreate(gltfURI, UriKind.Relative, out Uri xuri)) return;

            AddSchemaError(location, $"Invalid URI '{gltfURI}'.");
        }

        #pragma warning restore CA1054 // Uri parameters should not be strings

        #endregion

        #region semantic errors

        #endregion

        #region data errors

        public void CheckVertexIndex(ValueLocation location, UInt32 vertexIndex, UInt32 vertexCount, UInt32 vertexRestart)
        {
            if (vertexIndex == vertexRestart)
            {
                AddDataError(location, $"is a primitive restart value ({vertexIndex})");
                return;
            }

            if (vertexIndex >= vertexCount)
            {
                AddDataError(location, $"has a value ({vertexIndex}) that exceeds number of available vertices ({vertexCount})");
                return;
            }
        }

        public bool CheckIsFinite(ValueLocation location, System.Numerics.Vector2? value)
        {
            if (!value.HasValue) return true;
            if (value.Value._IsFinite()) return true;
            AddDataError(location, $"is NaN or Infinity.");
            return false;
        }

        public bool CheckIsFinite(ValueLocation location, System.Numerics.Vector3? value)
        {
            if (!value.HasValue) return true;
            if (value.Value._IsFinite()) return true;
            AddDataError(location, "is NaN or Infinity.");
            return false;
        }

        public bool CheckIsFinite(ValueLocation location, System.Numerics.Vector4? value)
        {
            if (!value.HasValue) return true;
            if (value.Value._IsFinite()) return true;
            AddDataError(location, "is NaN or Infinity.");
            return false;
        }

        public bool CheckIsFinite(ValueLocation location, System.Numerics.Quaternion? value)
        {
            if (!value.HasValue) return true;
            if (value.Value._IsFinite()) return true;
            AddDataError(location, "is NaN or Infinity.");
            return false;
        }

        public bool TryFixUnitLengthOrError(ValueLocation location, System.Numerics.Vector3? value)
        {
            if (!value.HasValue) return false;
            if (!CheckIsFinite(location, value)) return false;
            if (value.Value.IsValidNormal()) return false;

            return TryFixDataOrError(location, $"is not of unit length: {value.Value.Length()}.");
        }

        public bool TryFixTangentOrError(ValueLocation location, System.Numerics.Vector4 tangent)
        {
            if (TryFixUnitLengthOrError(location, new System.Numerics.Vector3(tangent.X, tangent.Y, tangent.Z))) return true;

            if (tangent.W == 1 || tangent.W == -1) return false;

            return TryFixDataOrError(location, $"has invalid value: {tangent.W}. Must be 1.0 or -1.0.");
        }

        public void CheckIsInRange(ValueLocation location, System.Numerics.Vector4 v, float minInclusive, float maxInclusive)
        {
            CheckIsInRange(location, v.X, minInclusive, maxInclusive);
            CheckIsInRange(location, v.Y, minInclusive, maxInclusive);
            CheckIsInRange(location, v.Z, minInclusive, maxInclusive);
            CheckIsInRange(location, v.W, minInclusive, maxInclusive);
        }

        public void CheckIsInRange(ValueLocation location, float value, float minInclusive, float maxInclusive)
        {
            if (value < minInclusive) AddDataError(location, $"is below minimum {minInclusive} value: {value}");
            if (value > maxInclusive) AddDataError(location, $"is above maximum {maxInclusive} value: {value}");
        }

        public bool CheckIsMatrix(ValueLocation location, System.Numerics.Matrix4x4? matrix)
        {
            if (matrix == null) return true;

            if (!matrix.Value._IsFinite())
            {
                AddDataError(location, "is NaN or Infinity.");
                return false;
            }

            if (!System.Numerics.Matrix4x4.Decompose(matrix.Value, out System.Numerics.Vector3 s, out System.Numerics.Quaternion r, out System.Numerics.Vector3 t))
            {
                AddDataError(location, "is not decomposable to TRS.");
                return false;
            }

            return true;
        }

        #endregion

        #region link errors

        public bool CheckArrayIndexAccess<T>(ValueLocation location, int? index, IReadOnlyList<T> array)
        {
            return CheckArrayRangeAccess(location, index, 1, array);
        }

        public bool CheckArrayRangeAccess<T>(ValueLocation location, int? offset, int length, IReadOnlyList<T> array)
        {
            if (!offset.HasValue) return true;

            if (!CheckSchemaNonNegative(location, offset)) return false;

            if (length <= 0)
            {
                AddSchemaError(location, "Invalid length");
                return false;
            }

            if (array == null)
            {
                AddLinkError(location, $"Index {offset} exceeds the number of available items (null).");
                return false;
            }

            if (offset > array.Count - length)
            {
                if (length == 1) AddLinkError(location, $"Index {offset} exceeds the number of available items ({array.Count}).");
                else AddLinkError(location, $"Index {offset}+{length} exceeds the number of available items ({array.Count}).");
                return false;
            }

            return true;
        }

        public bool CheckLinkMustBeAnyOf<T>(ValueLocation location, T value, params T[] values)
        {
            if (values.Contains(value)) return true;

            var validValues = string.Join(" ", values);

            AddLinkError(location, $"value {value} is invalid. Must be one of {validValues}");

            return false;
        }

        public bool CheckLinksInCollection<T>(ValueLocation location, IEnumerable<T> collection)
            where T : class
        {
            int idx = 0;

            if (collection == null)
            {
                AddLinkError(location, "Is NULL.");
                return false;
            }

            var uniqueInstances = new HashSet<T>();

            foreach (var v in collection)
            {
                if (v == null)
                {
                    AddLinkError((location, idx), "Is NULL.");
                    return false;
                }
                else if (uniqueInstances.Contains(v))
                {
                    AddSchemaError((location, idx), "Is duplicated.");
                    return false;
                }

                uniqueInstances.Add(v);

                ++idx;
            }

            return true;
        }

        public void UnsupportedExtensionError(String message)
        {
            AddLinkError(message);
        }

        #endregion
    }

    public struct ValueLocation
    {
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

        private readonly string _Name;
        private readonly int _Index;

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
    }
}
