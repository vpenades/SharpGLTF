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

        public IReadOnlyList<Single> MorphWeights => _weights.Count == 0 ? null : _weights.Select(item => (Single)item).ToList();

        public bool AllPrimitivesHaveJoints => Primitives.All(p => p.GetVertexAccessor("JOINTS_0") != null);

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

        protected override void OnValidateReferences(Validation.ValidationContext result)
        {
            base.OnValidateReferences(result);

            result.CheckLinksInCollection("Primitives", _primitives);

            foreach (var p in this.Primitives) p.ValidateReferences(result);
        }

        protected override void OnValidate(Validation.ValidationContext result)
        {
            base.OnValidate(result);

            foreach (var p in this.Primitives) p.Validate(result);

            var morphTargetsCount = this.Primitives
                .Select(item => item.MorphTargetsCount)
                .Distinct();

            if (morphTargetsCount.Count() != 1) result.AddSemanticError("Count", "All primitives must have the same number of morph targets.");

            if (_weights.Count != 0 && morphTargetsCount.First() != _weights.Count) result.AddSemanticError("Weights", $"The length of weights array ({_weights.Count}) does not match the number of morph targets({morphTargetsCount.First()}).");
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
