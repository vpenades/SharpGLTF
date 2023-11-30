using SharpGLTF.Geometry;
using SharpGLTF.Geometry.VertexTypes;
using System.Numerics;

namespace SharpGLTF
{
    public static class VertexBuilder
    {
        internal static VertexBuilder<VertexPositionNormal, VertexWithFeatureId, VertexEmpty> GetVertexWithFeatureId(Vector3 position, Vector3 normal, int featureid)
        {
            var vp0 = new VertexPositionNormal(position, normal);
            var vb0 = new VertexBuilder<VertexPositionNormal, VertexWithFeatureId, VertexEmpty>(vp0, featureid);
            return vb0;
        }

    }
}
