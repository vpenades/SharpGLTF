using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

using PARAMETER = System.Object;

namespace SharpGLTF.Schema2
{
    /// <summary>
    /// Represents a material sub-channel, which usually contains a texture.<br/>
    /// Use <see cref="Material.Channels"/> and <see cref="Material.FindChannel(string)"/> to access it.
    /// </summary>
    /// <remarks>
    /// This structure is not part of the gltf schema,
    /// but wraps several components of the material
    /// to have an homogeneous and easy to use API.
    /// </remarks>
    [System.Diagnostics.DebuggerDisplay("Channel {_Key}")]
    public readonly struct MaterialChannel
    {
        #region lifecycle

        internal MaterialChannel(Material m, String key, Func<Boolean, TextureInfo> texInfo, params MaterialParameter[] parameters)
        {
            Guard.NotNull(m, nameof(m));
            Guard.NotNullOrEmpty(key, nameof(key));

            Guard.NotNull(texInfo, nameof(texInfo));

            _Key = key;
            _Material = m;

            _TextureInfo = texInfo;
            _Parameters = parameters;
        }

        #endregion

        #region data

        [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
        private readonly String _Key;

        [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
        private readonly Material _Material;

        [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
        private readonly Func<Boolean, TextureInfo> _TextureInfo;

        [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.RootHidden)]
        private readonly IReadOnlyList<MaterialParameter> _Parameters;

        public override int GetHashCode()
        {
            if (_Key == null) return 0;

            return _Key.GetHashCode() ^ _Material.GetHashCode();
        }

        #endregion

        #region properties

        public Material LogicalParent => _Material;

        public String Key => _Key;

        public Boolean HasDefaultContent => _CheckHasDefaultContent();

        /// <summary>
        /// Gets or sets the <see cref="Vector4"/> parameter of this channel.
        /// The meaning of the <see cref="Vector4.X"/>, <see cref="Vector4.Y"/>. <see cref="Vector4.Z"/> and <see cref="Vector4.W"/>
        /// depend on the type of channel.
        /// </summary>
        [Obsolete("Use Parameters[]")]
        [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
        public Vector4 Parameter
        {
            get => MaterialParameter.Combine(_Parameters);
            set => MaterialParameter.Apply(_Parameters, value);
        }

        public IReadOnlyList<MaterialParameter> Parameters => _Parameters;

        /// <summary>
        /// Gets the <see cref="Texture"/> instance used by this Material, or null.
        /// </summary>
        public Texture Texture => _GetTexture();

        /// <summary>
        /// Gets the index of texture's TEXCOORD_[index] attribute used for texture coordinate mapping.
        /// </summary>
        public int TextureCoordinate => _TextureInfo?.Invoke(false)?.TextureCoordinate ?? 0;

        public TextureTransform TextureTransform => _TextureInfo?.Invoke(false)?.Transform;

        public TextureSampler TextureSampler => Texture?.Sampler;

        #endregion

        #region API

        private Texture _GetTexture()
        {
            var texInfo = _TextureInfo?.Invoke(false);
            if (texInfo == null) return null;

            return _Material.LogicalParent.LogicalTextures[texInfo._LogicalTextureIndex];
        }

        public Texture SetTexture(
            int texCoord,
            Image primaryImg,
            Image fallbackImg = null,
            TextureWrapMode ws = TextureWrapMode.REPEAT,
            TextureWrapMode wt = TextureWrapMode.REPEAT,
            TextureMipMapFilter min = TextureMipMapFilter.DEFAULT,
            TextureInterpolationFilter mag = TextureInterpolationFilter.DEFAULT)
        {
            if (primaryImg == null) return null; // in theory, we should completely remove the TextureInfo

            Guard.NotNull(_Material, nameof(_Material));

            if (_TextureInfo == null) throw new InvalidOperationException();

            var sampler = _Material.LogicalParent.UseTextureSampler(ws, wt, min, mag);
            var texture = _Material.LogicalParent.UseTexture(primaryImg, fallbackImg, sampler);

            SetTexture(texCoord, texture);

            return texture;
        }

        public void SetTexture(int texSet, Texture tex)
        {
            Guard.NotNull(tex, nameof(tex));
            Guard.MustShareLogicalParent(_Material, tex, nameof(tex));

            if (_TextureInfo == null) throw new InvalidOperationException();

            var texInfo = _TextureInfo(true);

            texInfo.TextureCoordinate = texSet;
            texInfo._LogicalTextureIndex = tex.LogicalIndex;
        }

        private Texture TryGetTexture()
        {
            var texInfo = _TextureInfo?.Invoke(false);
            if (texInfo == null) return null;
            if (texInfo._LogicalTextureIndex < 0) return null;
            return _Material.LogicalParent.LogicalTextures[texInfo._LogicalTextureIndex];
        }

        public void SetTransform(Vector2 offset, Vector2 scale, float rotation = 0, int? texCoordOverride = null)
        {
            if (_TextureInfo == null) throw new InvalidOperationException();

            var texInfo = _TextureInfo(true);

            texInfo.SetTransform(offset, scale, rotation, texCoordOverride);
        }

        private bool _CheckHasDefaultContent()
        {
            if (this.Texture != null) return false;
            if (!this._Parameters.All(item => item.IsDefault)) return false;
            return true;
        }

        #endregion
    }

    [System.Diagnostics.DebuggerDisplay("[{Name}, {Value}]")]
    public readonly struct MaterialParameter
    {
        #region constants

        internal enum Key
        {
            RGB,
            RGBA,

            NormalScale,
            OcclusionStrength,

            MetallicFactor,
            RoughnessFactor,
            SpecularFactor,
            GlossinessFactor,
            ClearCoatFactor,
            ThicknessFactor,
            TransmissionFactor,
            AttenuationDistance,
        }

        #endregion

        #region constructors

        internal MaterialParameter(Key key, double defval, Func<double?> getter, Action<double?> setter, double min = double.MinValue, double max = double.MaxValue)
        {
            _Key = key;
            _ValueDefault = defval;
            _ValueGetter = () => (PARAMETER)(float)getter().AsValue(defval);
            _ValueSetter = value => { double v = (float)value; setter(v.AsNullable(defval, min, max)); };
        }

        internal MaterialParameter(Key key, float defval, Func<float> getter, Action<float> setter)
        {
            _Key = key;
            _ValueDefault = defval;
            _ValueGetter = () => (PARAMETER)getter();
            _ValueSetter = value => setter((float)value);
        }

        internal MaterialParameter(Key key, Vector2 defval, Func<Vector2> getter, Action<Vector2> setter)
        {
            _Key = key;
            _ValueDefault = defval;
            _ValueGetter = () => (PARAMETER)getter();
            _ValueSetter = value => setter((Vector2)value);
        }

        internal MaterialParameter(Key key, Vector3 defval, Func<Vector3> getter, Action<Vector3> setter)
        {
            _Key = key;
            _ValueDefault = defval;
            _ValueGetter = () => (PARAMETER)getter();
            _ValueSetter = value => setter((Vector3)value);
        }

        internal MaterialParameter(Key key, Vector4 defval, Func<Vector4> getter, Action<Vector4> setter)
        {
            _Key = key;
            _ValueDefault = defval;
            _ValueGetter = () => (PARAMETER)getter();
            _ValueSetter = value => setter((Vector4)value);
        }

        #endregion

        #region data

        private readonly Key _Key;
        private readonly Object _ValueDefault;
        private readonly Func<PARAMETER> _ValueGetter;
        private readonly Action<PARAMETER> _ValueSetter;

        #endregion

        #region properties

        public string Name => _Key.ToString();

        public bool IsDefault => Object.Equals(Value, _ValueDefault);

        public PARAMETER Value
        {
            get => _ValueGetter();
            set => _ValueSetter(value);
        }

        #endregion

        #region helpers
        internal static Vector4 Combine(IReadOnlyList<MaterialParameter> parameters)
        {
            Span<float> tmp = stackalloc float[4];
            int idx = 0;

            foreach (var p in parameters)
            {
                if (p.Value is Single v1) { tmp[idx++] = v1; }
                if (p.Value is Vector2 v2) { tmp[idx++] = v2.X; tmp[idx++] = v2.Y; }
                if (p.Value is Vector3 v3) { tmp[idx++] = v3.X; tmp[idx++] = v3.Y; tmp[idx++] = v3.Z; }
                if (p.Value is Vector4 v4) { tmp[idx++] = v4.X; tmp[idx++] = v4.Y; tmp[idx++] = v4.Z; tmp[idx++] = v4.W; }
            }

            return new Vector4(tmp[0], tmp[1], tmp[2], tmp[3]);
        }
        internal static void Apply(IReadOnlyList<MaterialParameter> parameters, Vector4 value)
        {
            Span<float> tmp = stackalloc float[4];
            tmp[0] = value.X;
            tmp[1] = value.Y;
            tmp[2] = value.Z;
            tmp[3] = value.W;

            int idx = 0;

            foreach (var p in parameters)
            {
                if (p.Value is Single) { p.Value = tmp[idx++]; }
                if (p.Value is Vector2) { p.Value = new Vector2(tmp[idx + 0], tmp[idx + 1]); idx += 2; }
                if (p.Value is Vector3) { p.Value = new Vector3(tmp[idx + 0], tmp[idx + 1], tmp[idx + 2]); idx += 3; }
                if (p.Value is Vector4) { p.Value = new Vector4(tmp[idx + 0], tmp[idx + 1], tmp[idx + 2], tmp[idx + 3]); idx += 4; }
            }
        }

        #endregion
    }
}
