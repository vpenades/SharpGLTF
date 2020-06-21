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
        public Node VisualRoot => _FindVisualRootNode(this);

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
        /// Gets the Morph Weights of this <see cref="Node"/>.
        /// </summary>
        public IReadOnlyList<Single> MorphWeights => GetMorphWeights();

        #endregion

        #region properties - transform

        #pragma warning disable CA1721 // Property names should not match get methods

        /// <summary>
        /// Gets or sets the local Scale, Rotation and Translation of this <see cref="Node"/>.
        /// </summary>
        public Transforms.AffineTransform LocalTransform
        {
            get => Transforms.AffineTransform.CreateFromAny(_matrix, _scale, _rotation, _translation);
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

        #pragma warning restore CA1721 // Property names should not match get methods

        /// <summary>
        /// Gets or sets the local transform <see cref="Matrix4x4"/> of this <see cref="Node"/>.
        /// </summary>
        public Matrix4x4 LocalMatrix
        {
            get => Transforms.Matrix4x4Factory.CreateFrom(_matrix, _scale, _rotation, _translation);
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

        #pragma warning disable CA1721 // Property names should not match get methods

        /// <summary>
        /// Gets or sets the world transform <see cref="Matrix4x4"/> of this <see cref="Node"/>.
        /// </summary>
        public Matrix4x4 WorldMatrix
        {
            get
            {
                var vs = VisualParent;
                return vs == null ? LocalMatrix : Transforms.Matrix4x4Factory.LocalToWorld(vs.WorldMatrix, LocalMatrix);
            }
            set
            {
                var vs = VisualParent;
                LocalMatrix = vs == null ? value : Transforms.Matrix4x4Factory.WorldToLocal(vs.WorldMatrix, value);
            }
        }

        internal Transforms.Matrix4x4Double LocalMatrixPrecise
        {
            get
            {
                if (_matrix.HasValue) return new Transforms.Matrix4x4Double(_matrix.Value);

                var s = _scale ?? Vector3.One;
                var r = _rotation ?? Quaternion.Identity;
                var t = _translation ?? Vector3.Zero;

                return
                    Transforms.Matrix4x4Double.CreateScale(s.X, s.Y, s.Z)
                    *
                    Transforms.Matrix4x4Double.CreateFromQuaternion(r.Sanitized())
                    *
                    Transforms.Matrix4x4Double.CreateTranslation(t.X, t.Y, t.Z);
            }
        }

        internal Transforms.Matrix4x4Double WorldMatrixPrecise
        {
            get
            {
                var vs = this.VisualParent;
                return vs == null ? LocalMatrixPrecise : LocalMatrixPrecise * vs.WorldMatrixPrecise;
            }
        }

        #pragma warning restore CA1721 // Property names should not match get methods

        /// <summary>
        /// Gets a value indicating whether this transform is affected by any animation.
        /// </summary>
        public bool IsTransformAnimated
        {
            get
            {
                var root = this.LogicalParent;

                if (root.LogicalAnimations.Count == 0) return false;

                // check if it's affected by animations.
                if (root.LogicalAnimations.Any(anim => anim.FindScaleSampler(this) != null)) return true;
                if (root.LogicalAnimations.Any(anim => anim.FindRotationSampler(this) != null)) return true;
                if (root.LogicalAnimations.Any(anim => anim.FindTranslationSampler(this) != null)) return true;

                return false;
            }
        }

        internal bool IsTransformDecomposed
        {
            get
            {
                if (_scale.HasValue) return true;
                if (_rotation.HasValue) return true;
                if (_translation.HasValue) return true;
                return false;
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
            return vs == null ? lm : Transforms.Matrix4x4Factory.LocalToWorld(vs.GetWorldMatrix(animation, time), lm);
        }

        public IReadOnlyList<Single> GetMorphWeights()
        {
            if (!_mesh.HasValue) return Array.Empty<Single>();

            if (_weights == null || _weights.Count == 0) return Mesh.MorphWeights;

            return _weights.Select(item => (float)item).ToList();
        }

        public void SetMorphWeights(Transforms.SparseWeight8 weights)
        {
            Guard.IsTrue(_mesh.HasValue, nameof(weights), "Nodes with no mesh cannot have morph weights");

            int count = Mesh.Primitives.Max(item => item.MorphTargetsCount);

            _weights.SetMorphWeights(count, weights);
        }

        #endregion

        #region API - hierarchy

        internal static Node _FindVisualRootNode(Node childNode)
        {
            while (true)
            {
                var parent = childNode.VisualParent;
                if (parent == null) return childNode;
                childNode = parent;
            }
        }

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

        internal void _SetVisualParent(Node parentNode)
        {
            Guard.MustShareLogicalParent(this, parentNode, nameof(parentNode));
            Guard.IsFalse(_ContainsVisualNode(parentNode, true), nameof(parentNode));

            // remove from all the scenes
            foreach (var s in LogicalParent.LogicalScenes)
            {
                s._RemoveVisualNode(this);
            }

            // remove from current parent
            _RemoveFromVisualParent();

            // add to parent node.
            parentNode._children.Add(this.LogicalIndex);
        }

        internal void _RemoveFromVisualParent()
        {
            var oldParent = this.VisualParent;
            if (oldParent == null) return;

            oldParent._children.Remove(this.LogicalIndex);
        }

        #endregion

        #region validation

        protected override void OnValidateReferences(Validation.ValidationContext validate)
        {
            base.OnValidateReferences(validate);

            // check out of range indices
            foreach (var idx in this._children)
            {
                validate.IsNullOrIndex(nameof(VisualChildren), idx, this.LogicalParent.LogicalNodes);
            }

            validate
                .IsNullOrIndex(nameof(Mesh), _mesh, this.LogicalParent.LogicalMeshes)
                .IsNullOrIndex(nameof(Skin), _skin, this.LogicalParent.LogicalSkins)
                .IsNullOrIndex(nameof(Camera), _camera, this.LogicalParent.LogicalCameras);
        }

        internal static void _ValidateParentHierarchy(IEnumerable<Node> nodes, Validation.ValidationContext validate)
        {
            var childRefs = new List<int>();

            // in order to check if one node has more than two parents we simply need to check if its
            // index is repeated among the combined list of children indices.

            foreach (var n in nodes)
            {
                childRefs.AddRange(n._children);
            }

            var uniqueCount = childRefs.Distinct().Count();

            if (uniqueCount == childRefs.Count) return;

            var dupes = childRefs
                .GroupBy(item => item)
                .Where(group => group.Count() > 1);

            foreach (var dupe in dupes)
            {
                validate._LinkThrow($"LogicalNode[{dupe.Key}]", "has more than one parent.");
            }
        }

        protected override void OnValidateContent(Validation.ValidationContext validate)
        {
            base.OnValidateContent(validate);

            _ValidateChildrenHierarchy(validate);
            _ValidateTransforms(validate);
            _ValidateMeshAndSkin(validate, Mesh, Skin, _weights);
        }

        private void _ValidateChildrenHierarchy(Validation.ValidationContext validate)
        {
            var allNodes = this.LogicalParent.LogicalNodes;

            var nodePath = new Stack<int>();

            void checkTree(Node n)
            {
                var nidx = n.LogicalIndex;

                if (nodePath.Contains(nidx)) validate._LinkThrow($"LogicalNode[{nidx}]", "has a circular reference.");

                nodePath.Push(nidx);

                foreach (var cidx in n._children) checkTree(allNodes[cidx]);

                nodePath.Pop();
            }

            checkTree(this);
        }

        private void _ValidateTransforms(Validation.ValidationContext validate)
        {
            if (_matrix.HasValue)
            {
                validate
                    .IsUndefined(nameof(_scale), _scale)
                    .IsUndefined(nameof(_rotation), _rotation)
                    .IsUndefined(nameof(_translation), _translation);
            }

            validate
                .IsPosition("Scale", _scale.AsValue(Vector3.One))
                .IsRotation("Rotation", _rotation.AsValue(Quaternion.Identity))
                .IsPosition("Translation", _translation.AsValue(Vector3.Zero))
                .IsNullOrMatrix("Rotation", _matrix, true, true);
        }

        private static void _ValidateMeshAndSkin(Validation.ValidationContext validate, Mesh mesh, Skin skin, List<Double> weights)
        {
            var wcount = weights == null ? 0 : weights.Count;

            if (mesh == null && skin == null && wcount == 0) return;

            if (skin != null)
            {
                validate.IsDefined("Mesh", mesh);
                validate.IsTrue("Mesh", mesh.AllPrimitivesHaveJoints, "Node has skin defined, but mesh has no joints data.");
            }

            if (mesh == null)
            {
                validate.AreEqual("weights", wcount, 0); // , "Morph weights require a mesh."
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

        /// <summary>
        /// Applies a world transform to all the scenes of the model.
        /// </summary>
        /// <param name="basisTransform">The transform to apply.</param>
        /// <param name="basisNodeName">The name of the new root node, if it needs to be created.</param>
        /// <remarks>
        /// This method is appropiate to apply a general axis or scale change to the whole model.
        /// Animations are preserved by encapsulating animated nodes inside a master basis transform node.
        /// Meanwhile, unanimated nodes are transformed directly.
        /// If the determinant of <paramref name="basisTransform"/> is negative, the face culling should be
        /// flipped when rendering.
        /// </remarks>
        public void ApplyBasisTransform(Matrix4x4 basisTransform, string basisNodeName = "BasisTransform")
        {
            // TODO: nameless nodes with decomposed transform
            // could be considered intrinsic.

            Guard.IsTrue(basisTransform.IsValid(true, true), nameof(basisTransform));

            // gather all root nodes:
            var rootNodes = this.LogicalNodes
                .Where(item => item.VisualRoot == item)
                .ToList();

            // find all the nodes that cannot be modified
            bool isSensible(Node node)
            {
                if (node.IsTransformAnimated) return true;
                if (node.IsTransformDecomposed) return true;
                return false;
            }

            var sensibleNodes = rootNodes
                .Where(item => isSensible(item))
                .ToList();

            // find all the nodes that we can change their transform matrix safely.
            var intrinsicNodes = rootNodes
                .Except(sensibleNodes)
                .ToList();

            // apply the transform to the nodes that are safe to change.
            foreach (var n in intrinsicNodes)
            {
                n.LocalMatrix *= basisTransform;
            }

            if (sensibleNodes.Count == 0) return;

            // create a proxy node to be used as the root for all sensible nodes.
            var basisNode = CreateLogicalNode();
            basisNode.Name = basisNodeName;
            basisNode.LocalMatrix = basisTransform;

            // add the basis node to all scenes
            foreach (var scene in this.LogicalScenes)
            {
                scene._UseVisualNode(basisNode);
            }

            // assign all the sensible nodes to the basis node.
            foreach (var n in sensibleNodes)
            {
                n._SetVisualParent(basisNode);
            }
        }
    }
}
