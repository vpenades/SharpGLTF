using System;
using System.Collections.Generic;
using System.Linq;

using SharpGLTF.Collections;

namespace SharpGLTF.Schema2
{
    [System.Diagnostics.DebuggerDisplay("Mesh[{LogicalIndex}] {Name}")]
    public sealed partial class Mesh
    {
        #region lifecycle

        internal Mesh()
        {
            _primitives = new ChildrenCollection<MeshPrimitive, Mesh>(this);
            _weights = new List<double>();
        }

        #endregion

        #region properties

        /// <summary>
        /// Gets the zero-based index of this <see cref="Mesh"/> at <see cref="ModelRoot.LogicalMeshes"/>
        /// </summary>
        public int LogicalIndex => this.LogicalParent.LogicalMeshes.IndexOfReference(this);

        public IEnumerable<Node> VisualParents => Node.FindNodesUsingMesh(this);

        public IReadOnlyList<MeshPrimitive> Primitives => _primitives;

        public IReadOnlyList<Single> MorphWeights => _weights.Count == 0 ? null : _weights.Select(item => (Single)item).ToList();

        #endregion

        #region API

        public void SetMorphWeights(Transforms.SparseWeight8 weights)
        {
            int count = _primitives.Max(item => item.MorphTargetsCount);

            while (_weights.Count > count) _weights.RemoveAt(_weights.Count - 1);
            while (_weights.Count < count) _weights.Add(0);

            if (_weights.Count > 0)
            {
                foreach (var kw in weights.GetIndexedWeights())
                {
                    _weights[kw.Item1] = kw.Item2;
                }
            }
        }

        /// <inheritdoc />
        protected override IEnumerable<ExtraProperties> GetLogicalChildren()
        {
            return base.GetLogicalChildren().Concat(_primitives);
        }

        /// <summary>
        /// Creates a new <see cref="MeshPrimitive"/> instance
        /// and adds it to the current <see cref="Mesh"/>.
        /// </summary>
        /// <returns>A <see cref="MeshPrimitive"/> instance.</returns>
        public MeshPrimitive CreatePrimitive()
        {
            var mp = new MeshPrimitive();

            _primitives.Add(mp);

            return mp;
        }

        internal override void Validate(Validation.ValidationContext result)
        {
            base.Validate(result);

            foreach (var p in this.Primitives)
            {
                p.Validate(result);
            }
        }

        internal void ValidateSkinning(Validation.ValidationContext result, int jointsCount)
        {
            foreach (var p in Primitives) p.ValidateSkinning(result, jointsCount);
        }

        #endregion
    }

    public partial class ModelRoot
    {
        /// <summary>
        /// Creates a new <see cref="Mesh"/> instance
        /// and adds it to <see cref="ModelRoot.LogicalMeshes"/>.
        /// </summary>
        /// <param name="name">The name of the instance.</param>
        /// <returns>A <see cref="Mesh"/> instance.</returns>
        public Mesh CreateMesh(string name = null)
        {
            var mesh = new Mesh();
            mesh.Name = name;

            this._meshes.Add(mesh);

            return mesh;
        }
    }
}
