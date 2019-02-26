using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace SharpGLTF
{
    using Memory;
    using System.Linq;
    using COLOR = UInt32;

    class BufferBuilder
    {
        #region data

        private readonly VertexColumn<Vector3> _Positions = new VertexColumn<Vector3>();        
        private readonly Dictionary<COLOR, List<int>> _Indices = new Dictionary<COLOR, List<int>>();        

        #endregion

        #region API        

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

            var node = root.UseScene(0).AddVisualNode("Default");

            node.Mesh = root.CreateMesh();

            const int byteStride = 12 * 2;

            var vbuffer = root.CreateBuffer(_Positions.Count * byteStride);
            var vview = root.CreateBufferView(vbuffer, null, null, byteStride, Schema2.BufferMode.ARRAY_BUFFER);

            var vpositions = root.CreateAccessor("Positions");
            vpositions.SetVertexData(vview, 0, Schema2.ElementType.VEC3, Schema2.ComponentType.FLOAT, false, _Positions.Count);            
            vpositions.AsVector3Array().FillFrom(0, _Positions.ToArray());
            vpositions.UpdateBounds();

            var vnormals = root.CreateAccessor("Normals");
            vnormals.SetVertexData(vview, 12, Schema2.ElementType.VEC3, Schema2.ComponentType.FLOAT, false, _Positions.Count);            
            vnormals.AsVector3Array().FillFrom(0, _CalculateNormals());
            vnormals.UpdateBounds();

            foreach (var kvp in _Indices)
            {
                var color = new Vector4((kvp.Key >> 24) & 255, (kvp.Key >> 16) & 255, (kvp.Key >> 8) & 255, (kvp.Key) & 255) / 255.0f;                

                var prim = node.Mesh.CreatePrimitive();
                prim.Material = root.CreateMaterial().InitializeDefault(color);
                prim.DrawPrimitiveType = Schema2.PrimitiveType.TRIANGLES;
                
                prim.SetVertexAccessor("POSITION", vpositions);
                prim.SetVertexAccessor("NORMAL", vnormals);                

                var ibuffer = root.CreateBuffer(kvp.Value.Count * 4);
                var iview = root.CreateBufferView(ibuffer, null, null, null, Schema2.BufferMode.ELEMENT_ARRAY_BUFFER);
                var indices = root.CreateAccessor("Indices");
                indices.AsIndicesArray().FillFrom(0, kvp.Value.Select(item => (uint)item));

                indices.SetIndexData(iview, 0, Schema2.IndexType.UNSIGNED_INT, kvp.Value.Count);
                prim.IndexAccessor = indices;
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
