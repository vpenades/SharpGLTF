using System;
using System.Collections.Generic;
using System.Linq;

using Microsoft.Xna.Framework.Graphics;

using SharpGLTF.Schema2;

using SRCMESH = SharpGLTF.Schema2.Mesh;
using SRCPRIM = SharpGLTF.Schema2.MeshPrimitive;
using SRCMATERIAL = SharpGLTF.Schema2.Material;

using MODELMESH = SharpGLTF.Runtime.RuntimeModelMesh;

namespace SharpGLTF.Runtime
{
    /// <summary>
    /// Helper class used to import a glTF meshes and materials into MonoGame
    /// </summary>
    public abstract class LoaderContext
    {
        #region lifecycle

        public LoaderContext(GraphicsDevice device)
        {
            _Device = device;
        }

        #endregion

        #region data

        private GraphicsDevice _Device;

        private GraphicsResourceTracker _Disposables;
        
        private EffectsFactory _MatFactory;

        // gathers all meshes using shared vertex and index buffers whenever possible.
        private MeshPrimitiveWriter _MeshWriter;
        private int _CurrentMeshIndex;

        // used as a container to a default material;
        private SharpGLTF.Schema2.ModelRoot _DummyModel; 

        #endregion

        #region properties

        protected GraphicsDevice Device => _Device;

        internal IReadOnlyList<GraphicsResource> Disposables => _Disposables.Disposables;

        #endregion

        #region API

        internal void Reset()
        {
            _Disposables = new GraphicsResourceTracker();
            _MatFactory = new EffectsFactory(_Device, _Disposables);
            _MeshWriter = new MeshPrimitiveWriter();
        }

        #endregion

        #region Mesh API

        internal void _WriteMesh(SRCMESH srcMesh)
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
            if (srcMaterial == null) srcMaterial = GetDefaultMaterial();

            var effect = _MatFactory.GetMaterial(srcMaterial, srcPrim.IsSkinned);

            if (effect == null)
            {
                effect = CreateEffect(srcMaterial, srcPrim.IsSkinned);
                _MatFactory.Register(srcMaterial, srcPrim.IsSkinned, effect);
            }

            WriteMeshPrimitive(srcPrim, effect);
        }        

        protected abstract void WriteMeshPrimitive(MeshPrimitiveReader srcPrimitive, Effect effect);

        protected void WriteMeshPrimitive<TVertex>(Effect effect, MeshPrimitiveReader primitive)
            where TVertex : unmanaged, IVertexType
        {
            _MeshWriter.WriteMeshPrimitive<TVertex>(_CurrentMeshIndex, effect, primitive);
        }

        #endregion

        #region EFfects API

        /// <summary>
        /// Called when finding a new material that needs to be converted to an Effect.
        /// </summary>
        /// <param name="srcMaterial">The material to convert.</param>
        /// <param name="isSkinned">Indicates that the material is used in a skinned mesh.</param>
        /// <returns>An effect to be used in place of <paramref name="srcMaterial"/>. </returns>
        protected abstract Effect CreateEffect(Material srcMaterial, bool isSkinned);

        protected virtual Texture2D UseTexture(MaterialChannel? channel, string name)
        {
            return _MatFactory.UseTexture(channel, name);
        }

        #endregion

        #region resources API

        internal IReadOnlyDictionary<int, MODELMESH> CreateRuntimeModels()
        {
            return _MeshWriter.GetRuntimeMeshes(_Device, _Disposables);
        }

        private Material GetDefaultMaterial()
        {
            if (_DummyModel != null)
            {
                _DummyModel = ModelRoot.CreateModel();
                _DummyModel.CreateMaterial("Default");
            }
            
            return _DummyModel.LogicalMaterials[0];
        }           

        #endregion
    }

    
}
