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
        #region data

        public int NumVertices { get; internal set; }
        public int NumTriangles { get; internal set; }
        public int NumLines { get; internal set; }
        public int NumPoints { get; internal set; }

        public (XYZ Min, XYZ Max) Bounds { get; internal set; }

        public IEnumerable<string> VertexAttributes { get; internal set; }

        #endregion

        #region API

        protected virtual string DebuggerDisplay
        {
            get
            {
                var txt = string.Empty;

                if (!VertexAttributes.Contains("SCENE")) txt += " " + string.Join(" ", VertexAttributes);

                txt += " Vrts:" + NumVertices;
                txt += " Tris:" + NumTriangles;

                return txt;
            }

        }

        public void SetFrom(Schema2.Scene scene)
        {
            var tris = scene.EvaluateTriangles().ToList();

            this.NumTriangles = tris.Count;

            Bounds = tris
                .SelectMany(item => new[] { item.A, item.B, item.C })
                .Select(item => item.GetGeometry().GetPosition())
                .GetBounds();

            this.VertexAttributes = new[] { "SCENE" };
        }

        internal void SetFrom(IEnumerable<VisualReport> many)
        {
            NumVertices = many.Sum(item => item.NumVertices);
            NumTriangles = many.Sum(item => item.NumTriangles);
            NumLines = many.Sum(item => item.NumLines);
            NumPoints = many.Sum(item => item.NumPoints);

            var min = many.Select(item => item.Bounds.Min).GetMin();
            var max = many.Select(item => item.Bounds.Max).GetMax();
            Bounds = (min, max);

            VertexAttributes = many
                .SelectMany(item => item.VertexAttributes)
                .Distinct()
                .ToArray();
        }

        #endregion
    }

    [System.Diagnostics.DebuggerDisplay("{DebuggerDisplay,nq}")]
    public sealed class PrimitiveReport : VisualReport
    {
        internal PrimitiveReport(Schema2.MeshPrimitive prim)
        {
            var vertices = prim.GetVertexAccessor("POSITION").AsVector3Array();

            NumVertices = vertices.Count;
            NumPoints = prim.GetPointIndices().Count();
            NumLines = prim.GetLineIndices().Count();
            NumTriangles = prim.GetTriangleIndices().Count();
            Bounds = vertices.GetBounds();

            VertexAttributes = prim.VertexAccessors.Keys.ToArray();
        }

        public string MaterialName { get; private set; }

        [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
        protected override string DebuggerDisplay => "PRIM " + MaterialName + " " + base.DebuggerDisplay;
    }

    [System.Diagnostics.DebuggerDisplay("{DebuggerDisplay,nq}")]
    public class MeshReport : VisualReport
    {
        internal MeshReport(Schema2.Mesh mesh)
        {
            Name = mesh.Name;

            _Primitives = mesh.Primitives
                .Select(prim => new PrimitiveReport(prim))
                .ToArray();

            this.SetFrom(_Primitives);
        }

        [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.RootHidden)]
        private readonly PrimitiveReport[] _Primitives;        

        public String Name { get; private set; }

        [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
        protected override string DebuggerDisplay => "MESH " + Name + " " + base.DebuggerDisplay;
    }

    [System.Diagnostics.DebuggerDisplay("{DebuggerDisplay,nq}")]
    public class ModelReport : VisualReport
    {
        [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.RootHidden)]
        private readonly List<MeshReport> _Meshes = new List<MeshReport>();
        [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.RootHidden)]
        private readonly List<VisualReport> _Scenes = new List<VisualReport>();

        private readonly List<int> _VertexBuffers = new List<int>();
        private readonly List<int> _IndexBuffers = new List<int>();
        private readonly List<int> _DataBuffers = new List<int>();

        [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
        protected override string DebuggerDisplay => "MODEL" + base.DebuggerDisplay;

        public static ModelReport CreateReportFrom(Schema2.ModelRoot model)
        {
            var rrrr = new ModelReport();

            foreach (var vb in model.LogicalBufferViews.Where(item => item.IsVertexBuffer)) rrrr._VertexBuffers.Add(vb.Content.Count);
            foreach (var ib in model.LogicalBufferViews.Where(item => item.IsIndexBuffer)) rrrr._IndexBuffers.Add(ib.Content.Count);
            foreach (var db in model.LogicalBufferViews.Where(item => item.IsDataBuffer)) rrrr._DataBuffers.Add(db.Content.Count);

            foreach (var mesh in model.LogicalMeshes)
            {
                rrrr._Meshes.Add( new MeshReport(mesh) );                
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
