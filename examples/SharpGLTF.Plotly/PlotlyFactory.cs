using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;

using Plotly;
using Plotly.Types;

using SharpGLTF.Geometry;

namespace SharpGLTF
{
    static class PlotlyFactory
    {
        public static Box<ITracesProperty> ToTrace<TMaterial>(IEnumerable<(IVertexBuilder A, IVertexBuilder B, IVertexBuilder C, TMaterial Material)> tris, Func<TMaterial, int> materialColorFunc)
        {
            var mats = new Dictionary<TMaterial, int>();                // materials to color mapping

            var vrts = new List<(Vector3 p, Vector3 n)>();              // vertex list
            var vrtm = new Dictionary<(Vector3 p, Vector3 n), int>();   // vertex sharing map

            var idxs = new List<(int, int, int)>();                     // triangle indices
            var tric = new List<int>();                                 // face colors

            int _useSharedVertex(IVertexBuilder v)
            {
                var g = v.GetGeometry();

                g.TryGetNormal(out Vector3 n);
                var item = (g.GetPosition(), n);
                if (vrtm.TryGetValue(item, out int idx)) return idx;
                idx = vrts.Count;
                vrts.Add(item);
                vrtm.Add(item, idx);
                return idx;
            }

            foreach (var (A, B, C, Material) in tris)
            {
                if (!mats.TryGetValue(Material, out int color))
                {
                    mats[Material] = color = materialColorFunc(Material);
                }

                var ap = _useSharedVertex(A);
                var bp = _useSharedVertex(B);
                var cp = _useSharedVertex(C);

                idxs.Add((ap, bp, cp));
                tric.Add(color);
            }

            // create a Plotly Mesh3D from the previously filled lists.

            var mx = Mesh3d.x(vrts.Select(item => item.p.X));
            var my = Mesh3d.y(vrts.Select(item => item.p.Y));
            var mz = Mesh3d.z(vrts.Select(item => item.p.Z));

            var mi = Mesh3d.i(idxs.Select(item => item.Item1).ToArray());
            var mj = Mesh3d.j(idxs.Select(item => item.Item2).ToArray());
            var mk = Mesh3d.k(idxs.Select(item => item.Item3).ToArray());

            var mo = Mesh3d.opacity(1);
            var mc = Mesh3d.facecolor(tric.ToArray());

            return Traces.mesh3d(mx, my, mz, mi, mj, mk, mo, mc);
        }
    }
}
