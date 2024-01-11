using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Text;

using SharpGLTF.Memory;
using SharpGLTF.Schema2;

namespace SharpGLTF.Geometry.VertexTypes
{    
    [System.Diagnostics.DebuggerDisplay("𝐂:{Color} 𝐔𝐕:{TexCoord} {CustomId}")]
    public struct VertexColor1Texture1Custom1 : IVertexCustom
    {
        #region constructors

        public static implicit operator VertexColor1Texture1Custom1((Vector4 color, Vector2 tex, Single customId) tuple)
        {
            return new VertexColor1Texture1Custom1(tuple.color, tuple.tex, tuple.customId);
        }

        public VertexColor1Texture1Custom1(Vector4 color, Vector2 tex, Single customId)
        {
            Color = color;
            TexCoord = tex;
            CustomId = customId;
        }

        public VertexColor1Texture1Custom1(IVertexMaterial src)
        {
            this.Color = src.MaxColors > 0 ? src.GetColor(0) : Vector4.One;
            this.TexCoord = src.MaxTextCoords > 0 ? src.GetTexCoord(0) : Vector2.Zero;

            this.CustomId = 0;

            if (src is VertexColor1Texture1Custom1 custom)
            {
                this.CustomId = custom.CustomId;
            }
            else if (src is IVertexCustom otherx)
            {
                if (otherx.TryGetCustomAttribute(CUSTOMATTRIBUTENAME, out object attr0) && attr0 is float c0) this.CustomId = c0;                
            }
        }        

        #endregion

        #region data

        public const string CUSTOMATTRIBUTENAME = "_CUSTOM_0";
        
        public Single CustomId;        
        public Vector4 Color;        
        public Vector2 TexCoord;

        IEnumerable<KeyValuePair<string, AttributeFormat>> IVertexReflection.GetEncodingAttributes()
        {            
            yield return new KeyValuePair<string, AttributeFormat>("COLOR_0", new AttributeFormat(DimensionType.VEC4, EncodingType.UNSIGNED_BYTE, true));
            yield return new KeyValuePair<string, AttributeFormat>("TEXCOORD_0", new AttributeFormat(DimensionType.VEC2));
            yield return new KeyValuePair<string, AttributeFormat>(CUSTOMATTRIBUTENAME, new AttributeFormat(DimensionType.SCALAR));
        }

        public int MaxColors => 1;

        public int MaxTextCoords => 1;

        private static readonly string[] _CustomNames = { CUSTOMATTRIBUTENAME };
        public IEnumerable<string> CustomAttributes => _CustomNames;

        #endregion

        #region API

        /// <inheritdoc/>
        public VertexMaterialDelta Subtract(IVertexMaterial baseValue)
        {
            return this.Subtract((VertexColor1Texture1Custom1)baseValue);
        }

        /// <inheritdoc cref="Subtract(IVertexMaterial)"/>
        public VertexMaterialDelta Subtract(in VertexColor1Texture1Custom1 baseValue)
        {
            return new VertexMaterialDelta(this.Color - baseValue.Color, Vector4.Zero,
                this.TexCoord - baseValue.TexCoord, Vector2.Zero);
        }

        /// <inheritdoc/>
        public void Add(in VertexMaterialDelta delta)
        {
            this.Color += delta.Color0Delta;
            this.TexCoord += delta.TexCoord0Delta;
        }

        void IVertexMaterial.SetColor(int setIndex, Vector4 color) { if (setIndex == 0) this.Color = color; }

        void IVertexMaterial.SetTexCoord(int setIndex, Vector2 coord) { if (setIndex == 0) this.TexCoord = coord; }

        public Vector4 GetColor(int index)
        {
            if (index != 0) throw new ArgumentOutOfRangeException(nameof(index));
            return Color;
        }

        public Vector2 GetTexCoord(int index)
        {
            if (index != 0) throw new ArgumentOutOfRangeException(nameof(index));
            return TexCoord;
        }

        public void Validate()
        {
            if (CustomId < 0) throw new ArgumentOutOfRangeException(nameof(CustomId));
            if (CustomId > 1) throw new ArgumentOutOfRangeException(nameof(CustomId));
        }

        public bool TryGetCustomAttribute(string attribute, out object value)
        {
            if (attribute != CUSTOMATTRIBUTENAME) { value = null; return false; }
            value = CustomId; return true;
        }

        public void SetCustomAttribute(string attributeName, object value)
        {
            if (attributeName == CUSTOMATTRIBUTENAME && value is Single valueSingle) CustomId = valueSingle;
        }        

        #endregion
    }
    
    [System.Diagnostics.DebuggerDisplay("{CustomId0} {CustomId1}")]
    public struct VertexCustom2 : IVertexCustom
    {
        #region constructors

        public static implicit operator VertexCustom2((Single val, Vector4 vec) tuple)
        {
            return new VertexCustom2(tuple.val, tuple.vec);
        }

        public VertexCustom2(Single val, Vector4 vec)
        {
            CustomId0 = val;
            CustomId1 = vec;
        }

        public VertexCustom2(IVertexMaterial src)
        {
            this.CustomId0 = 0;
            this.CustomId1 = Vector4.Zero;

            if (src is VertexCustom2 other)
            {
                this.CustomId0 = other.CustomId0;
                this.CustomId1 = other.CustomId1;
            }
            else if (src is IVertexCustom otherx)
            {
                if (otherx.TryGetCustomAttribute(CUSTOMATTRIBUTENAME0, out object attr0) && attr0 is float c0) this.CustomId0 = c0;
                if (otherx.TryGetCustomAttribute(CUSTOMATTRIBUTENAME0, out object attr1) && attr1 is Vector4 c1) this.CustomId1 = c1;
            }            
        }        

        #endregion

        #region data

        public const string CUSTOMATTRIBUTENAME0 = "_CUSTOM_0";
        public const string CUSTOMATTRIBUTENAME1 = "_CUSTOM_1";
        
        public Single CustomId0;        
        public Vector4 CustomId1;

        IEnumerable<KeyValuePair<string, AttributeFormat>> IVertexReflection.GetEncodingAttributes()
        {            
            yield return new KeyValuePair<string, AttributeFormat>(CUSTOMATTRIBUTENAME0, new AttributeFormat(DimensionType.SCALAR, EncodingType.FLOAT, false));
            yield return new KeyValuePair<string, AttributeFormat>(CUSTOMATTRIBUTENAME1, new AttributeFormat(DimensionType.VEC4, EncodingType.UNSIGNED_BYTE, true));
        }

        public int MaxColors => 0;

        public int MaxTextCoords => 0;

        public IEnumerable<string> CustomAttributes
        {
            get
            {
                yield return CUSTOMATTRIBUTENAME0;
                yield return CUSTOMATTRIBUTENAME1;
            }
        }

        #endregion

        #region API

        /// <inheritdoc/>
        public VertexMaterialDelta Subtract(IVertexMaterial baseValue)
        {
            return this.Subtract((VertexColor1Texture1Custom1)baseValue);
        }

        /// <inheritdoc cref="Subtract(IVertexMaterial)"/>
        public VertexMaterialDelta Subtract(in VertexColor1Texture1Custom1 baseValue)
        {
            return new VertexMaterialDelta(Vector4.Zero, Vector4.Zero, Vector2.Zero, Vector2.Zero);
        }

        /// <inheritdoc/>
        public void Add(in VertexMaterialDelta delta)
        {
        }

        void IVertexMaterial.SetColor(int setIndex, Vector4 color) { }

        void IVertexMaterial.SetTexCoord(int setIndex, Vector2 coord) { }

        public Vector4 GetColor(int index) { throw new ArgumentOutOfRangeException(nameof(index)); }

        public Vector2 GetTexCoord(int index) { throw new ArgumentOutOfRangeException(nameof(index)); }

        public void Validate()
        {
            if (CustomId0 < 0) throw new ArgumentOutOfRangeException(nameof(CustomId0));
            if (CustomId0 > 1) throw new ArgumentOutOfRangeException(nameof(CustomId0));
        }

        public bool TryGetCustomAttribute(string attributeName, out object value)
        {
            switch (attributeName)
            {
                case CUSTOMATTRIBUTENAME0: value = CustomId0; return true;
                case CUSTOMATTRIBUTENAME1: value = CustomId1; return true;
            }

            value = null;
            return false;
        }

        public void SetCustomAttribute(string attributeName, object value)
        {
            switch (attributeName)
            {
                case CUSTOMATTRIBUTENAME0: if (value is Single c0) CustomId0 = c0; break;
                case CUSTOMATTRIBUTENAME1: if (value is Vector4 c1) CustomId1 = c1; break;
            }
        }

        #endregion
    }
}
