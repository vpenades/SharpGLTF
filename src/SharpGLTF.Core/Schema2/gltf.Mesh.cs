using System;
using System.Collections.Generic;
using System.Linq;

using SharpGLTF.Collections;

namespace SharpGLTF.Schema2
{
    [System.Diagnostics.DebuggerDisplay("{_DebuggerDisplay(),nq}")]
    [System.Diagnostics.DebuggerTypeProxy(typeof(Debug._MeshDebugProxy))]
    public sealed partial class Mesh
    {
        #region debug

        private String _DebuggerDisplay()
        {
            var txt = $"Mesh[{this.LogicalIndex}]";

            if (!string.IsNullOrWhiteSpace(this.Name)) txt += $" {this.Name}";

            txt += $" Primitives[{this.Primitives.Count}]";

            return txt;
        }

        #endregion

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

        public IReadOnlyList<Single> MorphWeights => GetMorphWeights();

        public bool AllPrimitivesHaveJoints => Primitives.All(p => p.GetVertexAccessor("JOINTS_0") != null);

        #endregion

        #region API

        public IReadOnlyList<Single> GetMorphWeights()
        {
            if (_weights == null || _weights.Count == 0) return Array.Empty<Single>();

            return _weights.Select(item => (float)item).ToList();
        }

        public void SetMorphWeights(Transforms.SparseWeight8 weights)
        {
            int count = _primitives.Max(item => item.MorphTargetsCount);

            _weights.SetMorphWeights(count, weights);
        }

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

        #endregion

        #region Validation

        protected override void OnValidateReferences(Validation.ValidationContext validate)
        {
            validate
                .IsGreater(nameof(Primitives), this.Primitives.Count, 0)
                .IsSetCollection(nameof(Primitives), _primitives);

            base.OnValidateReferences(validate);
        }

        protected override void OnValidateContent(Validation.ValidationContext validate)
        {
            if (_weights.Count > 0)
            {
                foreach (var p in this.Primitives)
                {
                    validate.GetContext(p).AreEqual("MorphTargetsCount", p.MorphTargetsCount, _weights.Count);
                }
            }

            base.OnValidateContent(validate);
        }

        #endregion
    }

    public partial class ModelRoot
    {
        /// <summary>
        /// Creates a new <see cref="Mesh"/> instance
        /// and appends it to <see cref="ModelRoot.LogicalMeshes"/>.
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
