using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

using TRANSFORM = SharpGLTF.Transforms.AffineTransform;

namespace SharpGLTF.Schema2
{
    /// <summary>
    /// Represents an abstract interface for a visual hierarchy.
    /// Implemented by <see cref="Node"/> and <see cref="Scene"/>.
    /// </summary>
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

            if (!string.IsNullOrWhiteSpace(this.Name)) txt += $" \"{this.Name}\" ";

            if (this.Mesh != null)
            {
                if (this.Skin != null) txt += $" / Skin[{this.Skin.LogicalIndex}ᴵᵈˣ]";
                txt += $" / Mesh[{this.Mesh.LogicalIndex}ᴵᵈˣ]";

                var instances = this.GetGpuInstancing();
                if (instances != null) txt += $" x {instances.Count} instances.";
            }

            if (this.VisualChildren.Any())
            {
                if (this.VisualChildren.Count() < 16)
                {
                    var indices = string.Join(", ", this.VisualChildren.Select(item => item.LogicalIndex));
                    txt += $" / Children[{indices}]";
                }
                else
                {
                    txt += $" / Children x {this.VisualChildren.Count()}";
                }
            }

            if (!this.LocalTransform.IsIdentity)
            {
                txt += " At " + this.LocalTransform.ToDebuggerDisplayString();
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
            
            _scale = null;
            _rotation = null;
            _translation = null;
            _matrix = null;
        }

        #endregion

        #region properties - hierarchy

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
        /// Gets or sets the world <see cref="Matrix4x4"/> of this <see cref="Node"/>.
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
                Transforms.Matrix4x4Factory.GuardMatrix(nameof(value), value, Transforms.Matrix4x4Factory.MatrixCheck.WorldTransform);

                var vs = VisualParent;
                LocalMatrix = vs == null ? value : Transforms.Matrix4x4Factory.WorldToLocal(vs.WorldMatrix, value);
            }
        }

        /// <summary>
        /// Gets or sets the local Scale, Rotation and Translation of this <see cref="Node"/>.
        /// </summary>
        public TRANSFORM LocalTransform
        {
            get => TRANSFORM.CreateFromAny(_matrix, _scale, _rotation, _translation);
            set => _SetLocalTransform(value);
        }

        /// <summary>
        /// Gets or sets the local transform <see cref="Matrix4x4"/> of this <see cref="Node"/>.
        /// </summary>
        public Matrix4x4 LocalMatrix
        {
            get => LocalTransform.Matrix;
            set => LocalTransform = value;
        }        

        /// <summary>
        /// Gets the local <see cref="Transforms.Matrix4x4Double"/> of this <see cref="Node"/>.
        /// </summary>
        /// <remarks>
        /// <para>
        /// This method is equivalent to <see cref="WorldMatrix"/>, but since the world matrix<br/>
        /// is calculated by concatenating all the local matrices in the hierarchy, there's chances<br/>
        /// to have some precission loss on large transform chains.
        /// </para>
        /// </remarks>
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

        /// <summary>
        /// Gets the world <see cref="Transforms.Matrix4x4Double"/> of this <see cref="Node"/>.
        /// </summary>
        /// <remarks>
        /// <para>
        /// This method is equivalent to <see cref="WorldMatrix"/>, but since the world matrix<br/>
        /// is calculated by concatenating all the local matrices in the hierarchy, there's chances<br/>
        /// to have some precission loss on large transform chains.
        /// </para>
        /// <para>
        /// Precission is specially relevant when calculating the Inverse Bind Matrix.
        /// </para>
        /// </remarks>
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

                // check if it's affected by any animation channel.

                static bool _isTransformPath(PropertyPath path)
                {
                    if (path == PropertyPath.scale) return true;
                    if (path == PropertyPath.rotation) return true;
                    if (path == PropertyPath.translation) return true;
                    // since morph weights are not part of the node transform, they're not handled here.
                    return false;
                }

                return root
                    .LogicalAnimations
                    .SelectMany(item => item.FindChannels(this))
                    .Where(item => _isTransformPath(item.TargetNodePath))
                    .Any();
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

        /// <summary>
        /// Gets the local transform of this node in a given animation at a given time.
        /// </summary>
        /// <param name="animation">the animation to sample.</param>
        /// <param name="time">the time offset within the animation.</param>
        /// <returns>the sampled transform.</returns>
        /// <remarks>
        /// This is a convenience method, but it's slow, it's better to cache <see cref="GetCurveSamplers(Animation)"/>.
        /// </remarks>
        public TRANSFORM GetLocalTransform(Animation animation, float time)
        {
            if (animation == null) return this.LocalTransform;

            return this.GetCurveSamplers(animation).GetLocalTransform(time);
        }

        /// <summary>
        /// Gets the world matrix of this node in a given animation at a given time.
        /// </summary>
        /// <param name="animation">the animation to sample.</param>
        /// <param name="time">the time offset within the animation.</param>
        /// <returns>the sampled transform.</returns>
        /// <remarks>
        /// This is a convenience method, but it's slow, it's better to cache <see cref="GetCurveSamplers(Animation)"/>.
        /// </remarks>
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

            // if the node doesn't have default morph weights, fall back to mesh weights.
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

        #region API

        public NodeCurveSamplers GetCurveSamplers(Animation animation)
        {
            Guard.NotNull(animation, nameof(animation));
            Guard.MustShareLogicalParent(this, animation, nameof(animation));

            return new NodeCurveSamplers(this, animation);
        }

        private void _SetLocalTransform(TRANSFORM value)
        {
            Guard.IsFalse(this._skin.HasValue, _NOTRANSFORMMESSAGE);
            Guard.IsTrue(value.IsValid, nameof(value));

            if (value.IsMatrix && this.IsTransformAnimated)
            {
                value = value.GetDecomposed(); // animated nodes require a decomposed transform.
            }

            if (value.IsMatrix)
            {
                _matrix = value.Matrix.AsNullable(Matrix4x4.Identity);
                _scale = null;
                _rotation = null;
                _translation = null;
            }
            else if (value.IsSRT)
            {
                _matrix = null;
                _scale = value.Scale.AsNullable(Vector3.One);
                _rotation = value.Rotation.Sanitized().AsNullable(Quaternion.Identity);
                _translation = value.Translation.AsNullable(Vector3.Zero);
            }
            else
            {
                throw new ArgumentException("Undefined", nameof(value));
            }
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
                    .IsUndefined(nameof(_translation), _translation)
                    .IsNullOrMatrix4x3("Matrix", _matrix);
            }

            validate
                .IsPosition("Scale", _scale.AsValue(Vector3.One))
                .IsRotation("Rotation", _rotation.AsValue(Quaternion.Identity))
                .IsPosition("Translation", _translation.AsValue(Vector3.Zero));
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

            Transforms.Matrix4x4Factory.GuardMatrix(nameof(basisTransform), basisTransform, Transforms.Matrix4x4Factory.MatrixCheck.WorldTransform);

            // gather all root nodes:
            var rootNodes = this.LogicalNodes
                .Where(item => item.VisualRoot == item)
                .ToList();

            // find all the nodes that cannot be modified
            static bool isSensible(Node node)
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

    /// <summary>
    /// Represents an proxy to acccess the animation curves of a <see cref="Node"/>.
    /// Use <see cref="Node.GetCurveSamplers(Animation)"/> for access.
    /// </summary>
    [System.Diagnostics.DebuggerDisplay("{_GetDebuggerDisplay(),nq}")]
    public readonly struct NodeCurveSamplers : IEquatable<NodeCurveSamplers>
    {
        #region debug

        private string _GetDebuggerDisplay()
        {
            if (TargetNode == null || Animation == null) return "Null";

            var txt = $"Node[{TargetNode.LogicalIndex}ᴵᵈˣ]";
            if (!string.IsNullOrWhiteSpace(this.TargetNode.Name)) txt += $" {this.TargetNode.Name}";

            txt += " <<< ";

            txt += $"Animation[{Animation.LogicalIndex}ᴵᵈˣ]";
            if (!string.IsNullOrWhiteSpace(this.Animation.Name)) txt += $" {this.Animation.Name}";

            return txt;
        }

        #endregion

        #region constructor

        internal NodeCurveSamplers(Node node, Animation animation)
        {
            TargetNode = node;
            Animation = animation;

            _ScaleSampler = null;
            _RotationSampler = null;
            _TranslationSampler = null;
            _MorphSampler = null;

            foreach (var c in animation.FindChannels(node))
            {
                switch (c.TargetNodePath)
                {
                    case PropertyPath.scale: _ScaleSampler = c._GetSampler(); break;
                    case PropertyPath.rotation: _RotationSampler = c._GetSampler(); break;
                    case PropertyPath.translation: _TranslationSampler = c._GetSampler(); break;
                    case PropertyPath.weights: _MorphSampler = c._GetSampler(); break;
                }
            }

            // if we have morphing animation, we might require to check this...
            // var morphWeights = node.MorphWeights;
            // if (morphWeights == null || morphWeights.Count == 0) { Morphing = null; MorphingSparse = null; }
        }

        #endregion

        #region data

        public readonly Node TargetNode;
        public readonly Animation Animation;

        private readonly AnimationSampler _ScaleSampler;
        private readonly AnimationSampler _RotationSampler;
        private readonly AnimationSampler _TranslationSampler;
        private readonly AnimationSampler _MorphSampler;

        /// <inheritdoc />
        public override int GetHashCode()
        {
            return TargetNode.GetHashCode() ^ Animation.GetHashCode();
        }

        /// <inheritdoc />
        public override bool Equals(object obj)
        {
            return base.Equals(obj);
        }

        public static bool operator ==(in NodeCurveSamplers a, in NodeCurveSamplers b) => a.Equals(b);
        public static bool operator !=(in NodeCurveSamplers a, in NodeCurveSamplers b) => !a.Equals(b);

        /// <inheritdoc />
        public bool Equals(NodeCurveSamplers other)
        {
            if (this.TargetNode != other.TargetNode) return false;
            if (this.Animation != other.Animation) return false;
            return true;
        }

        #endregion

        #region  properties

        /// <summary>
        /// True if any of <see cref="Scale"/>, <see cref="Rotation"/> or <see cref="Translation"/> is defined.
        /// </summary>
        public bool HasTransformCurves => _ScaleSampler != null || _RotationSampler != null || _TranslationSampler != null;

        /// <summary>
        /// True if there's a morphing curve.
        /// </summary>
        public bool HasMorphingCurves => _MorphSampler != null;

        /// <summary>
        /// Gets the Scale sampler, or null if there's no curve defined.
        /// </summary>
        public IAnimationSampler<Vector3> Scale => _ScaleSampler;

        /// <summary>
        /// Gets the Rotation sampler, or null if there's no curve defined.
        /// </summary>
        public IAnimationSampler<Quaternion> Rotation => _RotationSampler;

        /// <summary>
        /// Gets the Translation sampler, or null if there's no curve defined.
        /// </summary>
        public IAnimationSampler<Vector3> Translation => _TranslationSampler;

        /// <summary>
        /// Gets the raw Morphing sampler, or null if there's no curve defined.
        /// </summary>
        [Obsolete("Use GetMorphingSampler<T>()", true)]
        public IAnimationSampler<Single[]> Morphing => GetMorphingSampler<Single[]>();

        /// <summary>
        /// Gets the SparseWeight8 Morphing sampler, or null if there's no curve defined.
        /// </summary>
        [Obsolete("Use GetMorphingSampler<T>()", true)]
        public IAnimationSampler<Transforms.SparseWeight8> MorphingSparse => GetMorphingSampler<Transforms.SparseWeight8>();

        #endregion

        #region API

        /// <summary>
        /// Gets the morphing sampler, or null if there's no curve defined.
        /// </summary>
        /// <typeparam name="TWeights">
        /// It must be one of these:<br/>
        /// <list type="table">
        /// <item><see cref="float"/>[]</item>
        /// <item><see cref="Transforms.SparseWeight8"/></item>
        /// <item><see cref="ArraySegment{T}"/> of <see cref="float"/></item>
        /// </list>
        /// </typeparam>
        /// <returns>A valid sampler, or null.</returns>
        public IAnimationSampler<TWeights> GetMorphingSampler<TWeights>()
        {
            return _MorphSampler as IAnimationSampler<TWeights>;
        }

        public TRANSFORM GetLocalTransform(Single time)
        {
            var xform = TargetNode.LocalTransform.GetDecomposed();

            var s = Scale?.CreateCurveSampler()?.GetPoint(time) ?? xform.Scale;
            var r = Rotation?.CreateCurveSampler()?.GetPoint(time) ?? xform.Rotation;
            var t = Translation?.CreateCurveSampler()?.GetPoint(time) ?? xform.Translation;

            return new TRANSFORM(s, r, t);
        }

        public IReadOnlyList<float> GetMorphingWeights<TWeight>(Single time)
        {
            return GetMorphingSampler<float[]>()
                ?.CreateCurveSampler()
                ?.GetPoint(time)
                ?? TargetNode.MorphWeights;
        }

        public Transforms.SparseWeight8 GetSparseMorphingWeights(Single time)
        {
            return GetMorphingSampler<Transforms.SparseWeight8>()
                ?.CreateCurveSampler()
                ?.GetPoint(time)
                ?? Transforms.SparseWeight8.Create(TargetNode.MorphWeights);
        }

        #endregion
    }
}
