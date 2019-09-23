using System.Collections.Generic;

using XYZ = System.Numerics.Vector3;

namespace SharpGLTF.Runtime
{
    /// <summary>
    /// Helper class used to calculate smooth Normals on glTF meshes with missing normals.
    /// </summary>
    class MeshNormalsFallback
    {
        #region lifecycle

        public MeshNormalsFallback(Schema2.Mesh mesh)
        {
            foreach (var srcPrim in mesh.Primitives)
            {
                var accessor = srcPrim.GetVertexAccessor("POSITION");
                if (accessor == null) continue;

                var positions = accessor.AsVector3Array();

                foreach (var srcTri in srcPrim.GetTriangleIndices())
                {
                    var a = positions[srcTri.Item1];
                    var b = positions[srcTri.Item2];
                    var c = positions[srcTri.Item3];
                    var d = XYZ.Cross(b - a, c - a);

                    AddWeightedNormal(a, d);
                    AddWeightedNormal(b, d);
                    AddWeightedNormal(c, d);
                }
            }
        }

        #endregion

        #region data
        
        private readonly Dictionary<XYZ, XYZ> _WeightedNormals = new Dictionary<XYZ, XYZ>();

        #endregion

        #region API

        private void AddWeightedNormal(XYZ p, XYZ d)
        {
            if (_WeightedNormals.TryGetValue(p, out XYZ ddd)) ddd += d;
            else ddd = d;

            _WeightedNormals[p] = ddd;
        }

        public XYZ GetNormal(XYZ position)
        {
            if (!_WeightedNormals.TryGetValue(position, out XYZ normal)) normal = position;
            return normal == XYZ.Zero ? XYZ.UnitX : XYZ.Normalize(normal);
        }
        
        #endregion
    }
}
