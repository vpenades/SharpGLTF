using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;

namespace SharpGLTF.Materials
{
    /// <summary>
    /// Represents a material channel at <see cref="MaterialBuilder"/>.
    /// </summary>
    [System.Diagnostics.DebuggerDisplay("{_GetDebuggerDisplay(),nq}")]
    public partial class ChannelBuilder
    {
        #region debug

        private string _GetDebuggerDisplay()
        {
            var txt = Key.ToString();

            var tex = GetValidTexture();
            if (tex?.PrimaryImage != null)
            {
                // if (hasParam) txt += " ×";
                txt += $" {tex.PrimaryImage.Content.ToDebuggerDisplay()}";
            }

            return txt;
        }

        #endregion

        #region lifecycle

        internal ChannelBuilder(MaterialBuilder parent, KnownChannel key)
        {
            Guard.NotNull(parent, nameof(parent));

            _Parent = parent;
            _Key = key;

            _Parameters = MaterialValue.CreateDefaultProperties(key);
        }

        #endregion

        #region data

        [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
        private readonly MaterialBuilder _Parent;

        [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
        private readonly KnownChannel _Key;

        [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
        private readonly MaterialValue.Collection _Parameters;

        public TextureBuilder Texture { get; private set; }

        public static bool AreEqualByContent(ChannelBuilder x, ChannelBuilder y)
        {
            if ((x, y).AreSameReference(out bool areTheSame)) return areTheSame;

            if (x._Key != y._Key) return false;

            if (!MaterialValue.Collection.AreEqual(x._Parameters, y._Parameters)) return false;

            if (!TextureBuilder.AreEqualByContent(x.Texture, y.Texture)) return false;

            return true;
        }

        public static int GetContentHashCode(ChannelBuilder x)
        {
            if (x == null) return 0;

            var h = x._Key.GetHashCode();

            h ^= x._Parameters.GetHashCode();

            h ^= TextureBuilder.GetContentHashCode(x.Texture);

            return h;
        }

        #endregion

        #region properties

        /// <summary>
        /// Gets the <see cref="ChannelBuilder"/> name. It must be a name of <see cref="KnownChannel"/>.
        /// </summary>
        public KnownChannel Key => _Key;

        /// <summary>
        /// Gets or sets the <see cref="ChannelBuilder"/> parameter.
        /// </summary>
        /// <remarks>
        /// Its meaning differs depending on the value of <see cref="Key"/>.
        /// </remarks>
        [Obsolete("Use .Parameters[KnownProperty] or .Parameters.CombinedVector")]
        [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
        public Vector4 Parameter
        {
            get => _Parameters.CombinedVector;
            set => _Parameters.CombinedVector = value;
        }

        /// <summary>
        /// Gets the collection of parameters of this channel
        /// </summary>
        public MaterialValue.Collection Parameters => _Parameters;

        /// <summary>
        /// Gets an equality comparer that deep compares the internal fields and collections.
        /// </summary>
        [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
        public static IEqualityComparer<ChannelBuilder> ContentComparer => _ContentComparer.Default;

        #endregion

        #region API
        public TextureBuilder GetValidTexture()
        {
            if (Texture == null) return null;
            if (Texture.PrimaryImage == null) return null;
            return Texture;
        }

        public TextureBuilder UseTexture()
        {
            if (Texture == null) Texture = new TextureBuilder(this);
            return Texture;
        }

        public void RemoveTexture() { Texture = null; }

        internal void CopyTo(ChannelBuilder other)
        {
            this._Parameters.CopyTo(other._Parameters);

            if (this.Texture == null)
            {
                RemoveTexture();
            }
            else
            {
                this.Texture.CopyTo(other.UseTexture());
            }
        }

        #endregion

        #region Nested types

        sealed class _ContentComparer : IEqualityComparer<ChannelBuilder>
        {
            public static readonly _ContentComparer Default = new _ContentComparer();

            public bool Equals(ChannelBuilder x, ChannelBuilder y)
            {
                return AreEqualByContent(x, y);
            }

            public int GetHashCode(ChannelBuilder obj)
            {
                return GetContentHashCode(obj);
            }
        }

        #endregion
    }
}
