using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace SharpGLTF.Schema2
{
    public interface IVisualNodeContainer
    {
        IEnumerable<Node> VisualChildren { get; }

        Node CreateNode(string name = null);
    }

    [System.Diagnostics.DebuggerDisplay("Node[{LogicalIndex}] {Name} SkinJoint:{IsSkinJoint} T:{LocalTransform.Translation.X} {LocalTransform.Translation.Y} {LocalTransform.Translation.Z}")]
    public sealed partial class Node : IVisualNodeContainer
    {
        #region constants

        private const string _NOTRANSFORMMESSAGE = "Node instances with a Skin must not contain spatial transformations.";

        #endregion

        #region lifecycle

        internal Node()
        {
            _children = new List<int>();
            _weights = new List<double>();
        }

        #endregion

        #region properties - hierarchy

        /// <summary>
        /// Gets the zero-based index of this <see cref="Node"/> at <see cref="ModelRoot.LogicalNodes"/>
        /// </summary>
        public int LogicalIndex => this.LogicalParent.LogicalNodes.IndexOfReference(this);

        /// <summary>
        /// Gets the visual parent <see cref="Node"/> instance that contains this <see cref="Node"/>.
        /// </summary>
        public Node VisualParent => this.LogicalParent._FindVisualParentNode(this);

        /// <summary>
        /// Gets the visual root <see cref="Node"/> instance that contains this <see cref="Node"/>.
        /// </summary>
        public Node VisualRoot => this.LogicalParent._FindVisualRootNode(this);

        /// <summary>
        /// Gets the visual root <see cref="Scene"/> instance that contains this <see cref="Node"/>.
        /// </summary>
        public Scene VisualScene
        {
            get
            {
                var rootNode = this.VisualRoot;
                return LogicalParent.LogicalScenes.FirstOrDefault(item => item._ContainsVisualNode(rootNode, false));
            }
        }

        /// <summary>
        /// Gets the visual children <see cref="Node"/> instances contained in this <see cref="Node"/>.
        /// </summary>
        public IEnumerable<Node> VisualChildren => _GetVisualChildren();

        /// <summary>
        /// Gets a value indicating whether this node is used as a Bone joint in a <see cref="Skin"/>.
        /// </summary>
        public Boolean IsSkinJoint => Skin.FindSkinsUsingJoint(this).Any();

        /// <summary>
        /// Gets a value indicating whether this node is used as a Skeleton node in a <see cref="Skin"/>.
        /// </summary>
        public Boolean IsSkinSkeleton => Skin.FindSkinsUsingSkeleton(this).Any();

        #endregion

        #region properties - transform

        /// <summary>
        /// Gets or sets the local transform <see cref="Matrix4x4"/> of this <see cref="Node"/>.
        /// </summary>
        public Matrix4x4 LocalMatrix
        {
            get => Transforms.AffineTransform.Evaluate(_matrix, _scale, _rotation, _translation);
            set
            {
                if (value == Matrix4x4.Identity)
                {
                    _matrix = null;
                }
                else
                {
                    Guard.IsFalse(this._skin.HasValue, _NOTRANSFORMMESSAGE);
                    _matrix = value;
                }

                _scale = null;
                _rotation = null;
                _translation = null;
            }
        }

        /// <summary>
        /// Gets or sets the local Scale, Rotation and Translation of this <see cref="Node"/>.
        /// </summary>
        public Transforms.AffineTransform LocalTransform
        {
            get => new Transforms.AffineTransform(_matrix, _scale, _rotation, _translation);
            set
            {
                Guard.IsFalse(this._skin.HasValue, _NOTRANSFORMMESSAGE);

                Guard.IsTrue(value.IsValid, nameof(value));

                _matrix = null;
                _scale = value.Scale.AsNullable(Vector3.One);
                _rotation = value.Rotation.Sanitized().AsNullable(Quaternion.Identity);
                _translation = value.Translation.AsNullable(Vector3.Zero);
            }
        }

        public Transforms.AffineTransform GetLocalTransform(Animation animation, float time)
        {
            if (animation == null) return this.LocalTransform;

            return animation.GetLocalTransform(this, time);
        }

        /// <summary>
        /// Gets or sets the world transform <see cref="Matrix4x4"/> of this <see cref="Node"/>.
        /// </summary>
        public Matrix4x4 WorldMatrix
        {
            get
            {
                var vs = VisualParent;
                return vs == null ? LocalMatrix : Transforms.AffineTransform.LocalToWorld(vs.WorldMatrix, LocalMatrix);
            }
            set
            {
                var vs = VisualParent;
                LocalMatrix = vs == null ? value : Transforms.AffineTransform.WorldToLocal(vs.WorldMatrix, value);
            }
        }

        public Matrix4x4 GetWorldMatrix(Animation animation, float time)
        {
            if (animation == null) return this.WorldMatrix;

            var vs = VisualParent;
            var lm = GetLocalTransform(animation, time).Matrix;
            return vs == null ? lm : Transforms.AffineTransform.LocalToWorld(vs.GetWorldMatrix(animation, time), lm);
        }

        /// <summary>
        /// Creates a <see cref="Transforms.ITransform"/> object, based on the current
        /// transform state, that can be used to transform the <see cref="Mesh"/>
        /// vertices to world space.
        /// </summary>
        /// <returns>A <see cref="Transforms.ITransform"/> object</returns>
        public Transforms.ITransform GetMeshWorldTransform() { return GetMeshWorldTransform(null, 0); }

        /// <summary>
        /// Creates a <see cref="Transforms.ITransform"/> object, based on the current
        /// transform state, and the given <see cref="Animation"/>, that can be used
        /// to transform the <see cref="Mesh"/> vertices to world space.
        /// </summary>
        /// <param name="animation">The <see cref="Animation"/> to use.</param>
        /// <param name="time">The time within <paramref name="animation"/>.</param>
        /// <returns>A <see cref="Transforms.ITransform"/> object</returns>
        public Transforms.ITransform GetMeshWorldTransform(Animation animation, float time)
        {
            var weights = animation == null ? this.MorphWeights : animation.GetMorphWeights(this, time);

            if (this.Skin == null) return new Transforms.StaticTransform(this.GetWorldMatrix(animation, time), weights);

            var jointXforms = new Matrix4x4[this.Skin.JointsCount];
            var invBindings = new Matrix4x4[this.Skin.JointsCount];

            for (int i = 0; i < this.Skin.JointsCount; ++i)
            {
                var j = this.Skin.GetJoint(i);
                jointXforms[i] = j.Key.GetWorldMatrix(animation, time);
                invBindings[i] = j.Value;
            }

            return new Transforms.SkinTransform(invBindings, jointXforms, weights);
        }

        #endregion

        #region properties - content

        /// <summary>
        /// Gets or sets the <see cref="Schema2.Mesh"/> of this <see cref="Node"/>.
        /// </summary>
        public Mesh Mesh
        {
            get => this._mesh.HasValue ? this.LogicalParent.LogicalMeshes[this._mesh.Value] : null;
            set
            {
                if (value == null) { this._mesh = null; return; }

                Guard.MustShareLogicalParent(this.LogicalParent, value, nameof(value));

                this._mesh = value.LogicalIndex;
            }
        }

        /// <summary>
        /// Gets or sets the <see cref="Schema2.Skin"/> of this <see cref="Node"/>.
        /// </summary>
        public Skin Skin
        {
            get => this._skin.HasValue ? this.LogicalParent.LogicalSkins[this._skin.Value] : null;
            set
            {
                if (value == null) { this._skin = null; return; }

                Guard.MustShareLogicalParent(this.LogicalParent, value, nameof(value));
                Guard.IsFalse(_matrix.HasValue, _NOTRANSFORMMESSAGE);
                Guard.IsFalse(_scale.HasValue, _NOTRANSFORMMESSAGE);
                Guard.IsFalse(_rotation.HasValue, _NOTRANSFORMMESSAGE);
                Guard.IsFalse(_translation.HasValue, _NOTRANSFORMMESSAGE);
                // Todo: guard against animations.

                this._skin = value.LogicalIndex;
            }
        }

        /// <summary>
        /// Gets or sets the Morph Weights of this <see cref="Node"/>.
        /// </summary>
        public IReadOnlyList<Single> MorphWeights
        {
            get => _weights.Count == 0 ? Mesh?.MorphWeights : _weights.Select(item => (float)item).ToList();
            set
            {
                _weights.Clear();
                _weights.AddRange(value.Select(item => (Double)item));
            }
        }

        #endregion

        #region API

        /// <summary>
        /// Creates a new <see cref="Node"/> instance,
        /// adds it to <see cref="ModelRoot.LogicalNodes"/>
        /// and references it as a child in the current graph.
        /// </summary>
        /// <param name="name">The name of the instance.</param>
        /// <returns>A <see cref="Node"/> instance.</returns>
        public Node CreateNode(string name = null)
        {
            var node = this.LogicalParent._CreateLogicalNode(this._children);
            node.Name = name;
            return node;
        }

        /// <summary>
        /// Returns all the <see cref="Node"/> instances of a visual hierarchy as a flattened list.
        /// </summary>
        /// <param name="container">A <see cref="IVisualNodeContainer"/> instance.</param>
        /// <returns>A collection of <see cref="Node"/> instances.</returns>
        public static IEnumerable<Node> Flatten(IVisualNodeContainer container)
        {
            if (container is Node n) yield return n;

            foreach (var c in container.VisualChildren)
            {
                var cc = Flatten(c);

                foreach (var ccc in cc) yield return ccc;
            }
        }

        /// <summary>
        /// Gets a collection of <see cref="Node"/> instances using <paramref name="mesh"/>.
        /// </summary>
        /// <param name="mesh">A <see cref="Mesh"/> instance.</param>
        /// <returns>A collection of <see cref="Node"/> instances.</returns>
        public static IEnumerable<Node> FindNodesUsingMesh(Mesh mesh)
        {
            if (mesh == null) return Enumerable.Empty<Node>();

            var meshIdx = mesh.LogicalIndex;

            return mesh.LogicalParent
                .LogicalNodes
                .Where(item => item._mesh.AsValue(int.MinValue) == meshIdx);
        }

        /// <summary>
        /// Gets a collection of <see cref="Node"/> instances using <paramref name="skin"/>.
        /// </summary>
        /// <param name="skin">A <see cref="Skin"/> instance.</param>
        /// <returns>A collection of <see cref="Node"/> instances.</returns>
        public static IEnumerable<Node> FindNodesUsingSkin(Skin skin)
        {
            if (skin == null) return Enumerable.Empty<Node>();

            var meshIdx = skin.LogicalIndex;

            return skin.LogicalParent
                .LogicalNodes
                .Where(item => item._skin.AsValue(int.MinValue) == meshIdx);
        }

        internal bool _ContainsVisualNode(Node node, bool recursive)
        {
            Guard.MustShareLogicalParent(this, node, nameof(node));

            if (!recursive) return VisualChildren.Any(item => item == node);

            return VisualChildren.Any(item => item == node || item._ContainsVisualNode(node, recursive));
        }

        internal bool _HasVisualChild(int nodeIndex) { return _children.Contains(nodeIndex); }

        internal IEnumerable<Node> _GetVisualChildren()
        {
            var allChildren = _children.Select(idx => LogicalParent.LogicalNodes[idx]);

            return allChildren;
        }

        #endregion

        #region validation

        internal override void Validate(Validation.ValidationContext result)
        {
            base.Validate(result);

            // check out of range indices
            foreach (var idx in this._children)
            {
                if (idx < 0 || idx >= this.LogicalParent.LogicalNodes.Count) result.AddError(this, $"references invalid Node[{idx}]");
            }

            // check duplicated indices
            if (this._children.Distinct().Count() != this._children.Count) result.AddError(this, "has duplicated node references");

            // check self references
            if (this._children.Contains(this.LogicalIndex)) result.AddError(this, "has self references");

            // check circular references
            var p = this;
            while (true)
            {
                p = p.VisualParent;
                if (p == null) break;
                if (p.LogicalIndex == this.LogicalIndex)
                {
                    result.AddError(this, "has a circular reference");
                    break;
                }
            }

            // check Transforms (out or range, NaN, etc)

            // check morph weights

            if (this._skin != null)
            {
                if (this._mesh == null)
                {
                    result.AddError(this, "Found a Skin, but Mesh is missing");
                }
                else
                {
                    this.Mesh.ValidateSkinning(result, this.Skin.JointsCount);
                }
            }
        }

        #endregion
    }

    public partial class ModelRoot
    {
        internal Node _FindVisualParentNode(Node childNode)
        {
            var childIdx = _nodes.IndexOf(childNode);
            if (childIdx < 0) return null;

            // find the logical owner
            return _nodes.FirstOrDefault(item => item._HasVisualChild(childIdx));
        }

        internal Node _FindVisualRootNode(Node childNode)
        {
            while (true)
            {
                var parent = childNode.VisualParent;
                if (parent == null) return childNode;
                childNode = parent;
            }
        }

        internal Node _CreateLogicalNode()
        {
            var n = new Node();
            _nodes.Add(n);
            return n;
        }

        internal Node _CreateLogicalNode(IList<int> children)
        {
            var n = _CreateLogicalNode();
            children.Add(n.LogicalIndex);
            return n;
        }
    }
}
