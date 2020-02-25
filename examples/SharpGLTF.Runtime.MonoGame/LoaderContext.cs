using System;
using System.Collections.Generic;
using System.Linq;

using Microsoft.Xna.Framework.Graphics;

using SharpGLTF.Schema2;

#if USINGMONOGAMEMODEL
using MODELMESH = Microsoft.Xna.Framework.Graphics.ModelMesh;
using MODELMESHPART = Microsoft.Xna.Framework.Graphics.ModelMeshPart;
#else
using MODELMESH = SharpGLTF.Runtime.ModelMeshReplacement;
using MODELMESHPART = SharpGLTF.Runtime.ModelMeshPartReplacement;
#endif

namespace SharpGLTF.Runtime
{
    /// <summary>
    /// Helper class used to import a glTF model into MonoGame
    /// </summary>
    class LoaderContext
    {
        #region lifecycle

        public LoaderContext(GraphicsDevice device)
        {
            _Device = device;
            _MatFactory = new MaterialFactory(device, _Disposables);
        }

        #endregion

        #region data

        private GraphicsDevice _Device;

        private readonly GraphicsResourceTracker _Disposables = new GraphicsResourceTracker();
        private readonly MaterialFactory _MatFactory;        

        private readonly Dictionary<Mesh, MODELMESH> _RigidMeshes = new Dictionary<Mesh, MODELMESH>();
        private readonly Dictionary<Mesh, MODELMESH> _SkinnedMeshes = new Dictionary<Mesh, MODELMESH>();
        
        #endregion

        #region properties

        public IReadOnlyList<GraphicsResource> Disposables => _Disposables.Disposables;

        #endregion

        #region Mesh API

        private static IEnumerable<Schema2.MeshPrimitive> GetValidPrimitives(Schema2.Mesh srcMesh)
        {
            foreach (var srcPrim in srcMesh.Primitives)
            {
                var ppp = srcPrim.GetVertexAccessor("POSITION");
                if (ppp.Count < 3) continue;

                if (srcPrim.DrawPrimitiveType == Schema2.PrimitiveType.POINTS) continue;
                if (srcPrim.DrawPrimitiveType == Schema2.PrimitiveType.LINES) continue;
                if (srcPrim.DrawPrimitiveType == Schema2.PrimitiveType.LINE_LOOP) continue;
                if (srcPrim.DrawPrimitiveType == Schema2.PrimitiveType.LINE_STRIP) continue;

                yield return srcPrim;
            }
        }

        public MODELMESH CreateMesh(Schema2.Mesh srcMesh, int maxBones = 72)
        {
            if (_Device == null) throw new InvalidOperationException();            

            var srcPrims = GetValidPrimitives(srcMesh).ToList();            

            var dstMesh = new MODELMESH(_Device, Enumerable.Range(0, srcPrims.Count).Select(item => new MODELMESHPART()).ToList());

            dstMesh.Name = srcMesh.Name;
            dstMesh.BoundingSphere = srcMesh.CreateBoundingSphere();

            var srcNormals = new MeshNormalsFallback(srcMesh);

            var idx = 0;
            foreach (var srcPrim in srcPrims)
            {
                CreateMeshPart(dstMesh.MeshParts[idx++], srcPrim, srcNormals, maxBones);
            }

            return dstMesh;
        }

        private void CreateMeshPart(MODELMESHPART dstPart, MeshPrimitive srcPart, MeshNormalsFallback normalsFunc, int maxBones)
        {
            var doubleSided = srcPart.Material?.DoubleSided ?? false;

            var srcGeometry = new MeshPrimitiveReader(srcPart, doubleSided, normalsFunc);

            var eff = srcGeometry.IsSkinned ? _MatFactory.UseSkinnedEffect(srcPart.Material) : _MatFactory.UseRigidEffect(srcPart.Material);

            dstPart.Effect = eff;            

            var vb = srcGeometry.IsSkinned ? CreateVertexBuffer(srcGeometry.ToXnaSkinned()) : CreateVertexBuffer(srcGeometry.ToXnaRigid());

            dstPart.VertexBuffer = vb;
            dstPart.NumVertices = srcGeometry.VertexCount;
            dstPart.VertexOffset = 0;

            dstPart.IndexBuffer = CreateIndexBuffer(srcGeometry.TriangleIndices);
            dstPart.PrimitiveCount = srcGeometry.TriangleIndices.Length;
            dstPart.StartIndex = 0;
        }
        
        #endregion

        #region resources API

        private VertexBuffer CreateVertexBuffer<T>(T[] dstVertices) where T:struct, IVertexType
        {
            var vb = new VertexBuffer(_Device, typeof(T), dstVertices.Length, BufferUsage.None);
            _Disposables.AddDisposable(vb);

            vb.SetData(dstVertices);
            return vb;
        }

        private IndexBuffer CreateIndexBuffer(IEnumerable<(int A, int B, int C)> triangles)
        {
            var sequence32 = triangles
                .SelectMany(item => new[] { (UInt32)item.C, (UInt32)item.B, (UInt32)item.A })
                .ToArray();

            var max = sequence32.Max();

            if (max > 65535)
            {
                var indices = new IndexBuffer(_Device, typeof(UInt32), sequence32.Length, BufferUsage.None);
                _Disposables.AddDisposable(indices);

                indices.SetData(sequence32);
                return indices;
            }
            else
            {
                var sequence16 = sequence32.Select(item => (UInt16)item).ToArray();                

                var indices = new IndexBuffer(_Device, typeof(UInt16), sequence16.Length, BufferUsage.None);
                _Disposables.AddDisposable(indices);

                indices.SetData(sequence16);
                return indices;
            }
        }        

        #endregion
    }    
}
