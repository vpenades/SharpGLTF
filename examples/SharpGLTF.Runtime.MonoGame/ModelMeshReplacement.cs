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
    sealed class ModelMeshPartReplacement
    {
        internal ModelMeshReplacement _Parent;

        private Effect _Effect;

        public Effect Effect
        {
            get => _Effect;
            set
            {
                if (_Effect == value) return;
                _Effect = value;
                _Parent.InvalidateEffectsCollection(); // if we change this property, we need to invalidate the parent's effect collection.
            }
        }

        public IndexBuffer IndexBuffer { get; set; }

        public int NumVertices { get; set; }

        public int PrimitiveCount { get; set; }

        public int StartIndex { get; set; }

        public object Tag { get; set; }

        public VertexBuffer VertexBuffer { get; set; }

        public int VertexOffset { get; set; }
    }

    /// <summary>
    /// Replaces <see cref="ModelMesh"/>
    /// </summary>
    sealed class ModelMeshReplacement
    {
        private GraphicsDevice graphicsDevice;

        public ModelMeshReplacement(GraphicsDevice graphicsDevice, List<ModelMeshPartReplacement> parts)
        {
            // TODO: Complete member initialization
            this.graphicsDevice = graphicsDevice;

            MeshParts = parts.ToArray();

            foreach (var mp in MeshParts) mp._Parent = this;
        }

        private IReadOnlyList<Effect> _Effects;

        public IReadOnlyCollection<Effect> Effects
        {
            get
            {
                if (_Effects != null) return _Effects;

                // effects collection has changed since last call, so we reconstruct the collection.
                _Effects = MeshParts
                    .Select(item => item.Effect)
                    .Distinct()
                    .ToArray();

                return _Effects;
            }
        }

        public Microsoft.Xna.Framework.BoundingSphere BoundingSphere { get; set; }

        public IList<ModelMeshPartReplacement> MeshParts { get; set; }

        public string Name { get; set; }

        public ModelBone ParentBone { get; set; }

        public object Tag { get; set; }

        internal void InvalidateEffectsCollection() { _Effects = null; }

        public void Draw()
        {
            for (int i = 0; i < MeshParts.Count; i++)
            {
                var part = MeshParts[i];
                var effect = part.Effect;

                if (part.PrimitiveCount > 0)
                {
                    this.graphicsDevice.SetVertexBuffer(part.VertexBuffer);
                    this.graphicsDevice.Indices = part.IndexBuffer;

                    for (int j = 0; j < effect.CurrentTechnique.Passes.Count; j++)
                    {
                        effect.CurrentTechnique.Passes[j].Apply();
                        graphicsDevice.DrawIndexedPrimitives(PrimitiveType.TriangleList, part.VertexOffset, part.StartIndex, part.PrimitiveCount);
                    }
                }
            }
        }
    }
}
