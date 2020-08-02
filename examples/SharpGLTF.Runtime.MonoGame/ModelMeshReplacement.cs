using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework.Graphics;

namespace SharpGLTF.Runtime
{
    /// <summary>
    /// Replaces <see cref="ModelMeshPart"/>.
    /// </summary>    
    sealed class RuntimeModelMeshPart
    {
        #region lifecycle

        internal RuntimeModelMeshPart(RuntimeModelMesh parent)
        {
            _Parent = parent;
        }

        #endregion

        #region data

        private readonly RuntimeModelMesh _Parent;

        private Effect _Effect;

        private IndexBuffer _IndexBuffer;
        private int _IndexOffset;
        private int _PrimitiveCount;        

        private VertexBuffer _VertexBuffer;
        private int _VertexOffset;
        private int _VertexCount;

        public object Tag { get; set; }

        #endregion

        #region properties

        public Effect Effect
        {
            get => _Effect;
            set
            {
                if (_Effect == value) return;
                _Effect = value;
                _Parent.InvalidateEffectCollection(); // if we change this property, we need to invalidate the parent's effect collection.
            }
        }

        public GraphicsDevice Device => _Parent._GraphicsDevice;

        #endregion

        #region API

        public void SetVertexBuffer(VertexBuffer vb, int offset, int count)
        {
            this._VertexBuffer = vb;
            this._VertexOffset = offset;
            this._VertexCount = count;            
        }

        public void SetIndexBuffer(IndexBuffer ib, int offset, int count)
        {
            this._IndexBuffer = ib;
            this._IndexOffset = offset;
            this._PrimitiveCount = count;            
        }

        public void Draw(GraphicsDevice device)
        {
            if (_PrimitiveCount > 0)
            {
                device.SetVertexBuffer(_VertexBuffer);
                device.Indices = _IndexBuffer;

                for (int j = 0; j < _Effect.CurrentTechnique.Passes.Count; j++)
                {
                    _Effect.CurrentTechnique.Passes[j].Apply();
                    device.DrawIndexedPrimitives(PrimitiveType.TriangleList, _VertexOffset, _IndexOffset, _PrimitiveCount);
                }
            }
        }

        #endregion
    }

    /// <summary>
    /// Replaces <see cref="ModelMesh"/>
    /// </summary>
    sealed class RuntimeModelMesh
    {
        #region lifecycle

        public RuntimeModelMesh(GraphicsDevice graphicsDevice)
        {
            this._GraphicsDevice = graphicsDevice;
        }

        #endregion

        #region data        

        internal GraphicsDevice _GraphicsDevice;

        private readonly List<RuntimeModelMeshPart> _Primitives = new List<RuntimeModelMeshPart>();

        private IReadOnlyList<Effect> _Effects;

        private Microsoft.Xna.Framework.BoundingSphere? _Sphere;

        #endregion

        #region  properties

        public IReadOnlyCollection<Effect> Effects
        {
            get
            {
                if (_Effects != null) return _Effects;

                // Create the shared effects collection on demand.

                _Effects = _Primitives
                    .Select(item => item.Effect)
                    .Distinct()
                    .ToArray();

                return _Effects;
            }
        }

        public Microsoft.Xna.Framework.BoundingSphere BoundingSphere
        {
            set => _Sphere = value;

            get
            {
                if (_Sphere.HasValue) return _Sphere.Value;

                return default;
            }
            
        }

        public IReadOnlyList<RuntimeModelMeshPart> MeshParts => _Primitives;

        public string Name { get; set; }

        public ModelBone ParentBone { get; set; }

        public object Tag { get; set; }

        #endregion

        #region API

        internal void InvalidateEffectCollection() { _Effects = null; }

        public RuntimeModelMeshPart CreateMeshPart()
        {
            var primitive = new RuntimeModelMeshPart(this);

            _Primitives.Add(primitive);
            InvalidateEffectCollection();

            _Sphere = null;

            return primitive;
        }

        public void Draw()
        {
            for (int i = 0; i < _Primitives.Count; i++)
            {
                _Primitives[i].Draw(_GraphicsDevice);
            }
        }

        #endregion
    }
}
