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

        internal static VertexBuilder<VertexPosition, VertexPointcloud, VertexEmpty> GetVertexPointcloud(Vector3 position, float intensity, float classification)
        {
            var vertexPointcloud = new VertexPointcloud(intensity, classification);
            vertexPointcloud.SetColor(0, new Vector4(1, 0, 0, 0));
            var vp0 = new VertexPosition(position);
            var vb0 = new VertexBuilder<VertexPosition, VertexPointcloud, VertexEmpty>(vp0, vertexPointcloud);
            return vb0;
        }
    }
}
