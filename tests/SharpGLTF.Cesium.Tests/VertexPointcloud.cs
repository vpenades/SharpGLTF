using System;
using System.Collections.Generic;
using System.Numerics;

using SharpGLTF.Geometry.VertexTypes;
using SharpGLTF.Schema2;

namespace SharpGLTF
{
    [System.Diagnostics.DebuggerDisplay("𝐂:{Color} 𝐔𝐕:{TexCoord}")]
    public struct VertexPointcloud : IVertexCustom
    {
        public VertexPointcloud(float intensity, float classification)
        {
            Intensity = intensity;
            Classification = classification;
        }

        public const string INTENSITYATTRIBUTENAME = "_INTENSITY";
        public const string CLASSIFICATIONATTRIBUTENAME = "_CLASSIFICATION";

        [VertexAttribute(INTENSITYATTRIBUTENAME, EncodingType.FLOAT, false)]
        public float Intensity;
        
        [VertexAttribute(CLASSIFICATIONATTRIBUTENAME, EncodingType.FLOAT, false)]
        public float Classification;

        public int MaxColors => 0;

        public int MaxTextCoords => 0;

        public IEnumerable<string> CustomAttributes => throw new NotImplementedException();

        public void SetColor(int setIndex, Vector4 color) {
        }

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
            if (attribute == INTENSITYATTRIBUTENAME) {
                value = Intensity; return true;
            }
            else if(attribute == CLASSIFICATIONATTRIBUTENAME)
            {
                value = Classification; return true;
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
