using System;
using System.Collections.Generic;
using System.Text;
using System.Numerics;

namespace SharpGLTF.Schema2
{
    /// <summary>
    /// A parameter accessor interface
    /// </summary>
    /// <remarks>
    /// Implemented by <see cref="_MaterialParameter{}"/>
    /// </remarks>
    public interface IMaterialParameter
    {
        /// <summary>
        /// The name of the parameter.
        /// </summary>
        /// <remarks>
        /// The value must be one of the internal enum <see cref="_MaterialParameterKey"/>
        /// </remarks>
        string Name { get; }

        /// <summary>
        /// Gets a value indicating whether the current value is the default value.
        /// </summary>
        bool IsDefault { get; }

        /// <summary>
        /// Gets the type of <see cref="Value"/><br/>Which can be <see cref="float"/> <see cref="Vector3"/> and <see cref="Vector4"/>
        /// </summary>
        Type ValueType { get; }

        /// <summary>
        /// Gets or sets the value of this parameter.<br/>
        /// Valid types are <see cref="float"/> <see cref="Vector3"/> and <see cref="Vector4"/>
        /// </summary>
        Object Value { get; set; }
    }

    internal enum _MaterialParameterKey
    {
        Unknown = 0,

        RGB,
        RGBA,

        Minimum, Maximum,

        NormalScale,

        OcclusionStrength,
        EmissiveStrength,

        IndexOfRefraction,

        MetallicFactor,
        RoughnessFactor,
        SpecularFactor,
        GlossinessFactor,
        ClearCoatFactor,
        ThicknessFactor,
        TransmissionFactor,
        IridescenceFactor,
        AttenuationDistance,
        DiffuseTransmissionFactor,

        AnisotropyStrength,

        AnisotropyRotation,
    }

    [System.Diagnostics.DebuggerDisplay("{_Key} = {Value}")]
    readonly struct _MaterialParameter<T> : IMaterialParameter
        where T : unmanaged, IEquatable<T>
    {
        #region constructors

        internal _MaterialParameter(_MaterialParameterKey key, T defval, Func<T> getter, Action<T> setter)
        {
            _Key = key;
            _ValueDefault = defval;
            _ValueGetter = () => getter();
            _ValueSetter = value => setter(value);
        }

        #endregion

        #region data

        private readonly _MaterialParameterKey _Key;
        private readonly T _ValueDefault;
        private readonly Func<T> _ValueGetter;
        private readonly Action<T> _ValueSetter;

        #endregion

        #region properties

        public string Name => _Key.ToString();

        public bool IsDefault => Value.Equals(_ValueDefault);

        public Type ValueType => _ValueDefault.GetType();

        public T Value
        {
            get => _ValueGetter();
            set => _ValueSetter(value);
        }

        object IMaterialParameter.Value
        {
            get => _ValueGetter();
            set => _ValueSetter((T)value);
        }

        #endregion

        #region helpers
        internal static Vector4 Combine(IReadOnlyList<IMaterialParameter> parameters)
        {
            Span<float> tmp = stackalloc float[4];
            int idx = 0;

            foreach (var p in parameters)
            {
                if (p is _MaterialParameter<Single> v1) { tmp[idx++] = v1.Value; }
                if (p is _MaterialParameter<Vector2> v2) { tmp[idx++] = v2.Value.X; tmp[idx++] = v2.Value.Y; }
                if (p is _MaterialParameter<Vector3> v3) { tmp[idx++] = v3.Value.X; tmp[idx++] = v3.Value.Y; tmp[idx++] = v3.Value.Z; }
                if (p is _MaterialParameter<Vector4> v4) { tmp[idx++] = v4.Value.X; tmp[idx++] = v4.Value.Y; tmp[idx++] = v4.Value.Z; tmp[idx++] = v4.Value.W; }
            }

            return new Vector4(tmp[0], tmp[1], tmp[2], tmp[3]);
        }

        internal static void Apply(IReadOnlyList<IMaterialParameter> parameters, Vector4 value)
        {
            Span<float> tmp = stackalloc float[4];
            tmp[0] = value.X;
            tmp[1] = value.Y;
            tmp[2] = value.Z;
            tmp[3] = value.W;

            int idx = 0;

            foreach (var p in parameters)
            {
                if (p is _MaterialParameter<Single> v1) { v1.Value = tmp[idx++]; }
                if (p is _MaterialParameter<Vector2> v2) { v2.Value = new Vector2(tmp[idx + 0], tmp[idx + 1]); idx += 2; }
                if (p is _MaterialParameter<Vector3> v3) { v3.Value = new Vector3(tmp[idx + 0], tmp[idx + 1], tmp[idx + 2]); idx += 3; }
                if (p is _MaterialParameter<Vector4> v4) { v4.Value = new Vector4(tmp[idx + 0], tmp[idx + 1], tmp[idx + 2], tmp[idx + 3]); idx += 4; }
            }
        }

        #endregion
    }
}
