using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SharpGLTF.Validation
{
    using OUTTYPE = ValidationContext;

    using PARAMNAME = ValueLocation;

    [System.Diagnostics.DebuggerDisplay("{_Current}")]
    [System.Diagnostics.DebuggerStepThrough]
    partial struct ValidationContext
    {
        private readonly IO.JsonSerializable _Current;

        #region schema

        [System.Diagnostics.DebuggerStepThrough]
        internal void _SchemaThrow(PARAMNAME pname, string msg) { throw new SchemaException(_Current, $"{pname}: {msg}"); }

        public OUTTYPE IsTrue(PARAMNAME parameterName, bool value, string msg)
        {
            if (!value) _SchemaThrow(parameterName, msg);
            return this;
        }

        public OUTTYPE NotNull(PARAMNAME parameterName, object target)
        {
            if (target == null) _SchemaThrow(parameterName, "must not be null.");
            return this;
        }

        public OUTTYPE IsDefined<T>(PARAMNAME parameterName, T value)
            where T : class
        {
            if (value == null) _SchemaThrow(parameterName, "must be defined.");
            return this;
        }

        public OUTTYPE IsDefined<T>(PARAMNAME parameterName, T? value)
            where T : struct
        {
            if (!value.HasValue) _SchemaThrow(parameterName, "must be defined.");
            return this;
        }

        public OUTTYPE IsUndefined<T>(PARAMNAME parameterName, T value)
            where T : class
        {
            if (value != null) _SchemaThrow(parameterName, "must NOT be defined.");
            return this;
        }

        public OUTTYPE IsUndefined<T>(PARAMNAME parameterName, T? value)
            where T : struct
        {
            if (value.HasValue) _SchemaThrow(parameterName, "must NOT be defined.");
            return this;
        }

        public OUTTYPE AreSameReference<TRef>(PARAMNAME parameterName, TRef value, TRef expected)
            where TRef : class
        {
            if (!Object.ReferenceEquals(value, expected)) _SchemaThrow(parameterName, $"{value} and {expected} must be the same.");
            return this;
        }

        public OUTTYPE AreEqual<TValue>(PARAMNAME parameterName, TValue value, TValue expected)
                where TValue : IEquatable<TValue>
        {
            if (!value.Equals(expected)) _SchemaThrow(parameterName, $"{value} must be equal to {expected}.");
            return this;
        }

        public OUTTYPE IsLess<TValue>(PARAMNAME parameterName, TValue value, TValue max)
                where TValue : IComparable<TValue>
        {
            if (value.CompareTo(max) >= 0) _SchemaThrow(parameterName, $"{value} must be less than {max}.");
            return this;
        }

        public OUTTYPE IsLessOrEqual<TValue>(PARAMNAME parameterName, TValue value, TValue max)
                where TValue : IComparable<TValue>
        {
            if (value.CompareTo(max) > 0) _SchemaThrow(parameterName, $"{value} must be less or equal to {max}.");
            return this;
        }

        public OUTTYPE IsGreater<TValue>(PARAMNAME parameterName, TValue value, TValue min)
                where TValue : IComparable<TValue>
        {
            if (value.CompareTo(min) <= 0) _SchemaThrow(parameterName, $"{value} must be greater than {min}.");
            return this;
        }

        public OUTTYPE IsDefaultOrWithin<TValue>(PARAMNAME parameterName, TValue? value, TValue minInclusive, TValue maxInclusive)
                where TValue : unmanaged, IComparable<TValue>
        {
            if (!value.HasValue) return this;
            if (value.Value.CompareTo(minInclusive) < 0) _SchemaThrow(parameterName, $"{value} must be greater or equal to {minInclusive}.");
            if (value.Value.CompareTo(maxInclusive) > 0) _SchemaThrow(parameterName, $"{value} must be less or equal to {maxInclusive}.");
            return this;
        }

        public OUTTYPE IsGreaterOrEqual<TValue>(PARAMNAME parameterName, TValue value, TValue min)
                where TValue : IComparable<TValue>
        {
            if (value.CompareTo(min) < 0) _SchemaThrow(parameterName, $"{value} must be greater or equal to {min}.");
            return this;
        }

        public OUTTYPE IsMultipleOf(PARAMNAME parameterName, int value, int multiple)
        {
            if ((value % multiple) != 0) _SchemaThrow(parameterName, $"Value {value} is not a multiple of {multiple}.");
            return this;
        }

        public OUTTYPE NonNegative(PARAMNAME parameterName, int? value)
        {
            if ((value ?? 0) < 0) _SchemaThrow(parameterName, "must be a non-negative integer.");
            return this;
        }

        public OUTTYPE IsNullOrValidURI(PARAMNAME parameterName, string gltfURI, params string[] validHeaders)
        {
            if (gltfURI == null) return this;
            return IsValidURI(parameterName, gltfURI, validHeaders);
        }

        public OUTTYPE IsValidURI(PARAMNAME parameterName, string gltfURI, params string[] validHeaders)
        {
            try { Guard.IsValidURI(parameterName, gltfURI, validHeaders); return this; }
            catch (ArgumentException ex) { _SchemaThrow(parameterName, ex.Message); }
            return this;
        }

        public OUTTYPE IsJsonSerializable(PARAMNAME parameterName, Object value)
        {
            if (!IO.JsonUtils.IsJsonSerializable(value)) _SchemaThrow(parameterName, "cannot be serialized to Json");
            return this;
        }

        #endregion

        #region link

        [System.Diagnostics.DebuggerStepThrough]
        internal void _LinkThrow(PARAMNAME pname, string msg) { throw new LinkException(_Current, $"{pname.ToString()}: {msg}"); }

        public OUTTYPE EnumsAreEqual<TValue>(PARAMNAME parameterName, TValue value, TValue expected)
                where TValue : Enum
        {
            if (!value.Equals(expected)) _LinkThrow(parameterName, $"{value} must be equal to {expected}.");
            return this;
        }

        public OUTTYPE IsNullOrIndex<T>(PARAMNAME parameterName, int? index, IReadOnlyList<T> array)
        {
            return IsNullOrInRange(parameterName, index, 1, array);
        }

        public OUTTYPE IsNullOrInRange<T>(PARAMNAME parameterName, int? offset, int length, IReadOnlyList<T> array)
        {
            if (!offset.HasValue) return this;

            this.NonNegative($"{parameterName}.offset", offset.Value);
            this.IsGreater($"{parameterName}.length", length, 0);

            if (array == null)
            {
                _LinkThrow(parameterName, $".{offset} exceeds the number of available items (null).");
                return this;
            }

            if (offset > array.Count - length)
            {
                if (length == 1) _LinkThrow(parameterName, $".{offset} exceeds the number of available items ({array.Count}).");
                else _LinkThrow(parameterName, $".{offset}+{length} exceeds the number of available items ({array.Count}).");
            }

            return this;
        }

        public OUTTYPE IsAnyOf<T>(PARAMNAME parameterName, T value, params T[] values)
        {
            if (!values.Contains(value)) _LinkThrow(parameterName, $"value {value} is invalid.");

            return this;
        }

        public OUTTYPE IsAnyOf(PARAMNAME parameterName, Memory.AttributeFormat value, params Memory.AttributeFormat[] values)
        {
            if (!values.Contains(value)) _LinkThrow(parameterName, $"value {value} is invalid.");

            return this;
        }

        public OUTTYPE IsSetCollection<T>(PARAMNAME parameterName, IEnumerable<T> collection)
            where T : class
        {
            int idx = 0;

            if (collection == null)
            {
                _LinkThrow(parameterName, "must not be null.");
                return this;
            }

            var uniqueInstances = new HashSet<T>();

            foreach (var v in collection)
            {
                if (v == null) _LinkThrow((parameterName, idx), "Is NULL.");

                if (uniqueInstances.Contains(v)) _LinkThrow((parameterName, idx), "Is duplicated.");

                uniqueInstances.Add(v);

                ++idx;
            }

            return this;
        }

        #endregion

        #region data

        [System.Diagnostics.DebuggerStepThrough]
        private void _DataThrow(PARAMNAME pname, string msg) { throw new DataException(_Current, $"{pname}: {msg}"); }

        public OUTTYPE IsInRange<T>(PARAMNAME pname, T value, T minInclusive, T maxInclusive)
            where T : IComparable<T>
        {
            if (value.CompareTo(minInclusive) == -1) _DataThrow(pname, $"is below minimum {minInclusive} value: {value}");
            if (value.CompareTo(maxInclusive) == +1) _DataThrow(pname, $"is above maximum {maxInclusive} value: {value}");
            return this;
        }

        public OUTTYPE IsNullOrMatrix(PARAMNAME pname, System.Numerics.Matrix4x4? matrix, bool mustDecompose = true, bool mustInvert = true)
        {
            if (!matrix.HasValue) return this;
            return IsMatrix(pname, matrix.Value, mustDecompose, mustInvert);
        }

        public OUTTYPE IsPosition(PARAMNAME pname, in System.Numerics.Vector3 position)
        {
            if (!position._IsFinite()) _DataThrow(pname, "Invalid Position");
            return this;
        }

        public OUTTYPE IsNormal(PARAMNAME pname, in System.Numerics.Vector3 normal)
        {
            if (!normal.IsNormalized()) _DataThrow(pname, "Invalid Normal");
            return this;
        }

        public OUTTYPE IsRotation(PARAMNAME pname, in System.Numerics.Quaternion rotation)
        {
            if (!rotation.IsNormalized()) _DataThrow(pname, "Invalid Rotation");
            return this;
        }

        public OUTTYPE IsMatrix(PARAMNAME pname, in System.Numerics.Matrix4x4 matrix, bool mustDecompose = true, bool mustInvert = true)
        {
            if (!matrix.IsValid(mustDecompose, mustInvert)) _DataThrow(pname, "Invalid Matrix");
            return this;
        }

        public OUTTYPE ArePositions(PARAMNAME pname, IList<System.Numerics.Vector3> positions)
        {
            Guard.NotNull(positions, nameof(positions));

            for (int i = 0; i < positions.Count; ++i)
            {
                IsPosition((pname, i), positions[i]);
            }

            return this;
        }

        public OUTTYPE AreNormals(PARAMNAME pname, IList<System.Numerics.Vector3> normals)
        {
            Guard.NotNull(normals, nameof(normals));

            for (int i = 0; i < normals.Count; ++i)
            {
                IsNormal((pname, i), normals[i]);
            }

            return this;
        }

        public OUTTYPE AreTangents(PARAMNAME pname, IList<System.Numerics.Vector4> tangents)
        {
            Guard.NotNull(tangents, nameof(tangents));

            for (int i = 0; i < tangents.Count; ++i)
            {
                if (!tangents[i].IsValidTangent()) _DataThrow((pname, i), "Invalid Tangent");
            }

            return this;
        }

        public OUTTYPE AreRotations(PARAMNAME pname, IList<System.Numerics.Quaternion> rotations)
        {
            Guard.NotNull(rotations, nameof(rotations));

            for (int i = 0; i < rotations.Count; ++i)
            {
                if (!rotations[i].IsNormalized()) _DataThrow((pname, i), "Invalid Rotation");
            }

            return this;
        }

        public OUTTYPE AreJoints(PARAMNAME pname, IList<System.Numerics.Vector4> joints, int skinsMaxJointCount)
        {
            Guard.NotNull(joints, nameof(joints));

            for (int i = 0; i < joints.Count; ++i)
            {
                var jjjj = joints[i];

                if (!jjjj._IsFinite()) _DataThrow((pname, i), "Is not finite");

                if (jjjj.X < 0 || jjjj.X >= skinsMaxJointCount) _DataThrow((pname, i), "Is out of bounds");
                if (jjjj.Y < 0 || jjjj.Y >= skinsMaxJointCount) _DataThrow((pname, i), "Is out of bounds");
                if (jjjj.Z < 0 || jjjj.Z >= skinsMaxJointCount) _DataThrow((pname, i), "Is out of bounds");
                if (jjjj.W < 0 || jjjj.W >= skinsMaxJointCount) _DataThrow((pname, i), "Is out of bounds");
            }

            return this;
        }

        public OUTTYPE That(Action action)
        {
            Guard.NotNull(action, nameof(action));

            try
            {
                action.Invoke();
            }
            catch (ArgumentException ex)
            {
                _DataThrow(ex.ParamName, ex.Message);
            }

            return this;
        }

        #endregion
    }
}
