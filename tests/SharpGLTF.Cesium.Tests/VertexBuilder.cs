using SharpGLTF.Geometry;
using SharpGLTF.Geometry.VertexTypes;
using System.Numerics;

namespace SharpGLTF
{
    public static class VertexBuilder
    {
        internal static VertexBuilder<VertexPositionNormal, VertexWithFeatureIds, VertexEmpty> GetVertexWithFeatureIds(Vector3 position, Vector3 normal, int featureId0, int featureId1)
        {
            var vertexWithFeatureIds = new VertexWithFeatureIds(featureId0, featureId1);

            var vp0 = new VertexPositionNormal(position, normal);
            var vb0 = new VertexBuilder<VertexPositionNormal, VertexWithFeatureIds, VertexEmpty>(vp0, vertexWithFeatureIds);
            return vb0;
        }

        internal static VertexBuilder<VertexPositionNormal, VertexWithFeatureId, VertexEmpty> GetVertexWithFeatureId(Vector3 position, Vector3 normal, int featureid)
        {
            var vp0 = new VertexPositionNormal(position, normal);
            var vb0 = new VertexBuilder<VertexPositionNormal, VertexWithFeatureId, VertexEmpty>(vp0, featureid);
            return vb0;
        }

        internal static VertexBuilder<VertexPosition, VertexPointcloud, VertexEmpty> GetVertexPointcloud(Vector3 position, Vector4 color, float intensity, float classification)
        {
            var vertexPointcloud = new VertexPointcloud(color, intensity, classification);
            var vp0 = new VertexPosition(position);
            var vb0 = new VertexBuilder<VertexPosition, VertexPointcloud, VertexEmpty>(vp0, vertexPointcloud);
            return vb0;
        }
    }
}
