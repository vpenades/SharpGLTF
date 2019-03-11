using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
using System.Linq;

using SharpGLTF.Collections;

namespace SharpGLTF.Schema2.Authoring
{
    class SimpleSceneBuilder<TMaterial>
    {
        #region data

        private readonly VertexColumn<Vector3> _Positions = new VertexColumn<Vector3>();        
        private readonly Dictionary<TMaterial, List<int>> _Indices = new Dictionary<TMaterial, List<int>>();        

        #endregion

        #region API

        public void AddPolygon(TMaterial material, params (float,float,float)[] points)
        {
            var vertices = points.Select(item => new Vector3(item.Item1, item.Item2, item.Item3)).ToArray();

            AddPolygon(material, vertices);
        }

        public void AddPolygon(TMaterial material, params Vector3[] points)
        {
            for (int i = 2; i < points.Length; ++i)
            {
                AddTriangle(material, points[0], points[i - 1], points[i]);
            }
        }

        public void AddTriangle(TMaterial material, Vector3 a, Vector3 b, Vector3 c)
        {
            var aa = _Positions.Use(a);
            var bb = _Positions.Use(b);
            var cc = _Positions.Use(c);

            // check for degenerated triangles:
            if (aa == bb) return;
            if (aa == cc) return;
            if (bb == cc) return;

            if (!_Indices.TryGetValue(material, out List<int> indices))
            {
                indices = new List<int>();
                _Indices[material] = indices;
            }

            indices.Add(aa);
            indices.Add(bb);
            indices.Add(cc);
        }             

        public void CopyToNode(Node dstNode, Func<TMaterial, Material> materialEvaluator)
        {
            dstNode.Mesh = dstNode.LogicalParent.CreateMesh();
            CopyToMesh(dstNode.Mesh, materialEvaluator);
        }        

        public void CopyToMesh(Mesh dstMesh, Func<TMaterial,Material> materialEvaluator)
        {
            var root = dstMesh.LogicalParent;            

            // create vertex buffer
            const int byteStride = 12 * 2;

            var vbuffer = root.UseBufferView(new Byte[byteStride * _Positions.Count], byteStride, BufferMode.ARRAY_BUFFER);

            var positions = root
                .CreateAccessor("Positions")
                .WithVertexData(vbuffer, 0, _Positions);

            var normals = root
                .CreateAccessor("Normals")
                .WithVertexData(vbuffer, 12, _CalculateNormals());            

            foreach (var kvp in _Indices)
            {
                // create index buffer
                var ibuffer = root.UseBufferView(new Byte[4 * kvp.Value.Count], 0, BufferMode.ELEMENT_ARRAY_BUFFER);

                var indices = root
                    .CreateAccessor("Indices")
                    .WithIndexData(ibuffer, 0, kvp.Value);

                // create mesh primitive
                var prim = dstMesh.CreatePrimitive();
                prim.SetVertexAccessor("POSITION", positions);
                prim.SetVertexAccessor("NORMAL", normals);
                prim.SetIndexAccessor(indices);
                prim.DrawPrimitiveType = PrimitiveType.TRIANGLES;

                prim.Material = materialEvaluator(kvp.Key);
            }
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
