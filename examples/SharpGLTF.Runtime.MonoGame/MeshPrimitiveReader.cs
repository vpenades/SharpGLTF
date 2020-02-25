using System;
using System.Collections.Generic;
using System.Linq;

using Microsoft.Xna.Framework.Graphics;

using SharpGLTF.Schema2;

using XY = System.Numerics.Vector2;
using XYZ = System.Numerics.Vector3;
using XYZW = System.Numerics.Vector4;

namespace SharpGLTF.Runtime
{
    /// <summary>
    /// Reads the content of a glTF <see cref="MeshPrimitive"/> object into a structure that's easier to consume.
    /// </summary>
    class MeshPrimitiveReader
    {
        #region lifecycle

        public MeshPrimitiveReader(MeshPrimitive srcPrim, bool doubleSided, MeshNormalsFallback fallbackNormals)
        {
            _Positions = srcPrim.GetVertexAccessor("POSITION")?.AsVector3Array();
            _Normals = srcPrim.GetVertexAccessor("NORMAL")?.AsVector3Array();

            if (_Normals == null)
            {
                _Normals = new XYZ[_Positions.Count];

                for (int i = 0; i < _Normals.Count; ++i)
                {
                    _Normals[i] = fallbackNormals.GetNormal(_Positions[i]);
                }
            }

            _Color0 = srcPrim.GetVertexAccessor("COLOR_0")?.AsColorArray();
            _TexCoord0 = srcPrim.GetVertexAccessor("TEXCOORD_0")?.AsVector2Array();

            _Joints0 = srcPrim.GetVertexAccessor("JOINTS_0")?.AsVector4Array();
            _Joints1 = srcPrim.GetVertexAccessor("JOINTS_1")?.AsVector4Array();
            _Weights0 = srcPrim.GetVertexAccessor("WEIGHTS_0")?.AsVector4Array();
            _Weights1 = srcPrim.GetVertexAccessor("WEIGHTS_1")?.AsVector4Array();

            if (_Joints0 == null || _Weights0 == null) { _Joints0 = _Joints1 = _Weights0 = _Weights1 = null; }
            if (_Joints1 == null || _Weights1 == null) { _Joints1 = _Weights1 = null; }

            if (_Weights0 != null)
            {
                _Weights0 = _Weights0.ToArray(); // isolate memory to prevent overwriting source glTF.

                for (int i = 0; i < _Weights0.Count; ++i)
                {
                    var r = XYZW.Dot(_Weights0[i], XYZW.One);
                    _Weights0[i] /= r;
                }
            }

            if (doubleSided) // Monogame's effect material does not support double sided materials, so we simulate it by adding reverse faces
            {
                var front = srcPrim.GetTriangleIndices();
                var back = front.Select(item => (item.A, item.C, item.B));
                _Triangles = front.Concat(back).ToArray();
            }
            else
            {
                _Triangles = srcPrim.GetTriangleIndices().ToArray();
            }

            
        }

        #endregion

        #region data

        private readonly (int, int, int)[] _Triangles;

        private readonly IList<XYZ> _Positions;
        private readonly IList<XYZ> _Normals;

        private readonly IList<XYZW> _Color0;
        private readonly IList<XY> _TexCoord0;

        private readonly IList<XYZW> _Joints0;
        private readonly IList<XYZW> _Joints1;

        private readonly IList<XYZW> _Weights0;
        private readonly IList<XYZW> _Weights1;

        #endregion

        #region properties

        public bool IsSkinned => _Joints0 != null;

        public int VertexCount => _Positions?.Count ?? 0;

        public (int A, int B, int C)[] TriangleIndices => _Triangles;

        #endregion

        #region API        

        public VertexPositionNormalTexture[] ToXnaRigid()
        {
            var dst = new VertexPositionNormalTexture[_Positions.Count];

            for (int i = 0; i < dst.Length; ++i)
            {
                dst[i].Position = _Positions[i].ToXna();
                dst[i].Normal = _Normals[i].ToXna();                

                if (_TexCoord0 != null) dst[i].TextureCoordinate = _TexCoord0[i].ToXna();
            }

            return dst;
        }

        public VertexSkinned[] ToXnaSkinned()
        {
            var dst = new VertexSkinned[_Positions.Count];

            for (int i = 0; i < dst.Length; ++i)
            {
                dst[i].Position = _Positions[i].ToXna();
                dst[i].Normal = _Normals[i].ToXna();

                if (_TexCoord0 != null) dst[i].TextureCoordinate = _TexCoord0[i].ToXna();

                dst[i].BlendIndices = new Microsoft.Xna.Framework.Graphics.PackedVector.Byte4(_Joints0[i].ToXna());
                dst[i].BlendWeight = _Weights0[i].ToXna();
            }

            return dst;
        }

        #endregion
    }
}
