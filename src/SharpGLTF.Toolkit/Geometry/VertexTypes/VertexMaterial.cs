using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Text;

using SharpGLTF.Memory;

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
    public interface IVertexMaterial : IVertexReflection
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


    public partial struct VertexColor1
    {
        public static implicit operator VertexColor1(Vector4 color)
        {
            return new VertexColor1(color);
        }
    }

    public partial struct VertexColor2
    {
        public static implicit operator VertexColor2((Vector4 Color0, Vector4 Color1) tuple)
        {
            return new VertexColor2(tuple.Color0, tuple.Color1);
        }
    }

    
    public partial struct VertexTexture1
    {
        public static implicit operator VertexTexture1(Vector2 uv)
        {
            return new VertexTexture1(uv);
        }
    }

    
    public partial struct VertexTexture2
    {
        public static implicit operator VertexTexture2((Vector2 Tex0, Vector2 Tex1) tuple)
        {
            return new VertexTexture2(tuple.Tex0, tuple.Tex1);
        }
    }

    
    public partial struct VertexColor1Texture1
    {
        public static implicit operator VertexColor1Texture1((Vector4 Color, Vector2 Tex) tuple)
        {
            return new VertexColor1Texture1(tuple.Color, tuple.Tex);
        }
    }

    
    public partial struct VertexColor1Texture2
    {
        public static implicit operator VertexColor1Texture2((Vector4 Color, Vector2 Tex0, Vector2 Tex1) tuple)
        {
            return new VertexColor1Texture2(tuple.Color, tuple.Tex0, tuple.Tex1);
        }
    }

    
    public partial struct VertexColor2Texture1
    {
        public static implicit operator VertexColor2Texture1((Vector4 Color0, Vector4 Color1, Vector2 Tex) tuple)
        {
            return new VertexColor2Texture1(tuple.Color0, tuple.Color1, tuple.Tex);
        }
    }

    
    public partial struct VertexColor2Texture2
    {
        public static implicit operator VertexColor2Texture2((Vector4 Color0, Vector4 Color1, Vector2 Tex0, Vector2 Tex1) tuple)
        {
            return new VertexColor2Texture2(tuple.Color0, tuple.Color1, tuple.Tex0, tuple.Tex1);
        }
    }

    /// <summary>
    /// Defines a Vertex attribute with two material Colors and two Texture Coordinates.
    /// </summary>    
    [System.Diagnostics.DebuggerDisplay("{_GetDebuggerDisplay(),nq}")]
    [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]
    public partial struct VertexMaterialDelta : IVertexMaterial, IEquatable<VertexMaterialDelta>
    {
        #region debug

        private readonly string _GetDebuggerDisplay() => $"ΔC₀:{Color0Delta} ΔC₁:{Color1Delta} ΔUV₀:{TexCoord0Delta}  ΔUV₁:{TexCoord1Delta}";

        #endregion

        #region constructors

        public static implicit operator VertexMaterialDelta(in (Vector4 Color0Delta, Vector4 Color1Delta, Vector2 TextCoord0Delta, Vector2 TextCoord1Delta) tuple)
        {
            return new VertexMaterialDelta(tuple.Color0Delta, tuple.Color1Delta, tuple.TextCoord0Delta, tuple.TextCoord1Delta, Vector2.Zero, Vector2.Zero);
        }

        public static implicit operator VertexMaterialDelta(in (Vector4 Color0Delta, Vector4 Color1Delta, Vector2 TextCoord0Delta, Vector2 TextCoord1Delta, Vector2 TextCoord2Delta, Vector2 TextCoord3Delta) tuple)
        {
            return new VertexMaterialDelta(tuple.Color0Delta, tuple.Color1Delta, tuple.TextCoord0Delta, tuple.TextCoord1Delta, tuple.TextCoord2Delta, tuple.TextCoord3Delta);
        }

        public VertexMaterialDelta(IVertexMaterial src)
        {
            Guard.NotNull(src, nameof(src));

            MaxColors = Math.Min(2, src.MaxColors);
            MaxTextCoords = Math.Min(4,src.MaxTextCoords);

            Color0Delta = src.MaxColors < 1 ? Vector4.Zero : src.GetColor(0);
            Color1Delta = src.MaxColors < 2 ? Vector4.Zero : src.GetColor(1);

            TexCoord0Delta = src.MaxTextCoords < 1 ? Vector2.Zero : src.GetTexCoord(0);
            TexCoord1Delta = src.MaxTextCoords < 2 ? Vector2.Zero : src.GetTexCoord(1);
            TexCoord2Delta = src.MaxTextCoords < 3 ? Vector2.Zero : src.GetTexCoord(2);
            TexCoord3Delta = src.MaxTextCoords < 4 ? Vector2.Zero : src.GetTexCoord(3);
        }
        
        public VertexMaterialDelta(in Vector4 color0Delta, in Vector4 color1Delta, in Vector2 texCoord0Delta, in Vector2 texCoord1Delta)
        {
            MaxColors = 2;
            MaxTextCoords = 4;

            Color0Delta = color0Delta;
            Color1Delta = color1Delta;
            TexCoord0Delta = texCoord0Delta;
            TexCoord1Delta = texCoord1Delta;
            TexCoord2Delta = Vector2.Zero;
            TexCoord3Delta = Vector2.Zero;
        }

        public VertexMaterialDelta(in Vector4 color0Delta, in Vector4 color1Delta, in Vector2 texCoord0Delta, in Vector2 texCoord1Delta, in Vector2 texCoord2Delta, in Vector2 texCoord3Delta)
        {
            MaxColors = 2;
            MaxTextCoords = 4;

            Color0Delta = color0Delta;
            Color1Delta = color1Delta;
            TexCoord0Delta = texCoord0Delta;
            TexCoord1Delta = texCoord1Delta;
            TexCoord2Delta = texCoord2Delta;
            TexCoord3Delta = texCoord3Delta;
        }
        
        internal VertexMaterialDelta(in VertexMaterialDelta rootVal, in VertexMaterialDelta morphVal)
        {
            if (rootVal.MaxColors != morphVal.MaxColors) throw new ArgumentException("MaxColors do not match!");
            if (rootVal.MaxTextCoords != morphVal.MaxTextCoords) throw new ArgumentException("MaxTextCoords do not match!");

            MaxColors = rootVal.MaxColors;
            MaxTextCoords = rootVal.MaxTextCoords;

            Color0Delta = morphVal.Color0Delta - rootVal.Color0Delta;
            Color1Delta = morphVal.Color1Delta - rootVal.Color1Delta;
            TexCoord0Delta = morphVal.TexCoord0Delta - rootVal.TexCoord0Delta;
            TexCoord1Delta = morphVal.TexCoord1Delta - rootVal.TexCoord1Delta;
            TexCoord2Delta = morphVal.TexCoord2Delta - rootVal.TexCoord2Delta;
            TexCoord3Delta = morphVal.TexCoord3Delta - rootVal.TexCoord3Delta;
        }

        #endregion

        #region data

        public static VertexMaterialDelta Zero => new VertexMaterialDelta(Vector4.Zero, Vector4.Zero, Vector2.Zero, Vector2.Zero, Vector2.Zero, Vector2.Zero);
                
        public Vector4 Color0Delta;        
        public Vector4 Color1Delta;        
        public Vector2 TexCoord0Delta;
        public Vector2 TexCoord1Delta;
        public Vector2 TexCoord2Delta;
        public Vector2 TexCoord3Delta;

        IEnumerable<KeyValuePair<string, AttributeFormat>> IVertexReflection.GetEncodingAttributes()
        {
            yield return new KeyValuePair<string, AttributeFormat>("COLOR_0DELTA", new AttributeFormat(Schema2.DimensionType.VEC4, ENCODING.UNSIGNED_BYTE, true));
            yield return new KeyValuePair<string, AttributeFormat>("COLOR_1DELTA", new AttributeFormat(Schema2.DimensionType.VEC4, ENCODING.UNSIGNED_BYTE, true));

            yield return new KeyValuePair<string, AttributeFormat>("TEXCOORD_0DELTA", new AttributeFormat(Schema2.DimensionType.VEC2));
            yield return new KeyValuePair<string, AttributeFormat>("TEXCOORD_1DELTA", new AttributeFormat(Schema2.DimensionType.VEC2));
            yield return new KeyValuePair<string, AttributeFormat>("TEXCOORD_2DELTA", new AttributeFormat(Schema2.DimensionType.VEC2));
            yield return new KeyValuePair<string, AttributeFormat>("TEXCOORD_3DELTA", new AttributeFormat(Schema2.DimensionType.VEC2));
        }

        /// <inheritdoc/>
        public int MaxColors { get; }

        /// <inheritdoc/>
        public int MaxTextCoords { get; }

        /// <inheritdoc/>
        public readonly override int GetHashCode()
        {
            return Color0Delta.GetHashCode()
                ^ Color1Delta.GetHashCode()
                ^ TexCoord0Delta.GetHashCode()
                ^ TexCoord1Delta.GetHashCode()
                ^ TexCoord2Delta.GetHashCode()
                ^ TexCoord3Delta.GetHashCode();
        }

        /// <inheritdoc/>
        public readonly override bool Equals(object obj) { return obj is VertexMaterialDelta other && AreEqual(this, other); }

        /// <inheritdoc/>
        public readonly bool Equals(VertexMaterialDelta other) { return AreEqual(this, other); }
        public static bool operator ==(in VertexMaterialDelta a, in VertexMaterialDelta b) { return AreEqual(a, b); }
        public static bool operator !=(in VertexMaterialDelta a, in VertexMaterialDelta b) { return !AreEqual(a, b); }
        public static bool AreEqual(in VertexMaterialDelta a, in VertexMaterialDelta b)
        {
            return a.Color0Delta == b.Color0Delta
                && a.Color1Delta == b.Color1Delta
                && a.TexCoord0Delta == b.TexCoord0Delta
                && a.TexCoord1Delta == b.TexCoord1Delta
                && a.TexCoord2Delta == b.TexCoord2Delta
                && a.TexCoord3Delta == b.TexCoord3Delta;
        }        

        #endregion

        #region API

        /// <inheritdoc/>
        public readonly VertexMaterialDelta Subtract(IVertexMaterial baseValue)
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
            this.TexCoord2Delta += delta.TexCoord2Delta;
            this.TexCoord3Delta += delta.TexCoord3Delta;
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
            if (setIndex == 2) this.TexCoord2Delta = coord;
            if (setIndex == 3) this.TexCoord3Delta = coord;
        }

        /// <inheritdoc/>
        public readonly Vector4 GetColor(int index)
        {
            switch (index)
            {
                case 0: return this.Color0Delta;
                case 1: return this.Color1Delta;
                default: throw new ArgumentOutOfRangeException(nameof(index));
            }
        }

        /// <inheritdoc/>
        public readonly Vector2 GetTexCoord(int index)
        {
            switch (index)
            {
                case 0: return this.TexCoord0Delta;
                case 1: return this.TexCoord1Delta;
                case 2: return this.TexCoord2Delta;
                case 3: return this.TexCoord3Delta;
                default: throw new ArgumentOutOfRangeException(nameof(index));
            }
        }

        #endregion
    }
}
