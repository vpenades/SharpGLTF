using System;
using System.Collections.Generic;
using System.Linq;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;


namespace SharpGLTF.Runtime
{
    struct VertexSkinned : IVertexType
    {
        #region static

        private static VertexDeclaration _VDecl = CreateVertexDeclaration();

        public static VertexDeclaration CreateVertexDeclaration()
        {
            int offset = 0;

            var a = new VertexElement(offset, VertexElementFormat.Vector3, VertexElementUsage.Position, 0);
            offset += 3 * 4;

            var b = new VertexElement(offset, VertexElementFormat.Vector3, VertexElementUsage.Normal, 0);
            offset += 3 * 4;

            var c = new VertexElement(offset, VertexElementFormat.Vector2, VertexElementUsage.TextureCoordinate, 0);
            offset += 2 * 4;

            var d = new VertexElement(offset, VertexElementFormat.Byte4, VertexElementUsage.BlendIndices, 0);
            offset += 4 * 1;

            var e = new VertexElement(offset, VertexElementFormat.Vector4, VertexElementUsage.BlendWeight, 0);
            offset += 4 * 4;

            return new VertexDeclaration(a, b, c, d, e);
        }

        #endregion

        #region data

        public VertexDeclaration VertexDeclaration => _VDecl;

        public Vector3 Position;
        public Vector3 Normal;
        public Vector2 TextureCoordinate;
        public Microsoft.Xna.Framework.Graphics.PackedVector.Byte4 BlendIndices;
        public Vector4 BlendWeight;
        
        #endregion
    }

    public interface IEffectBones // it could be great if SkinnedEffect implemented this.
    {
        void SetBoneTransforms(Matrix[] boneTransforms, int boneStart, int boneCount);
    }
}
