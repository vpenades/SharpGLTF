using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;

using SharpGLTF.Geometry.VertexTypes;
using SharpGLTF.Memory;
using SharpGLTF.Schema2;

namespace SharpGLTF
{
    [System.Diagnostics.DebuggerDisplay("𝐂:{Color} 𝐔𝐕:{TexCoord}")]
    public struct VertexWithFeatureId : IVertexCustom
    {
        public static implicit operator VertexWithFeatureId(float batchId)
        {
            return new VertexWithFeatureId(batchId);
        }

        public VertexWithFeatureId(float batchId)
        {
            BatchId = batchId;
        }

        public const string CUSTOMATTRIBUTENAME = "_FEATURE_ID_0";
        
        public float BatchId;

        IEnumerable<KeyValuePair<string, AttributeFormat>> IVertexReflection.GetEncodingAttributes()
        {
            yield return new KeyValuePair<string, AttributeFormat>(CUSTOMATTRIBUTENAME, new AttributeFormat(DimensionType.SCALAR));            
        }

        public int MaxColors => 0;

        public int MaxTextCoords => 0;

        public IEnumerable<string> CustomAttributes => throw new NotImplementedException();

        public void SetColor(int setIndex, Vector4 color) { }

        public void SetTexCoord(int setIndex, Vector2 coord) { }

        public Vector4 GetColor(int index) { throw new ArgumentOutOfRangeException(nameof(index)); }

        public Vector2 GetTexCoord(int index) { throw new ArgumentOutOfRangeException(nameof(index)); }

        public void Validate() { }

        public object GetCustomAttribute(string attributeName)
        {
            return attributeName == CUSTOMATTRIBUTENAME ? (Object)BatchId : null;
        }

        public bool TryGetCustomAttribute(string attributeName, out object value)
        {
            if (attributeName != CUSTOMATTRIBUTENAME) { value = null; return false; }
            value = BatchId; return true;
        }

        public void SetCustomAttribute(string attributeName, object value)
        {
            throw new NotImplementedException();
        }

        public VertexMaterialDelta Subtract(IVertexMaterial baseValue)
        {
            throw new NotImplementedException();
        }

        public void Add(in VertexMaterialDelta delta)
        {
            throw new NotImplementedException();
        }
    }
}
