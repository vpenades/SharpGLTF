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

    [System.Diagnostics.DebuggerDisplay("{_GetDebuggerDisplay(),nq}")]
    public sealed partial class Node : IVisualNodeContainer
    {
        #region debug

        private string _GetDebuggerDisplay()
        {
            var txt = $"Node[{LogicalIndex}ᴵᵈˣ]";

            if (!string.IsNullOrWhiteSpace(this.Name)) txt += $" {this.Name}";

            if (_matrix.HasValue)
            {
                if (_matrix.Value != Matrix4x4.Identity)
                {
                    var xform = this.LocalTransform;
                    if (xform.Scale != Vector3.One) txt += $" 𝐒:{xform.Scale}";
                    if (xform.Rotation != Quaternion.Identity) txt += $" 𝐑:{xform.Rotation}";
                    if (xform.Translation != Vector3.Zero) txt += $" 𝚻:{xform.Translation}";
                }
            }
            else
            {
                if (_scale.HasValue) txt += $" 𝐒:{_scale.Value}";
                if (_rotation.HasValue) txt += $" 𝐑:{_rotation.Value}";
                if (_translation.HasValue) txt += $" 𝚻:{_translation.Value}";
            }

            if (this.Mesh != null)
            {
                if (this.Skin != null) txt += $" ⇨ Skin[{this.Skin.LogicalIndex}ᴵᵈˣ]";
                txt += $" ⇨ Mesh[{this.Mesh.LogicalIndex}ᴵᵈˣ]";
            }

            if (this.VisualChildren.Any())
            {
                txt += $" | Children[{this.VisualChildren.Count()}]";
            }

            return txt;
        }

        #endregion

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
        /// Gets the collection of <see cref="Scene"/> instances that reference this <see cref="Node"/>.
        /// </summary>
        public IEnumerable<Scene> VisualScenes
        {
            get
            {
                var rootNode = this.VisualRoot;

                return LogicalParent
                    .LogicalScenes
                    .Where(item => item._ContainsVisualNode(rootNode, false));
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

        #region properties - content

        /// <summary>
        /// Gets or sets the <see cref="Schema2.Camera"/> of this <see cref="Node"/>.
        /// </summary>
        public Camera Camera
        {
            get => this._camera.HasValue ? this.LogicalParent.LogicalCameras[this._camera.Value] : null;
            set
            {
                if (value == null) { this._camera = null; return; }

                Guard.MustShareLogicalParent(this.LogicalParent, nameof(this.LogicalParent), value, nameof(value));

                this._camera = value.LogicalIndex;
            }
        }

        /// <summary>
        /// Gets or sets the <see cref="Schema2.Mesh"/> of this <see cref="Node"/>.
        /// </summary>
        public Mesh Mesh
        {
            get => this._mesh.HasValue ? this.LogicalParent.LogicalMeshes[this._mesh.Value] : null;
            set
            {
                if (value == null) { this._mesh = null; return; }

                Guard.MustShareLogicalParent(this.LogicalParent, nameof(this.LogicalParent), value, nameof(value));

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

                Guard.MustShareLogicalParent(this.LogicalParent, nameof(this.LogicalParent), value, nameof(value));

                Guard.IsFalse(_matrix.HasValue, _NOTRANSFORMMESSAGE);
                Guard.IsFalse(_scale.HasValue, _NOTRANSFORMMESSAGE);
                Guard.IsFalse(_rotation.HasValue, _NOTRANSFORMMESSAGE);
                Guard.IsFalse(_translation.HasValue, _NOTRANSFORMMESSAGE);

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
                if (value != null) _weights.AddRange(value.Select(item => (Double)item));
            }
        }

        #endregion

        #region properties - transform

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

        #endregion

        #region API - Transforms

        public Transforms.AffineTransform GetLocalTransform(Animation animation, float time)
        {
            if (animation == null) return this.LocalTransform;

            return animation.GetLocalTransform(this, time);
        }

        public Matrix4x4 GetWorldMatrix(Animation animation, float time)
        {
            if (animation == null) return this.WorldMatrix;

            var vs = VisualParent;
            var lm = GetLocalTransform(animation, time).Matrix;
            return vs == null ? lm : Transforms.AffineTransform.LocalToWorld(vs.GetWorldMatrix(animation, time), lm);
        }

        #endregion

        #region API - hierarchy

        /// <summary>
        /// Creates a new <see cref="Node"/> instance,
        /// adds it to <see cref="ModelRoot.LogicalNodes"/>
        /// and references it as a child in the current graph.
        /// </summary>
        /// <param name="name">The name of the instance.</param>
        /// <returns>A <see cref="Node"/> instance.</returns>
        public Node CreateNode(string name = null)
        {
            var node = this.LogicalParent._CreateVisualNode(this._children);
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
            if (container == null) yield break;

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

        protected override void OnValidateReferences(Validation.ValidationContext result)
        {
            base.OnValidateReferences(result);

            // check out of range indices
            foreach (var idx in this._children)
            {
                result.CheckArrayIndexAccess(nameof(VisualChildren), idx, this.LogicalParent.LogicalNodes);
            }

            result.CheckArrayIndexAccess(nameof(Mesh), _mesh, this.LogicalParent.LogicalMeshes);
            result.CheckArrayIndexAccess(nameof(Skin), _skin, this.LogicalParent.LogicalSkins);
            result.CheckArrayIndexAccess(nameof(Camera), _camera, this.LogicalParent.LogicalCameras);
        }

        protected override void OnValidate(Validation.ValidationContext result)
        {
            base.OnValidate(result);

            _ValidateHierarchy(result);
            _ValidateTransforms(result);
            _ValidateMeshAndSkin(result, Mesh, Skin);
        }

        private void _ValidateHierarchy(Validation.ValidationContext result)
        {
            var allNodes = this.LogicalParent.LogicalNodes;

            var thisIndex = this.LogicalIndex;

            var pidx = thisIndex;

            while (true)
            {
                result = result.GetContext(result.Root.LogicalNodes[pidx]);

                // every node must have 0 or 1 parents.

                var allParents = allNodes
                    .Where(n => n._HasVisualChild(pidx))
                    .ToList();

                if (allParents.Count == 0) break; // we're already root

                if (allParents.Count > 1)
                {
                    var parents = string.Join(" ", allParents);

                    result.AddLinkError($"is child of nodes {parents}. A node can only have one parent.");
                    break;
                }

                if (allParents[0].LogicalIndex == pidx)
                {
                    result.AddLinkError("is a part of a node loop.");
                    break;
                }

                pidx = allParents[0].LogicalIndex;
            }
        }

        private void _ValidateTransforms(Validation.ValidationContext result)
        {
            result.CheckIsFinite("Scale", _scale);
            result.CheckIsFinite("Rotation", _rotation);
            result.CheckIsFinite("Translation", _translation);
            result.CheckIsMatrix("Matrix", _matrix);
        }

        private static void _ValidateMeshAndSkin(Validation.ValidationContext result, Mesh mesh, Skin skin)
        {
            if (mesh == null && skin == null) return;

            if (mesh != null)
            {
                if (skin == null && mesh.AllPrimitivesHaveJoints) result.AddLinkWarning("Skin", "Node uses skinned mesh, but has no skin defined.");
            }

            if (skin != null)
            {
                if (mesh == null || !mesh.AllPrimitivesHaveJoints) result.AddLinkError("Mesh", "Node has skin defined, but mesh has no joints data.");
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

        public Node CreateLogicalNode()
        {
            var n = new Node();
            _nodes.Add(n);
            return n;
        }

        internal Node _CreateVisualNode(IList<int> parentChildren)
        {
            var n = CreateLogicalNode();
            parentChildren.Add(n.LogicalIndex);
            return n;
        }
    }
}
