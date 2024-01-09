using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;

using SharpGLTF.Geometry.VertexTypes;
using SharpGLTF.Memory;
using SharpGLTF.Schema2;

namespace SharpGLTF
{
    #if NET6_0_OR_GREATER
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields)]
    #endif

    [System.Diagnostics.DebuggerDisplay("𝐂:{Color} 𝐔𝐕:{TexCoord}")]
    public struct VertexWithFeatureIds : IVertexCustom
    {
        public VertexWithFeatureIds(float featureId0, float featureId1)
        {
            FeatureId0 = featureId0;
            FeatureId1 = featureId1;
        }

        public const string FEATUREID0ATTRIBUTENAME = "_FEATURE_ID_0";
        public const string FEATUREID1ATTRIBUTENAME = "_FEATURE_ID_1";
        
        public float FeatureId0;        
        public float FeatureId1;

        IEnumerable<KeyValuePair<string, AttributeFormat>> IVertexReflection.GetEncodingAttributes()
        {
            yield return new KeyValuePair<string, AttributeFormat>(FEATUREID0ATTRIBUTENAME, new AttributeFormat(DimensionType.SCALAR));
            yield return new KeyValuePair<string, AttributeFormat>(FEATUREID1ATTRIBUTENAME, new AttributeFormat(DimensionType.SCALAR));
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
            throw new NotImplementedException();
        }

        public bool TryGetCustomAttribute(string attribute, out object value)
        {
            if (attribute == FEATUREID0ATTRIBUTENAME)
            {
                value = FeatureId0; return true;
            }
            else if (attribute == FEATUREID1ATTRIBUTENAME)
            {
                value = FeatureId1; return true;
            }
            else
            {
                value = null; return false;
            }

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
