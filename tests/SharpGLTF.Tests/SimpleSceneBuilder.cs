using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
using System.Linq;

namespace SharpGLTF
{
    using COLOR = Vector4;

    class SimpleSceneBuilder
    {
        #region data

        private readonly VertexColumn<Vector3> _Positions = new VertexColumn<Vector3>();        
        private readonly Dictionary<COLOR, List<int>> _Indices = new Dictionary<COLOR, List<int>>();        

        #endregion

        #region API

        public void AddPolygon(COLOR color, params (float,float,float)[] points)
        {
            var vertices = points.Select(item => new Vector3(item.Item1, item.Item2, item.Item3)).ToArray();

            AddPolygon(color, vertices);
        }

        public void AddTriangle(COLOR color, Vector3 a, Vector3 b, Vector3 c)
        {
            var aa = _Positions.Use(a);
            var bb = _Positions.Use(b);
            var cc = _Positions.Use(c);

            // check for degenerated triangles:
            if (aa == bb) return;
            if (aa == cc) return;
            if (bb == cc) return;

            if (!_Indices.TryGetValue(color, out List<int> indices))
            {
                indices = new List<int>();
                _Indices[color] = indices;
            }

            indices.Add(aa);
            indices.Add(bb);
            indices.Add(cc);
        }

        public void AddPolygon(COLOR color, params Vector3[] points)
        {
            for(int i=2; i < points.Length; ++i)
            {
                AddTriangle(color, points[0], points[i - 1], points[i]);
            }
        }

        public Schema2.ModelRoot ToModel()
        {
            var root = Schema2.ModelRoot.CreateModel();

            var node = root.UseScene(0).CreateNode("Default");            

            // create vertex buffer
            const int byteStride = 12 * 2;            

            var vbuffer = root.UseBufferView(new Byte[byteStride * _Positions.Count], byteStride, Schema2.BufferMode.ARRAY_BUFFER);

            var positions = root
                .CreateAccessor("Positions")
                .WithVertexData(vbuffer, 0, _Positions);

            var ppp = positions.AsVector3Array();

            var normals = root
                .CreateAccessor("Normals")
                .WithVertexData(vbuffer, 12, _CalculateNormals());

            var nnn = normals.AsVector3Array();

            // create mesh
            node.Mesh = root.CreateMesh();

            foreach (var kvp in _Indices)
            {
                // create index buffer
                var ibuffer = root.UseBufferView(new Byte[4 * kvp.Value.Count], 0, Schema2.BufferMode.ELEMENT_ARRAY_BUFFER);

                var indices = root
                    .CreateAccessor("Indices")
                    .WithIndexData(ibuffer, 0, kvp.Value);

                // create mesh primitive
                var prim = node.Mesh.CreatePrimitive();
                prim.SetVertexAccessor("POSITION", positions);
                prim.SetVertexAccessor("NORMAL", normals);
                prim.SetIndexAccessor(indices);
                prim.DrawPrimitiveType = Schema2.PrimitiveType.TRIANGLES;
                prim.Material = root.CreateMaterial().InitializeDefault(kvp.Key);
                prim.Material.DoubleSided = true;
            }

            root.MergeBuffers();

            return root;
        }

        private Vector3[] _CalculateNormals()
        {
            var normals = new Vector3[_Positions.Count];

            foreach(var prim in _Indices.Values)
            {
                for(int i=0; i < prim.Count; i+=3)
                {
                    var a = prim[i + 0];
                    var b = prim[i + 1];
                    var c = prim[i + 2];

                    var aa = _Positions[a];
                    var bb = _Positions[b];
                    var cc = _Positions[c];

                    var n = Vector3.Cross(bb - aa, cc - aa);

                    normals[a] += n;
                    normals[b] += n;
                    normals[c] += n;
                }
            }

            for(int i=0; i < normals.Length; ++i)
            {
                normals[i] = normals[i].Length() <= float.Epsilon ? Vector3.UnitX : Vector3.Normalize(normals[i]);
            }

            return normals;
        }

        #endregion
    }

    
}
