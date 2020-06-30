using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;

using Plotly;
using Plotly.Types;

using TRACES = Plotly.Box<Plotly.Types.ITracesProperty>;

using VERTEX = SharpGLTF.Geometry.IVertexBuilder;

namespace SharpGLTF
{
    public class PlotlyScene
    {
        #region data

        private readonly List<TRACES> _Traces = new List<TRACES>();
        private readonly Dictionary<Object, int> mats = new Dictionary<Object, int>();                // materials to color mapping

        #endregion

        #region API

        public void AppendTriangles<TMaterial>(IEnumerable<(VERTEX A, VERTEX B, VERTEX C, TMaterial Material)> tris, Matrix4x4 xform, Func<TMaterial, int> materialColorFunc)
        {
            var trace = _CreateTrace(tris, xform, materialColorFunc);

            _Traces.Add(trace);
        }

        public Box<IPlotProperty> ToPlotProperties() { return Plot.traces(_Traces.ToArray()); }
        
        public Plot ToPlot()
        {
            var plot = ToPlotProperties();
            var layout = _CreateLayoutProperties();

            var document = new Plot(plot, layout);
            return document;
        }

        public string ToHtml()
        {
            Plot document = ToPlot();
            return document.Render().ToString();
        }

        #endregion

        #region core

        private static Box<IPlotProperty> _CreateLayoutProperties()
        {
            var xaxis = Scene.xaxis(Xaxis.color("red"));
            var yaxis = Scene.yaxis(Yaxis.color("green"));
            var zaxis = Scene.zaxis(Zaxis.color("blue"));
            var camera = Camera.up(Up.x(0), Up.y(1), Up.z(0));
            var scene = Layout.scene(Scene.Aspectmode.data(), Scene.camera(camera), xaxis, yaxis, zaxis);

            return Plot.layout
                (Layout.autosize(true)
                // , Layout.width(0)
                , Layout.height(920)
                // , Layout.margin(Margin.autoexpand(true))
                // , Layout.margin(Margin.pad(5))                    
                // , Layout.margin(Margin.t(5), Margin.b(5))                    
                , scene
                );
        }

        private TRACES _CreateTrace<TMaterial>(IEnumerable<(VERTEX A, VERTEX B, VERTEX C, TMaterial Material)> tris, Matrix4x4 xform, Func<TMaterial, int> materialColorFunc)
        {
            var vrts = new List<(Vector3 p, Vector3 n)>();              // vertex list
            var vrtm = new Dictionary<(Vector3 p, Vector3 n), int>();   // vertex sharing map

            var idxs = new List<(int, int, int)>();                     // triangle indices
            var tric = new List<int>();                                 // face colors

            int _useSharedVertex(VERTEX v)
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

        #endregion
    }
}
