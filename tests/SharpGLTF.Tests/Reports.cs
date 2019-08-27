using System;
using System.Collections.Generic;
using System.Text;

using XYZ = System.Numerics.Vector3;

using SharpGLTF.Schema2;
using System.Linq;

namespace SharpGLTF.Reporting
{
    public class VisualReport
    {
        private VisualReport(Schema2.MeshPrimitive prim)
        {
            SetFrom(prim);
        }

        internal VisualReport() { }

        public int NumTriangles { get; internal set; }
        public int NumLines { get; internal set; }
        public int NumPoints { get; internal set; }

        public (XYZ, XYZ) Bounds { get; internal set; }

        public IEnumerable<string> VertexAttributes { get; internal set; }

        public void SetFrom(Schema2.Mesh mesh)
        {
            SetFrom(mesh.Primitives.Select(prim => new VisualReport(prim)));
        }

        public void SetFrom(Schema2.MeshPrimitive prim)
        {
            NumPoints = prim.GetPointIndices().Count();
            NumLines = prim.GetLineIndices().Count();
            NumTriangles = prim.GetTriangleIndices().Count();
            Bounds = prim.GetVertexAccessor("POSITION").AsVector3Array().GetBounds();
        }

        public void SetFrom(Schema2.Scene scene)
        {
            var tris = scene.EvaluateTriangles().ToList();

            this.NumTriangles = tris.Count;

            Bounds = tris
                .SelectMany(item => new[] { item.Item1, item.Item2, item.Item3 })
                .Select(item => item.GetGeometry().GetPosition())
                .GetBounds();
        }

        internal void SetFrom(IEnumerable<VisualReport> many)
        {
            NumTriangles = many.Sum(item => item.NumTriangles);
            NumLines = many.Sum(item => item.NumLines);
            NumPoints = many.Sum(item => item.NumPoints);

            var min = many.Select(item => item.Bounds.Item1).GetMin();
            var max = many.Select(item => item.Bounds.Item2).GetMax();
            Bounds = (min, max);
        }
    }

    public class MeshReport : VisualReport
    {
        internal MeshReport(string name)
        {
            Name = name;
        }

        public String Name { get; private set; }
    }
    public class ModelReport : VisualReport
    {
        private readonly List<MeshReport> _Meshes = new List<MeshReport>();
        private readonly List<VisualReport> _Scenes = new List<VisualReport>();

        public static ModelReport CreateReportFrom(Schema2.ModelRoot model)
        {
            var rrrr = new ModelReport();

            foreach(var mesh in model.LogicalMeshes)
            {
                var r = new MeshReport(mesh.Name);
                r.SetFrom(mesh);
                rrrr._Meshes.Add(r);
                
            }

            foreach (var scene in model.LogicalScenes)
            {
                var r = new VisualReport();
                r.SetFrom(scene);
                rrrr._Scenes.Add(r);
            }

            rrrr.SetFrom(rrrr._Scenes);

            return rrrr;
        }
    }

    
}
