using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

using ENCODING = SharpGLTF.Schema2.EncodingType;

namespace SharpGLTF.Geometry.VertexTypes
{
    /// <summary>
    /// Represents the interface that must be implemented by a material vertex fragment.
    /// </summary>
    /// <remarks>
    /// Implemented by:
    /// <list type="table">
    /// <item><see cref="VertexEmpty"/></item>
    /// <item><see cref="VertexColor1"/></item>
    /// <item><see cref="VertexColor2"/></item>
    /// <item><see cref="VertexTexture1"/></item>
    /// <item><see cref="VertexTexture2"/></item>
    /// <item><see cref="VertexColor1Texture1"/></item>
    /// <item><see cref="VertexColor1Texture2"/></item>
    /// <item><see cref="VertexColor2Texture1"/></item>
    /// <item><see cref="VertexColor2Texture2"/></item>
    /// <item>And also by other custom vertex material fragment types.</item>
    /// </list>
    /// </remarks>
    public interface IVertexMaterial
    {
        /// <summary>
        /// Gets the number of color attributes available in this vertex
        /// </summary>
        int MaxColors { get; }

        /// <summary>
        /// Gets the number of texture coordinate attributes available in this vertex
        /// </summary>
        int MaxTextCoords { get; }

        /// <summary>
        /// Gets a color attribute.
        /// </summary>
        /// <param name="index">An index from 0 to <see cref="MaxColors"/>.</param>
        /// <returns>A <see cref="Vector4"/> value in the range of 0 to 1</returns>
        Vector4 GetColor(int index);

        /// <summary>
        /// Gets a UV texture coordinate attribute.
        /// </summary>
        /// <param name="index">An index from 0 to <see cref="MaxTextCoords"/>.</param>
        /// <returns>A <see cref="Vector2"/> UV texture coordinate.</returns>
        Vector2 GetTexCoord(int index);

        /// <summary>
        /// Sets a color attribute.
        /// <para><b>⚠️ USE ONLY ON UNBOXED VALUES ⚠️</b></para>
        /// </summary>
        /// <param name="setIndex">An index from 0 to <see cref="MaxColors"/>.</param>
        /// <param name="color">A <see cref="Vector4"/> value in the range of 0 to 1</param>
        void SetColor(int setIndex, Vector4 color);

        /// <summary>
        /// Sets a UV texture coordinate attribute.
        /// <para><b>⚠️ USE ONLY ON UNBOXED VALUES ⚠️</b></para>
        /// </summary>
        /// <param name="setIndex">An index from 0 to <see cref="MaxTextCoords"/>.</param>
        /// <param name="coord">A <see cref="Vector2"/> UV texture coordinate.</param>
        void SetTexCoord(int setIndex, Vector2 coord);

        /// <summary>
        /// calculates the difference between this vertex and <paramref name="baseValue"/>
        /// </summary>
        /// <param name="baseValue">The other vertex.</param>
        /// <returns>The <see cref="VertexMaterialDelta"/> value to subtract.</returns>
        VertexMaterialDelta Subtract(IVertexMaterial baseValue);

        /// <summary>
        /// Adds a vertex delta to this value.
        /// <para><b>⚠️ USE ONLY ON UNBOXED VALUES ⚠️</b></para>
        /// </summary>
        /// <param name="delta">The <see cref="VertexMaterialDelta"/> value to add.</param>
        void Add(in VertexMaterialDelta delta);
    }

    /// <summary>
    /// Defines a Vertex attribute with a material Color.
    /// </summary>
    [System.Diagnostics.DebuggerDisplay("{_GetDebuggerDisplay(),nq}")]
    public struct VertexColor1 : IVertexMaterial, IEquatable<VertexColor1>
    {
        #region debug

        private string _GetDebuggerDisplay() => VertexUtils._GetDebuggerDisplay(this);

        #endregion

        #region constructors

        public VertexColor1(Vector4 color)
        {
            Color = color;
        }

        public VertexColor1(IVertexMaterial src)
        {
            Guard.NotNull(src, nameof(src));

            this.Color = src.MaxColors > 0 ? src.GetColor(0) : Vector4.One;
        }

        public static implicit operator VertexColor1(Vector4 color)
        {
            return new VertexColor1(color);
        }

        #endregion

        #region data

        [VertexAttribute("COLOR_0", ENCODING.UNSIGNED_BYTE, true)]
        public Vector4 Color;

        /// <inheritdoc/>
        public int MaxColors => 1;

        /// <inheritdoc/>
        public int MaxTextCoords => 0;

        public override bool Equals(object obj) { return obj is VertexColor1 other && AreEqual(this, other); }
        public bool Equals(VertexColor1 other) { return AreEqual(this, other); }
        public static bool operator ==(in VertexColor1 a, in VertexColor1 b) { return AreEqual(a, b); }
        public static bool operator !=(in VertexColor1 a, in VertexColor1 b) { return !AreEqual(a, b); }

        public static bool AreEqual(in VertexColor1 a, in VertexColor1 b)
        {
            return a.Color == b.Color;
        }

        public override int GetHashCode() { return Color.GetHashCode(); }

        #endregion

        #region API

        /// <inheritdoc/>
        public VertexMaterialDelta Subtract(IVertexMaterial baseValue)
        {
            return new VertexMaterialDelta((VertexColor1)baseValue, this);
        }

        /// <inheritdoc/>
        public void Add(in VertexMaterialDelta delta)
        {
            this.Color += delta.Color0Delta;
        }

        void IVertexMaterial.SetColor(int setIndex, Vector4 color) { if (setIndex == 0) this.Color = color; }

        void IVertexMaterial.SetTexCoord(int setIndex, Vector2 coord) { }

        /// <inheritdoc/>
        public Vector4 GetColor(int index)
        {
            if (index != 0) throw new ArgumentOutOfRangeException(nameof(index));
            return Color;
        }

        /// <inheritdoc/>
        public Vector2 GetTexCoord(int index)
        {
            throw new NotSupportedException();
        }

        #endregion
    }

    /// <summary>
    /// Defines a Vertex attribute with a two material Colors.
    /// </summary>
    [System.Diagnostics.DebuggerDisplay("{_GetDebuggerDisplay(),nq}")]
    public struct VertexColor2 : IVertexMaterial, IEquatable<VertexColor2>
    {
        #region debug

        private string _GetDebuggerDisplay() => VertexUtils._GetDebuggerDisplay(this);

        #endregion

        #region constructors

        public VertexColor2(Vector4 color0, Vector4 color1)
        {
            Color0 = color0;
            Color1 = color1;
        }

        public VertexColor2(IVertexMaterial src)
        {
            Guard.NotNull(src, nameof(src));

            this.Color0 = src.MaxColors > 0 ? src.GetColor(0) : Vector4.One;
            this.Color1 = src.MaxColors > 1 ? src.GetColor(1) : Vector4.One;
        }

        public static implicit operator VertexColor2((Vector4 Color0, Vector4 Color1) tuple)
        {
            return new VertexColor2(tuple.Color0, tuple.Color1);
        }

        #endregion

        #region data

        [VertexAttribute("COLOR_0", ENCODING.UNSIGNED_BYTE, true)]
        public Vector4 Color0;

        [VertexAttribute("COLOR_1", ENCODING.UNSIGNED_BYTE, true)]
        public Vector4 Color1;

        /// <inheritdoc/>
        public int MaxColors => 2;

        /// <inheritdoc/>
        public int MaxTextCoords => 0;

        public override bool Equals(object obj) { return obj is VertexColor2 other && AreEqual(this, other); }
        public bool Equals(VertexColor2 other) { return AreEqual(this, other); }
        public static bool operator ==(in VertexColor2 a, in VertexColor2 b) { return AreEqual(a, b); }
        public static bool operator !=(in VertexColor2 a, in VertexColor2 b) { return !AreEqual(a, b); }

        public static bool AreEqual(in VertexColor2 a, in VertexColor2 b)
        {
            return a.Color0 == b.Color0 && a.Color1 == b.Color1;
        }

        public override int GetHashCode() { return Color0.GetHashCode() ^ Color1.GetHashCode(); }

        #endregion

        #region API

        /// <inheritdoc/>
        public VertexMaterialDelta Subtract(IVertexMaterial baseValue)
        {
            return new VertexMaterialDelta((VertexColor2)baseValue, this);
        }

        /// <inheritdoc/>
        public void Add(in VertexMaterialDelta delta)
        {
            this.Color0 += delta.Color0Delta;
            this.Color1 += delta.Color1Delta;
        }

        void IVertexMaterial.SetColor(int setIndex, Vector4 color)
        {
            if (setIndex == 0) this.Color0 = color;
            if (setIndex == 1) this.Color1 = color;
        }

        void IVertexMaterial.SetTexCoord(int setIndex, Vector2 coord) { }

        /// <inheritdoc/>
        public Vector4 GetColor(int index)
        {
            if (index == 0) return Color0;
            if (index == 1) return Color1;
            throw new ArgumentOutOfRangeException(nameof(index));
        }

        /// <inheritdoc/>
        public Vector2 GetTexCoord(int index) { throw new NotSupportedException(); }

        #endregion
    }

    /// <summary>
    /// Defines a Vertex attribute with a Texture Coordinate.
    /// </summary>
    [System.Diagnostics.DebuggerDisplay("{_GetDebuggerDisplay(),nq}")]
    public struct VertexTexture1 : IVertexMaterial, IEquatable<VertexTexture1>
    {
        #region debug

        private string _GetDebuggerDisplay() => VertexUtils._GetDebuggerDisplay(this);

        #endregion

        #region constructors

        public VertexTexture1(Vector2 uv)
        {
            TexCoord = uv;
        }

        public VertexTexture1(IVertexMaterial src)
        {
            Guard.NotNull(src, nameof(src));

            this.TexCoord = src.MaxTextCoords > 0 ? src.GetTexCoord(0) : Vector2.Zero;
        }

        public static implicit operator VertexTexture1(Vector2 uv)
        {
            return new VertexTexture1(uv);
        }

        #endregion

        #region data

        [VertexAttribute("TEXCOORD_0")]
        public Vector2 TexCoord;

        /// <inheritdoc/>
        public int MaxColors => 0;

        /// <inheritdoc/>
        public int MaxTextCoords => 1;

        public override bool Equals(object obj) { return obj is VertexTexture1 other && AreEqual(this, other); }
        public bool Equals(VertexTexture1 other) { return AreEqual(this, other); }
        public static bool operator ==(in VertexTexture1 a, in VertexTexture1 b) { return AreEqual(a, b); }
        public static bool operator !=(in VertexTexture1 a, in VertexTexture1 b) { return !AreEqual(a, b); }
        public static bool AreEqual(in VertexTexture1 a, in VertexTexture1 b)
        {
            return a.TexCoord == b.TexCoord;
        }

        public override int GetHashCode() { return TexCoord.GetHashCode(); }

        #endregion

        #region API

        /// <inheritdoc/>
        public VertexMaterialDelta Subtract(IVertexMaterial baseValue)
        {
            return new VertexMaterialDelta((VertexTexture1)baseValue, this);
        }

        /// <inheritdoc/>
        public void Add(in VertexMaterialDelta delta)
        {
            this.TexCoord += delta.TexCoord0Delta;
        }

        void IVertexMaterial.SetColor(int setIndex, Vector4 color) { }

        void IVertexMaterial.SetTexCoord(int setIndex, Vector2 coord) { if (setIndex == 0) this.TexCoord = coord; }

        /// <inheritdoc/>
        public Vector4 GetColor(int index)
        {
            throw new NotSupportedException();
        }

        /// <inheritdoc/>
        public Vector2 GetTexCoord(int index)
        {
            if (index != 0) throw new ArgumentOutOfRangeException(nameof(index));
            return TexCoord;
        }

        #endregion
    }

    /// <summary>
    /// Defines a Vertex attribute with two Texture Coordinates.
    /// </summary>
    [System.Diagnostics.DebuggerDisplay("{_GetDebuggerDisplay(),nq}")]
    public struct VertexTexture2 : IVertexMaterial, IEquatable<VertexTexture2>
    {
        #region debug

        private string _GetDebuggerDisplay() => VertexUtils._GetDebuggerDisplay(this);

        #endregion

        #region constructors

        public VertexTexture2(Vector2 uv0, Vector2 uv1)
        {
            TexCoord0 = uv0;
            TexCoord1 = uv1;
        }

        public VertexTexture2(IVertexMaterial src)
        {
            Guard.NotNull(src, nameof(src));

            this.TexCoord0 = src.MaxTextCoords > 0 ? src.GetTexCoord(0) : Vector2.Zero;
            this.TexCoord1 = src.MaxTextCoords > 1 ? src.GetTexCoord(1) : Vector2.Zero;
        }

        public static implicit operator VertexTexture2((Vector2 Tex0, Vector2 Tex1) tuple)
        {
            return new VertexTexture2(tuple.Tex0, tuple.Tex1);
        }

        #endregion

        #region data

        [VertexAttribute("TEXCOORD_0")]
        public Vector2 TexCoord0;

        [VertexAttribute("TEXCOORD_1")]
        public Vector2 TexCoord1;

        /// <inheritdoc/>
        public int MaxColors => 0;

        /// <inheritdoc/>
        public int MaxTextCoords => 2;

        public override bool Equals(object obj) { return obj is VertexTexture2 other && AreEqual(this, other); }
        public bool Equals(VertexTexture2 other) { return AreEqual(this, other); }
        public static bool operator ==(in VertexTexture2 a, in VertexTexture2 b) { return AreEqual(a, b); }
        public static bool operator !=(in VertexTexture2 a, in VertexTexture2 b) { return !AreEqual(a, b); }
        public static bool AreEqual(in VertexTexture2 a, in VertexTexture2 b)
        {
            return a.TexCoord0 == b.TexCoord0 && a.TexCoord1 == b.TexCoord1;
        }

        public override int GetHashCode() { return TexCoord0.GetHashCode() ^ TexCoord1.GetHashCode(); }

        #endregion

        #region API

        /// <inheritdoc/>
        public VertexMaterialDelta Subtract(IVertexMaterial baseValue)
        {
            return new VertexMaterialDelta((VertexTexture2)baseValue, this);
        }

        /// <inheritdoc/>
        public void Add(in VertexMaterialDelta delta)
        {
            this.TexCoord0 += delta.TexCoord0Delta;
            this.TexCoord1 += delta.TexCoord1Delta;
        }

        void IVertexMaterial.SetColor(int setIndex, Vector4 color) { }

        void IVertexMaterial.SetTexCoord(int setIndex, Vector2 coord)
        {
            if (setIndex == 0) this.TexCoord0 = coord;
            if (setIndex == 1) this.TexCoord1 = coord;
        }

        /// <inheritdoc/>
        public Vector4 GetColor(int index)
        {
            throw new NotSupportedException();
        }

        /// <inheritdoc/>
        public Vector2 GetTexCoord(int index)
        {
            if (index == 0) return TexCoord0;
            if (index == 1) return TexCoord1;
            throw new ArgumentOutOfRangeException(nameof(index));
        }

        #endregion
    }

    /// <summary>
    /// Defines a Vertex attribute with a Color material and a Texture Coordinate.
    /// </summary>
    [System.Diagnostics.DebuggerDisplay("{_GetDebuggerDisplay(),nq}")]
    public struct VertexColor1Texture1 : IVertexMaterial, IEquatable<VertexColor1Texture1>
    {
        #region debug

        private string _GetDebuggerDisplay() => VertexUtils._GetDebuggerDisplay(this);

        #endregion

        #region constructors

        public VertexColor1Texture1(Vector4 color, Vector2 tex)
        {
            Color = color;
            TexCoord = tex;
        }

        public VertexColor1Texture1(IVertexMaterial src)
        {
            Guard.NotNull(src, nameof(src));

            this.Color = src.MaxColors > 0 ? src.GetColor(0) : Vector4.One;
            this.TexCoord = src.MaxTextCoords > 0 ? src.GetTexCoord(0) : Vector2.Zero;
        }

        public static implicit operator VertexColor1Texture1((Vector4 Color, Vector2 Tex) tuple)
        {
            return new VertexColor1Texture1(tuple.Color, tuple.Tex);
        }

        #endregion

        #region data

        [VertexAttribute("COLOR_0", ENCODING.UNSIGNED_BYTE, true)]
        public Vector4 Color;

        [VertexAttribute("TEXCOORD_0")]
        public Vector2 TexCoord;

        /// <inheritdoc/>
        public int MaxColors => 1;

        /// <inheritdoc/>
        public int MaxTextCoords => 1;

        public override bool Equals(object obj) { return obj is VertexColor1Texture1 other && AreEqual(this, other); }
        public bool Equals(VertexColor1Texture1 other) { return AreEqual(this, other); }
        public static bool operator ==(in VertexColor1Texture1 a, in VertexColor1Texture1 b) { return AreEqual(a, b); }
        public static bool operator !=(in VertexColor1Texture1 a, in VertexColor1Texture1 b) { return !AreEqual(a, b); }
        public static bool AreEqual(in VertexColor1Texture1 a, in VertexColor1Texture1 b)
        {
            return a.TexCoord == b.TexCoord && a.Color == b.Color;
        }

        public override int GetHashCode() { return TexCoord.GetHashCode() ^ Color.GetHashCode(); }

        #endregion

        #region API

        /// <inheritdoc/>
        public VertexMaterialDelta Subtract(IVertexMaterial baseValue)
        {
            return new VertexMaterialDelta((VertexColor1Texture1)baseValue, this);
        }

        /// <inheritdoc/>
        public void Add(in VertexMaterialDelta delta)
        {
            this.Color += delta.Color0Delta;
            this.TexCoord += delta.TexCoord0Delta;
        }

        void IVertexMaterial.SetColor(int setIndex, Vector4 color) { if (setIndex == 0) this.Color = color; }

        void IVertexMaterial.SetTexCoord(int setIndex, Vector2 coord) { if (setIndex == 0) this.TexCoord = coord; }

        /// <inheritdoc/>
        public Vector4 GetColor(int index)
        {
            if (index != 0) throw new ArgumentOutOfRangeException(nameof(index));
            return Color;
        }

        /// <inheritdoc/>
        public Vector2 GetTexCoord(int index)
        {
            if (index != 0) throw new ArgumentOutOfRangeException(nameof(index));
            return TexCoord;
        }

        #endregion
    }

    /// <summary>
    /// Defines a Vertex attribute with a material Colors and two Texture Coordinates.
    /// </summary>
    [System.Diagnostics.DebuggerDisplay("{_GetDebuggerDisplay(),nq}")]
    public struct VertexColor1Texture2 : IVertexMaterial, IEquatable<VertexColor1Texture2>
    {
        #region debug

        private string _GetDebuggerDisplay() => VertexUtils._GetDebuggerDisplay(this);

        #endregion

        #region constructors

        public VertexColor1Texture2(Vector4 color, Vector2 tex0, Vector2 tex1)
        {
            Color = color;
            TexCoord0 = tex0;
            TexCoord1 = tex1;
        }

        public VertexColor1Texture2(IVertexMaterial src)
        {
            Guard.NotNull(src, nameof(src));

            this.Color = src.MaxColors > 0 ? src.GetColor(0) : Vector4.One;
            this.TexCoord0 = src.MaxTextCoords > 0 ? src.GetTexCoord(0) : Vector2.Zero;
            this.TexCoord1 = src.MaxTextCoords > 1 ? src.GetTexCoord(1) : Vector2.Zero;
        }

        public static implicit operator VertexColor1Texture2((Vector4 Color, Vector2 Tex0, Vector2 Tex1) tuple)
        {
            return new VertexColor1Texture2(tuple.Color, tuple.Tex0, tuple.Tex1);
        }

        #endregion

        #region data

        [VertexAttribute("COLOR_0", ENCODING.UNSIGNED_BYTE, true)]
        public Vector4 Color;

        [VertexAttribute("TEXCOORD_0")]
        public Vector2 TexCoord0;

        [VertexAttribute("TEXCOORD_1")]
        public Vector2 TexCoord1;

        /// <inheritdoc/>
        public int MaxColors => 1;

        /// <inheritdoc/>
        public int MaxTextCoords => 2;

        public override bool Equals(object obj) { return obj is VertexColor1Texture2 other && AreEqual(this, other); }
        public bool Equals(VertexColor1Texture2 other) { return AreEqual(this, other); }
        public static bool operator ==(in VertexColor1Texture2 a, in VertexColor1Texture2 b) { return AreEqual(a, b); }
        public static bool operator !=(in VertexColor1Texture2 a, in VertexColor1Texture2 b) { return !AreEqual(a, b); }
        public static bool AreEqual(in VertexColor1Texture2 a, in VertexColor1Texture2 b)
        {
            return a.Color == b.Color && a.TexCoord0 == b.TexCoord0 && a.TexCoord1 == b.TexCoord1;
        }

        public override int GetHashCode() { return Color.GetHashCode() ^ TexCoord0.GetHashCode() ^ TexCoord1.GetHashCode(); }

        #endregion

        #region API

        /// <inheritdoc/>
        public VertexMaterialDelta Subtract(IVertexMaterial baseValue)
        {
            return new VertexMaterialDelta((VertexColor1Texture2)baseValue, this);
        }

        /// <inheritdoc/>
        public void Add(in VertexMaterialDelta delta)
        {
            this.Color += delta.Color0Delta;
            this.TexCoord0 += delta.TexCoord0Delta;
            this.TexCoord1 += delta.TexCoord1Delta;
        }

        void IVertexMaterial.SetColor(int setIndex, Vector4 color) { if (setIndex == 0) this.Color = color; }

        void IVertexMaterial.SetTexCoord(int setIndex, Vector2 coord)
        {
            if (setIndex == 0) this.TexCoord0 = coord;
            if (setIndex == 1) this.TexCoord1 = coord;
        }

        /// <inheritdoc/>
        public Vector4 GetColor(int index)
        {
            if (index != 0) throw new ArgumentOutOfRangeException(nameof(index));
            return Color;
        }

        /// <inheritdoc/>
        public Vector2 GetTexCoord(int index)
        {
            switch (index)
            {
                case 0: return this.TexCoord0;
                case 1: return this.TexCoord1;
                default: throw new ArgumentOutOfRangeException(nameof(index));
            }
        }

        #endregion
    }

    /// <summary>
    /// Defines a Vertex attribute with two material Colors and two Texture Coordinates.
    /// </summary>
    [System.Diagnostics.DebuggerDisplay("{_GetDebuggerDisplay(),nq}")]
    public struct VertexColor2Texture1 : IVertexMaterial, IEquatable<VertexColor2Texture1>
    {
        #region debug

        private string _GetDebuggerDisplay() => VertexUtils._GetDebuggerDisplay(this);

        #endregion

        #region constructors

        public VertexColor2Texture1(Vector4 color0, Vector4 color1, Vector2 tex)
        {
            Color0 = color0;
            Color1 = color1;
            TexCoord = tex;
        }

        public VertexColor2Texture1(IVertexMaterial src)
        {
            Guard.NotNull(src, nameof(src));

            this.Color0 = src.MaxColors > 0 ? src.GetColor(0) : Vector4.One;
            this.Color1 = src.MaxColors > 1 ? src.GetColor(1) : Vector4.One;
            this.TexCoord = src.MaxTextCoords > 0 ? src.GetTexCoord(0) : Vector2.Zero;
        }

        public static implicit operator VertexColor2Texture1((Vector4 Color0, Vector4 Color1, Vector2 Tex) tuple)
        {
            return new VertexColor2Texture1(tuple.Color0, tuple.Color1, tuple.Tex);
        }

        #endregion

        #region data

        [VertexAttribute("COLOR_0", ENCODING.UNSIGNED_BYTE, true)]
        public Vector4 Color0;

        [VertexAttribute("COLOR_1", ENCODING.UNSIGNED_BYTE, true)]
        public Vector4 Color1;

        [VertexAttribute("TEXCOORD_0")]
        public Vector2 TexCoord;

        /// <inheritdoc/>
        public int MaxColors => 2;

        /// <inheritdoc/>
        public int MaxTextCoords => 1;

        public override bool Equals(object obj) { return obj is VertexColor2Texture1 other && AreEqual(this, other); }
        public bool Equals(VertexColor2Texture1 other) { return AreEqual(this, other); }
        public static bool operator ==(in VertexColor2Texture1 a, in VertexColor2Texture1 b) { return AreEqual(a, b); }
        public static bool operator !=(in VertexColor2Texture1 a, in VertexColor2Texture1 b) { return !AreEqual(a, b); }

        public static bool AreEqual(in VertexColor2Texture1 a, in VertexColor2Texture1 b)
        {
            return a.Color0 == b.Color0 && a.Color1 == b.Color1 && a.TexCoord == b.TexCoord;
        }

        public override int GetHashCode() { return Color0.GetHashCode() ^ Color1.GetHashCode() ^ TexCoord.GetHashCode(); }

        #endregion

        #region API

        /// <inheritdoc/>
        public VertexMaterialDelta Subtract(IVertexMaterial baseValue)
        {
            return new VertexMaterialDelta((VertexColor2Texture1)baseValue, this);
        }

        /// <inheritdoc/>
        public void Add(in VertexMaterialDelta delta)
        {
            this.Color0 += delta.Color0Delta;
            this.Color1 += delta.Color1Delta;
            this.TexCoord += delta.TexCoord0Delta;
        }

        void IVertexMaterial.SetColor(int setIndex, Vector4 color)
        {
            if (setIndex == 0) this.Color0 = color;
            if (setIndex == 1) this.Color1 = color;
        }

        void IVertexMaterial.SetTexCoord(int setIndex, Vector2 coord)
        {
            if (setIndex == 0) this.TexCoord = coord;
        }

        /// <inheritdoc/>
        public Vector4 GetColor(int index)
        {
            switch (index)
            {
                case 0: return this.Color0;
                case 1: return this.Color1;
                default: throw new ArgumentOutOfRangeException(nameof(index));
            }
        }

        /// <inheritdoc/>
        public Vector2 GetTexCoord(int index)
        {
            switch (index)
            {
                case 0: return this.TexCoord;
                default: throw new ArgumentOutOfRangeException(nameof(index));
            }
        }

        #endregion
    }

    /// <summary>
    /// Defines a Vertex attribute with two material Colors and two Texture Coordinates.
    /// </summary>
    [System.Diagnostics.DebuggerDisplay("{_GetDebuggerDisplay(),nq}")]
    public struct VertexColor2Texture2 : IVertexMaterial, IEquatable<VertexColor2Texture2>
    {
        #region debug

        private string _GetDebuggerDisplay() => VertexUtils._GetDebuggerDisplay(this);

        #endregion

        #region constructors

        public VertexColor2Texture2(Vector4 color0, Vector4 color1, Vector2 tex0, Vector2 tex1)
        {
            Color0 = color0;
            Color1 = color1;
            TexCoord0 = tex0;
            TexCoord1 = tex1;
        }

        public VertexColor2Texture2(IVertexMaterial src)
        {
            Guard.NotNull(src, nameof(src));

            this.Color0 = src.MaxColors > 0 ? src.GetColor(0) : Vector4.One;
            this.Color1 = src.MaxColors > 1 ? src.GetColor(1) : Vector4.One;
            this.TexCoord0 = src.MaxTextCoords > 0 ? src.GetTexCoord(0) : Vector2.Zero;
            this.TexCoord1 = src.MaxTextCoords > 1 ? src.GetTexCoord(1) : Vector2.Zero;
        }

        public static implicit operator VertexColor2Texture2((Vector4 Color0, Vector4 Color1, Vector2 Tex0, Vector2 Tex1) tuple)
        {
            return new VertexColor2Texture2(tuple.Color0, tuple.Color1, tuple.Tex0, tuple.Tex1);
        }

        #endregion

        #region data

        [VertexAttribute("COLOR_0", ENCODING.UNSIGNED_BYTE, true)]
        public Vector4 Color0;

        [VertexAttribute("COLOR_1", ENCODING.UNSIGNED_BYTE, true)]
        public Vector4 Color1;

        [VertexAttribute("TEXCOORD_0")]
        public Vector2 TexCoord0;

        [VertexAttribute("TEXCOORD_1")]
        public Vector2 TexCoord1;

        /// <inheritdoc/>
        public int MaxColors => 2;

        /// <inheritdoc/>
        public int MaxTextCoords => 2;

        public override bool Equals(object obj) { return obj is VertexColor2Texture2 other && AreEqual(this, other); }
        public bool Equals(VertexColor2Texture2 other) { return AreEqual(this, other); }
        public static bool operator ==(in VertexColor2Texture2 a, in VertexColor2Texture2 b) { return AreEqual(a, b); }
        public static bool operator !=(in VertexColor2Texture2 a, in VertexColor2Texture2 b) { return !AreEqual(a, b); }

        public static bool AreEqual(in VertexColor2Texture2 a, in VertexColor2Texture2 b)
        {
            return a.Color0 == b.Color0 && a.Color1 == b.Color1 && a.TexCoord0 == b.TexCoord0 && a.TexCoord1 == b.TexCoord1;
        }

        public override int GetHashCode() { return Color0.GetHashCode() ^ Color1.GetHashCode() ^ TexCoord0.GetHashCode() ^ TexCoord1.GetHashCode(); }

        #endregion

        #region API

        /// <inheritdoc/>
        public VertexMaterialDelta Subtract(IVertexMaterial baseValue)
        {
            return new VertexMaterialDelta((VertexColor2Texture2)baseValue, this);
        }

        /// <inheritdoc/>
        public void Add(in VertexMaterialDelta delta)
        {
            this.Color0 += delta.Color0Delta;
            this.Color1 += delta.Color1Delta;
            this.TexCoord0 += delta.TexCoord0Delta;
            this.TexCoord1 += delta.TexCoord1Delta;
        }

        void IVertexMaterial.SetColor(int setIndex, Vector4 color)
        {
            if (setIndex == 0) this.Color0 = color;
            if (setIndex == 1) this.Color1 = color;
        }

        void IVertexMaterial.SetTexCoord(int setIndex, Vector2 coord)
        {
            if (setIndex == 0) this.TexCoord0 = coord;
            if (setIndex == 1) this.TexCoord1 = coord;
        }

        /// <inheritdoc/>
        public Vector4 GetColor(int index)
        {
            switch (index)
            {
                case 0: return this.Color0;
                case 1: return this.Color1;
                default: throw new ArgumentOutOfRangeException(nameof(index));
            }
        }

        /// <inheritdoc/>
        public Vector2 GetTexCoord(int index)
        {
            switch (index)
            {
                case 0: return this.TexCoord0;
                case 1: return this.TexCoord1;
                default: throw new ArgumentOutOfRangeException(nameof(index));
            }
        }

        #endregion
    }

    /// <summary>
    /// Defines a Vertex attribute with two material Colors and two Texture Coordinates.
    /// </summary>
    [System.Diagnostics.DebuggerDisplay("{_GetDebuggerDisplay(),nq}")]
    public struct VertexMaterialDelta : IVertexMaterial, IEquatable<VertexMaterialDelta>
    {
        #region debug

        private string _GetDebuggerDisplay() => $"ΔC₀:{Color0Delta} ΔC₁:{Color1Delta} ΔUV₀:{TexCoord0Delta}  ΔUV₁:{TexCoord1Delta}";

        #endregion

        #region constructors

        public static implicit operator VertexMaterialDelta(in (Vector4 Color0Delta, Vector4 Color1Delta, Vector2 TextCoord0Delta, Vector2 TextCoord1Delta) tuple)
        {
            return new VertexMaterialDelta(tuple.Color0Delta, tuple.Color1Delta, tuple.TextCoord0Delta, tuple.TextCoord1Delta);
        }

        public VertexMaterialDelta(IVertexMaterial src)
        {
            Guard.NotNull(src, nameof(src));

            MaxColors = src.MaxColors;
            MaxTextCoords = src.MaxTextCoords;

            if (src.MaxColors == 0)
            {
                Color0Delta = Vector4.Zero;
                Color1Delta = Vector4.Zero;
            }
            else if (src.MaxColors == 1)
            {
                Color0Delta = src.GetColor(0);
                Color1Delta = Vector4.Zero;
            }
            else
            {
                Color0Delta = src.GetColor(0);
                Color1Delta = src.GetColor(1);
            }

            if (src.MaxTextCoords == 0)
            {
                TexCoord0Delta = Vector2.Zero;
                TexCoord1Delta = Vector2.Zero;
            }
            else if (src.MaxTextCoords == 1)
            {
                TexCoord0Delta = src.GetTexCoord(0);
                TexCoord1Delta = Vector2.Zero;
            }
            else
            {
                TexCoord0Delta = src.GetTexCoord(0);
                TexCoord1Delta = src.GetTexCoord(1);
            }
        }

        public VertexMaterialDelta(in Vector4 color0Delta, in Vector4 color1Delta, in Vector2 texCoord0Delta, in Vector2 texCoord1Delta)
        {
            MaxColors = 2;
            MaxTextCoords = 2;

            Color0Delta = color0Delta;
            Color1Delta = color1Delta;
            TexCoord0Delta = texCoord0Delta;
            TexCoord1Delta = texCoord1Delta;
        }

        internal VertexMaterialDelta(in VertexColor1 rootVal, in VertexColor1 morphVal)
        {
            MaxColors = 1;
            MaxTextCoords = 0;

            Color0Delta = morphVal.Color - rootVal.Color;
            Color1Delta = Vector4.Zero;
            TexCoord0Delta = Vector2.Zero;
            TexCoord1Delta = Vector2.Zero;
        }

        internal VertexMaterialDelta(in VertexColor2 rootVal, in VertexColor2 morphVal)
        {
            MaxColors = 2;
            MaxTextCoords = 0;

            Color0Delta = morphVal.Color0 - rootVal.Color0;
            Color1Delta = morphVal.Color1 - rootVal.Color1;
            TexCoord0Delta = Vector2.Zero;
            TexCoord1Delta = Vector2.Zero;
        }

        internal VertexMaterialDelta(in VertexTexture1 rootVal, in VertexTexture1 morphVal)
        {
            MaxColors = 0;
            MaxTextCoords = 1;

            Color0Delta = Vector4.Zero;
            Color1Delta = Vector4.Zero;
            TexCoord0Delta = morphVal.TexCoord - rootVal.TexCoord;
            TexCoord1Delta = Vector2.Zero;
        }

        internal VertexMaterialDelta(in VertexTexture2 rootVal, in VertexTexture2 morphVal)
        {
            MaxColors = 0;
            MaxTextCoords = 2;

            Color0Delta = Vector4.Zero;
            Color1Delta = Vector4.Zero;
            TexCoord0Delta = morphVal.TexCoord0 - rootVal.TexCoord0;
            TexCoord1Delta = morphVal.TexCoord1 - rootVal.TexCoord1;
        }

        internal VertexMaterialDelta(in VertexColor1Texture1 rootVal, in VertexColor1Texture1 morphVal)
        {
            MaxColors = 1;
            MaxTextCoords = 1;

            Color0Delta = morphVal.Color - rootVal.Color;
            Color1Delta = Vector4.Zero;
            TexCoord0Delta = morphVal.TexCoord - rootVal.TexCoord;
            TexCoord1Delta = Vector2.Zero;
        }

        internal VertexMaterialDelta(in VertexColor2Texture1 rootVal, in VertexColor2Texture1 morphVal)
        {
            MaxColors = 2;
            MaxTextCoords = 1;

            Color0Delta = morphVal.Color0 - rootVal.Color0;
            Color1Delta = morphVal.Color1 - rootVal.Color1;
            TexCoord0Delta = morphVal.TexCoord - rootVal.TexCoord;
            TexCoord1Delta = Vector2.Zero;
        }

        internal VertexMaterialDelta(in VertexColor1Texture2 rootVal, in VertexColor1Texture2 morphVal)
        {
            MaxColors = 1;
            MaxTextCoords = 2;

            Color0Delta = morphVal.Color - rootVal.Color;
            Color1Delta = Vector4.Zero;
            TexCoord0Delta = morphVal.TexCoord0 - rootVal.TexCoord0;
            TexCoord1Delta = morphVal.TexCoord1 - rootVal.TexCoord1;
        }

        internal VertexMaterialDelta(in VertexColor2Texture2 rootVal, in VertexColor2Texture2 morphVal)
        {
            MaxColors = 2;
            MaxTextCoords = 2;

            Color0Delta = morphVal.Color0 - rootVal.Color0;
            Color1Delta = morphVal.Color1 - rootVal.Color1;
            TexCoord0Delta = morphVal.TexCoord0 - rootVal.TexCoord0;
            TexCoord1Delta = morphVal.TexCoord1 - rootVal.TexCoord1;
        }

        internal VertexMaterialDelta(in VertexMaterialDelta rootVal, in VertexMaterialDelta morphVal)
        {
            if (rootVal.MaxColors != morphVal.MaxColors)
                throw new ArgumentException("MaxColors do not match!");
            if (rootVal.MaxTextCoords != morphVal.MaxTextCoords)
                throw new ArgumentException("MaxTextCoords do not match!");

            MaxColors = rootVal.MaxColors;
            MaxTextCoords = rootVal.MaxTextCoords;

            Color0Delta = morphVal.Color0Delta - rootVal.Color0Delta;
            Color1Delta = morphVal.Color1Delta - rootVal.Color1Delta;
            TexCoord0Delta = morphVal.TexCoord0Delta - rootVal.TexCoord0Delta;
            TexCoord1Delta = morphVal.TexCoord1Delta - rootVal.TexCoord1Delta;
        }

        #endregion

        #region data

        public static VertexMaterialDelta Zero => new VertexMaterialDelta(Vector4.Zero, Vector4.Zero, Vector2.Zero, Vector2.Zero);

        [VertexAttribute("COLOR_0DELTA", ENCODING.UNSIGNED_BYTE, true)]
        public Vector4 Color0Delta;

        [VertexAttribute("COLOR_1DELTA", ENCODING.UNSIGNED_BYTE, true)]
        public Vector4 Color1Delta;

        [VertexAttribute("TEXCOORD_0DELTA")]
        public Vector2 TexCoord0Delta;

        [VertexAttribute("TEXCOORD_1DELTA")]
        public Vector2 TexCoord1Delta;

        /// <inheritdoc/>
        public int MaxColors { get; }

        /// <inheritdoc/>
        public int MaxTextCoords { get; }

        public override bool Equals(object obj) { return obj is VertexMaterialDelta other && AreEqual(this, other); }
        public bool Equals(VertexMaterialDelta other) { return AreEqual(this, other); }
        public static bool operator ==(in VertexMaterialDelta a, in VertexMaterialDelta b) { return AreEqual(a, b); }
        public static bool operator !=(in VertexMaterialDelta a, in VertexMaterialDelta b) { return !AreEqual(a, b); }

        public static bool AreEqual(in VertexMaterialDelta a, in VertexMaterialDelta b)
        {
            return a.Color0Delta == b.Color0Delta && a.Color1Delta == b.Color1Delta && a.TexCoord0Delta == b.TexCoord0Delta && a.TexCoord1Delta == b.TexCoord1Delta;
        }

        public override int GetHashCode() { return Color0Delta.GetHashCode() ^ Color1Delta.GetHashCode() ^ TexCoord0Delta.GetHashCode() ^ TexCoord1Delta.GetHashCode(); }

        #endregion

        #region API

        /// <inheritdoc/>
        public VertexMaterialDelta Subtract(IVertexMaterial baseValue)
        {
            return new VertexMaterialDelta((VertexMaterialDelta)baseValue, this);
        }

        /// <inheritdoc/>
        public void Add(in VertexMaterialDelta delta)
        {
            this.Color0Delta += delta.Color0Delta;
            this.Color1Delta += delta.Color1Delta;
            this.TexCoord0Delta += delta.TexCoord0Delta;
            this.TexCoord1Delta += delta.TexCoord1Delta;
        }

        void IVertexMaterial.SetColor(int setIndex, Vector4 color)
        {
            SetColor(setIndex, color);
        }

        void SetColor(int setIndex, Vector4 color)
        {
            if (setIndex == 0) this.Color0Delta = color;
            if (setIndex == 1) this.Color1Delta = color;
        }

        void IVertexMaterial.SetTexCoord(int setIndex, Vector2 coord)
        {
            SetTexCoord(setIndex, coord);
        }

        void SetTexCoord(int setIndex, Vector2 coord)
        {
            if (setIndex == 0) this.TexCoord0Delta = coord;
            if (setIndex == 1) this.TexCoord1Delta = coord;
        }

        /// <inheritdoc/>
        public Vector4 GetColor(int index)
        {
            switch (index)
            {
                case 0: return this.Color0Delta;
                case 1: return this.Color1Delta;
                default: throw new ArgumentOutOfRangeException(nameof(index));
            }
        }

        /// <inheritdoc/>
        public Vector2 GetTexCoord(int index)
        {
            switch (index)
            {
                case 0: return this.TexCoord0Delta;
                case 1: return this.TexCoord1Delta;
                default: throw new ArgumentOutOfRangeException(nameof(index));
            }
        }

        #endregion
    }
}
