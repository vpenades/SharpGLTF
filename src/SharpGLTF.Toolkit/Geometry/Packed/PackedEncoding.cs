using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using ENCODING = SharpGLTF.Schema2.EncodingType;

namespace SharpGLTF.Geometry
{
    class PackedEncoding
    {
        public ENCODING? JointsEncoding;
        public ENCODING? WeightsEncoding;

        public void AdjustJointEncoding<TVertex>(IReadOnlyList<TVertex> vertices)
            where TVertex : IVertexBuilder
        {
            if (JointsEncoding.HasValue) return;

            var indices = vertices.Select(item => item.GetSkinning().GetWeights().MaxIndex);
            var maxIndex = indices.Any() ? indices.Max() : 0;
            JointsEncoding = maxIndex < 256 ? ENCODING.UNSIGNED_BYTE : ENCODING.UNSIGNED_SHORT;
        }
    }
}
