using System;
using System.Collections.Generic;
using System.Linq;

using Microsoft.Xna.Framework.Graphics;

using SharpGLTF.Runtime.Template;
using SharpGLTF.Schema2;

using MODELMESH = SharpGLTF.Runtime.Template.RuntimeModelMesh;
using SRCMATERIAL = SharpGLTF.Schema2.Material;
using SRCMESH = SharpGLTF.Schema2.Mesh;
using SRCPRIM = SharpGLTF.Schema2.MeshPrimitive;


namespace SharpGLTF.Runtime.Pipeline
{
    /// <summary>
    /// Helper class used to import a glTF meshes and materials into MonoGame
    /// </summary>
    /// <remarks>
    /// derived types: <see cref="DefaultMeshesFactory"/>
    /// </remarks>
    public abstract class MeshesFactory
    {
        #region lifecycle        

        /// <summary>
        /// Register here your own <see cref="MeshesFactory"/> derived class to override mesh creation
        /// </summary>
        public static Func<GraphicsDevice, MeshesFactory> InstanceBuilder { get; set; }

        public static MeshesFactory Create(GraphicsDevice device)
        {
            ArgumentNullException.ThrowIfNull(device);

            var mf = InstanceBuilder?.Invoke(device);
            mf ??= new DefaultMeshesFactory(device);
            return mf;
        }

        protected MeshesFactory(GraphicsDevice device)
        {
            _Device = device;
        }

        #endregion

        #region data

        private GraphicsDevice _Device;

        private GraphicsResourceTracker _Disposables;
        
        private EffectsFactory _EffectsFactory;

        // gathers all meshes using shared vertex and index buffers whenever possible.
        private MeshPrimitiveWriter _MeshWriter;
        private int _CurrentMeshIndex;

        // used as a container to a default material;
        private ModelRoot _DummyModel; 

        #endregion

        #region properties        
        
        /// <summary>
        /// Gets the collection of disposables accumulated by processing meshes, effects and textures.
        /// </summary>
        internal IReadOnlyList<GraphicsResource> Disposables => _Disposables.Disposables;

        #endregion

        #region API

        internal void Reset()
        {
            _Disposables = new GraphicsResourceTracker();            
            _EffectsFactory = EffectsFactory.Create(_Device, _Disposables);            
        }

        internal IReadOnlyDictionary<int, MODELMESH> CreateRuntimeMeshes(IEnumerable<SRCMESH> srcMeshes)
        {
            _MeshWriter = new MeshPrimitiveWriter();

            foreach (var srcMesh in srcMeshes)
            {
                _WriteMesh(srcMesh);
            }

            return _MeshWriter.GetRuntimeMeshes(_Device, _Disposables);
        }

        #endregion

        #region Mesh API

        private void _WriteMesh(SRCMESH srcMesh)
        {
            if (_Device == null) throw new InvalidOperationException();            

            var srcPrims = _GetValidPrimitives(srcMesh)
                .ToDictionary(item => item, item => new MeshPrimitiveReader(item, item.Material?.DoubleSided ?? false));

            VertexNormalsFactory.CalculateSmoothNormals(srcPrims.Values.ToList());
            VertexTangentsFactory.CalculateTangents(srcPrims.Values.ToList());
            
            foreach (var srcPrim in srcPrims)
            {
                _CurrentMeshIndex = srcMesh.LogicalIndex;

                _WriteMeshPrimitive(srcPrim.Value, srcPrim.Key.Material);                
            }            
        }

        private static IEnumerable<SRCPRIM> _GetValidPrimitives(SRCMESH srcMesh)
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

        private void _WriteMeshPrimitive(MeshPrimitiveReader srcPrim, SRCMATERIAL srcMaterial)
        {
            srcMaterial ??= GetDefaultMaterial();

            var effect = _EffectsFactory.UseEffect(srcMaterial, srcPrim.IsSkinned);

            WriteMeshPrimitive(srcPrim, effect);
        }        
        

        protected void WriteMeshPrimitive<TVertex>(Effect effect, MeshPrimitiveReader primitive)
            where TVertex : unmanaged, IVertexType
        {
            _MeshWriter.WriteMeshPrimitive<TVertex>(_CurrentMeshIndex, effect, primitive);
        }             

        private SRCMATERIAL GetDefaultMaterial()
        {
            if (_DummyModel != null)
            {
                _DummyModel = ModelRoot.CreateModel();
                _DummyModel.CreateMaterial("Default");
            }
            
            return _DummyModel.LogicalMaterials[0];
        }

        #endregion

        #region overridable API

        /// <summary>
        /// Converts a <see cref="MeshPrimitiveReader"/> to a device primitive.
        /// </summary>
        /// <param name="srcPrimitive">The mesh primitive to read and convert</param>
        /// <param name="effect">The <see cref="Effect"/> used by the primitive.</param>
        protected abstract void WriteMeshPrimitive(MeshPrimitiveReader srcPrimitive, Effect effect);

        #endregion
    }


    class DefaultMeshesFactory : MeshesFactory
    {
        #region lifecycle

        public DefaultMeshesFactory(GraphicsDevice device) : base(device) { }

        #endregion        

        #region meshes creation

        protected override void WriteMeshPrimitive(MeshPrimitiveReader srcPrimitive, Effect effect)
        {
            if (srcPrimitive.IsSkinned) WriteMeshPrimitive<VertexSkinned>(effect, srcPrimitive);
            else WriteMeshPrimitive<VertexPositionNormalTexture>(effect, srcPrimitive);
        }

        #endregion        
    }
}
