using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;

using SharpGLTF.IO;

using MESHBUILDER = SharpGLTF.Geometry.IMeshBuilder<SharpGLTF.Materials.MaterialBuilder>;
using TRANSFORM = SharpGLTF.Transforms.AffineTransform;

namespace SharpGLTF.Scenes
{
    /// <summary>
    /// Represents the transform of a <see cref="InstanceBuilder.Content"/>.<br/>
    /// Applies a transform to the underlaying content object (usually a Mesh, a Camera or a light)
    /// </summary>
    /// <remarks>
    /// Base class of:<br/>
    /// <see cref="FixedTransformer"/><br/>
    /// <see cref="RigidTransformer"/><br/>
    /// <see cref="SkinnedTransformer"/><br/>
    /// </remarks>
    public abstract class ContentTransformer
    {
        #region debug

        [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
        private string _DebugName => string.IsNullOrWhiteSpace(Name) ? "*" : Name;

        #endregion

        #region lifecycle

        protected ContentTransformer(Object content)
        {
            Guard.NotNull(content, nameof(content));

            if (content is MESHBUILDER mesh) content = new MeshContent(mesh);

            _Content = content;
        }

        public abstract ContentTransformer DeepClone(DeepCloneContext args);

        protected ContentTransformer(ContentTransformer other)
        {
            Guard.NotNull(other, nameof(other));

            this._Content = other._Content is ICloneable cloneable
                ? cloneable.Clone()
                : other._Content;

            this._Morphings = other._Morphings?.Clone();
        }

        #endregion

        #region data

        [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
        private Object _Content;

        [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
        private Animations.AnimatableProperty<ArraySegment<float>> _Morphings;

        #endregion

        #region properties

        /// <summary>
        /// Gets or sets the display text name, or null.
        /// <para><b>⚠️ DO NOT USE AS AN OBJECT ID ⚠️</b> see remarks.</para>
        /// </summary>
        /// <remarks>
        /// glTF does not define any rule for object names.<br/>
        /// This means that names can be null or non unique.<br/>
        /// So don't use <see cref="Name"/> for anything other than object name display.<br/>
        /// If you need to reference objects by some ID, use lookup tables instead.
        /// </remarks>
        public abstract String Name { get; set; }

        /// <summary>
        /// Gets or sets the custom data of this object.
        /// </summary>
        public abstract IO.JsonContent Extras { get; set; }

        /// <summary>
        /// Gets the content of this transformer.<br/>
        /// </summary>
        internal Object Content => _Content;

        public Animations.AnimatableProperty<ArraySegment<float>> Morphings => _Morphings;

        /// <summary>
        /// Gets a value indicating whether <see cref="Content"/> implements <see cref="IRenderableContent"/>
        /// </summary>
        public bool HasRenderableContent => _Content is IRenderableContent;

        #endregion

        #region API

        /// <summary>
        /// If this <see cref="ContentTransformer"/> contains a <see cref="MESHBUILDER"/>.
        /// </summary>
        /// <returns>A <see cref="MESHBUILDER"/> instance, or NULL.</returns>
        public virtual MESHBUILDER GetGeometryAsset() { return (_Content as IRenderableContent)?.GetGeometryAsset(); }

        /// <summary>
        /// If this <see cref="ContentTransformer"/> uses a <see cref="NodeBuilder"/> armature, it returns the root of the armature.
        /// </summary>
        /// <returns>A <see cref="NodeBuilder"/> instance, or NULL.</returns>
        public abstract NodeBuilder GetArmatureRoot();

        public Animations.AnimatableProperty<ArraySegment<float>> UseMorphing()
        {
            if (_Morphings == null)
            {
                _Morphings = new Animations.AnimatableProperty<ArraySegment<float>>();
                _Morphings.Value = default;
            }

            return _Morphings;
        }

        public Animations.CurveBuilder<ArraySegment<float>> UseMorphing(string animationTrack)
        {
            var m = UseMorphing();

            if (m.Value.Count == 0) throw new InvalidOperationException("A default sequence of weights must be set before setting animated weights. Use UseMorphing().SetValue(...)");

            return m.UseTrackBuilder(animationTrack);
        }

        public abstract Matrix4x4 GetPoseWorldMatrix();

        internal IEnumerable<string> GetAnimationTracksNames()
        {
            var tracks = NodeBuilder.Flatten(this.GetArmatureRoot()).SelectMany(item => item.AnimationTracksNames);
            if (_Morphings != null) tracks = tracks.Concat(_Morphings.Tracks.Keys);

            return tracks.Distinct();
        }

        #endregion

        #region nestedTypes

        public readonly struct DeepCloneContext
        {
            internal DeepCloneContext(IReadOnlyDictionary<NodeBuilder, NodeBuilder> nmap)
            {
                _NodeMap = nmap;
            }

            private readonly IReadOnlyDictionary<NodeBuilder, NodeBuilder> _NodeMap;

            public NodeBuilder GetNode(NodeBuilder node)
            {
                if (node == null) return null;
                if (_NodeMap == null) return node;
                return _NodeMap.TryGetValue(node, out NodeBuilder clone) ? clone : node;
            }
        }

        #endregion
    }

    /// <summary>
    /// Represents the transform of a <see cref="InstanceBuilder.Content"/>.<br/>
    /// Applies a fixed <see cref="Matrix4x4"/> transform to the underlaying content.
    /// </summary>
    [System.Diagnostics.DebuggerDisplay("Fixed Node[{_DebugName,nq}] = {Content}")]
    public partial class FixedTransformer : ContentTransformer
    {
        #region lifecycle

        internal FixedTransformer(Object content, TRANSFORM transform)
            : base(content)
        {
            _ChildTransform = transform;
        }

        internal FixedTransformer(Object content, NodeBuilder parentNode, TRANSFORM childTransform)
            : base(content)
        {
            _ParentNode = parentNode;
            _ChildTransform = childTransform;
        }

        protected FixedTransformer(FixedTransformer other, DeepCloneContext args)
            : base(other)
        {
            Guard.NotNull(other, nameof(other));

            this._ParentNode = args.GetNode(other._ParentNode);
            this._NodeName = other._NodeName;
            this._NodeExtras = other._NodeExtras.DeepClone();
            this._ChildTransform = other._ChildTransform;
        }

        public override ContentTransformer DeepClone(DeepCloneContext args)
        {
            return new FixedTransformer(this, args);
        }

        #endregion

        #region data

        [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
        private String _NodeName;

        [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
        private IO.JsonContent _NodeExtras;

        [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
        private NodeBuilder _ParentNode;

        [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
        private TRANSFORM _ChildTransform;

        #endregion

        #region properties

        /// <inheritdoc/>
        public override String Name
        {
            get => _NodeName;
            set => _NodeName = value;
        }

        /// <inheritdoc/>
        public override JsonContent Extras
        {
            get => _NodeExtras;
            set => _NodeExtras = value;
        }

        public NodeBuilder ParentNode => _ParentNode;

        public TRANSFORM ChildTransform
        {
            get => _ChildTransform;
            set => _ChildTransform = value;
        }

        #endregion

        #region API

        public override NodeBuilder GetArmatureRoot() { return _ParentNode?.Root; }

        public override Matrix4x4 GetPoseWorldMatrix()
        {
            return _ParentNode == null
                ? _ChildTransform.Matrix
                : _ChildTransform.Matrix * _ParentNode.WorldMatrix;
        }

        #endregion

    }

    /// <summary>
    /// Represents the transform of a <see cref="InstanceBuilder.Content"/>.<br/>
    /// Applies the transform of a single <see cref="NodeBuilder"/> to the underlaying content.
    /// </summary>
    [System.Diagnostics.DebuggerDisplay("Rigid Node[{_DebugName,nq}] = {Content}")]
    public partial class RigidTransformer : ContentTransformer
    {
        #region lifecycle

        internal RigidTransformer(Object content, NodeBuilder node)
            : base(content)
        {
            _Node = node;
        }

        protected RigidTransformer(RigidTransformer other, DeepCloneContext args)
            : base(other)
        {
            Guard.NotNull(other, nameof(other));

            this._Node = args.GetNode(other._Node);
        }

        public override ContentTransformer DeepClone(DeepCloneContext args)
        {
            return new RigidTransformer(this, args);
        }

        #endregion

        #region data

        [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
        private NodeBuilder _Node;

        #endregion

        #region properties

        /// <inheritdoc/>
        public override String Name
        {
            get => _Node.Name;
            set => _Node.Name = value;
        }

        /// <inheritdoc/>
        public override JsonContent Extras
        {
            get => _Node.Extras;
            set => _Node.Extras = value;
        }

        public NodeBuilder Transform
        {
            get => _Node;
            set => _Node = value;
        }

        #endregion

        #region API

        public override NodeBuilder GetArmatureRoot() { return _Node.Root; }

        public override Matrix4x4 GetPoseWorldMatrix() => Transform.WorldMatrix;

        #endregion
    }

    /// <summary>
    /// Represents the transform of a <see cref="InstanceBuilder.Content"/>.<br/>
    /// Applies the transforms of many <see cref="NodeBuilder"/> to the underlaying content.
    /// </summary>
    [System.Diagnostics.DebuggerDisplay("Skinned Node[{_DebugName,nq}] = {Content}")]
    public partial class SkinnedTransformer : ContentTransformer
    {
        #region lifecycle

        internal SkinnedTransformer(MESHBUILDER mesh, TRANSFORM meshWorldTransform, NodeBuilder[] joints)
            : base(mesh)
        {
            SetJoints(meshWorldTransform, joints);
        }

        internal SkinnedTransformer(MESHBUILDER mesh, (NodeBuilder Joint, Matrix4x4 InverseBindMatrix)[] joints)
            : base(mesh)
        {
            SetJoints(joints);
        }

        protected SkinnedTransformer(SkinnedTransformer other, DeepCloneContext args)
            : base(other)
        {
            Guard.NotNull(other, nameof(other));

            this._NodeName = other._NodeName;
            this._NodeExtras = other._NodeExtras.DeepClone();
            this._MeshPoseWorldTransform = other._MeshPoseWorldTransform;

            foreach (var (joint, inverseBindMatrix) in other._Joints)
            {
                var jj = (args.GetNode(joint), inverseBindMatrix);

                this._Joints.Add(jj);
            }
        }

        public override ContentTransformer DeepClone(DeepCloneContext args)
        {
            return new SkinnedTransformer(this, args);
        }

        #endregion

        #region data

        [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
        private String _NodeName;

        [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
        private IO.JsonContent _NodeExtras;

        /// <summary>
        /// Defines the world matrix of the mesh at the time of binding.
        /// </summary>
        [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
        private TRANSFORM? _MeshPoseWorldTransform;

        // condition: all NodeBuilder objects must have the same root.
        [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
        private readonly List<(NodeBuilder Joint, Matrix4x4? InverseBindMatrix)> _Joints = new List<(NodeBuilder, Matrix4x4?)>();

        #endregion

        #region properties

        /// <inheritdoc/>
        public override String Name
        {
            get => _NodeName;
            set => _NodeName = value;
        }

        /// <inheritdoc/>
        public override JsonContent Extras
        {
            get => _NodeExtras;
            set => _NodeExtras = value;
        }

        #endregion

        #region API

        private void SetJoints(TRANSFORM meshWorldTransform, NodeBuilder[] joints)
        {
            Guard.NotNull(joints, nameof(joints));
            Guard.IsTrue(NodeBuilder.IsValidArmature(joints), nameof(joints));

            _MeshPoseWorldTransform = meshWorldTransform;
            _Joints.Clear();
            _Joints.AddRange(joints.Select(item => (item, (Matrix4x4?)null)));
        }

        private void SetJoints((NodeBuilder Joint, Matrix4x4 InverseBindMatrix)[] joints)
        {
            Guard.NotNull(joints, nameof(joints));
            Guard.IsTrue(NodeBuilder.IsValidArmature(joints.Select(item => item.Joint)), nameof(joints));

            _MeshPoseWorldTransform = null;
            _Joints.Clear();
            _Joints.AddRange(joints.Select(item => (item.Joint, (Matrix4x4?)item.InverseBindMatrix)));
        }

        public (NodeBuilder Joint, Matrix4x4 InverseBindMatrix)[] GetJointBindings()
        {
            var jb = new (NodeBuilder Joint, Matrix4x4 InverseBindMatrix)[_Joints.Count];

            var meshPoseWorld = _MeshPoseWorldTransform?.Matrix ?? Matrix4x4.Identity;

            for (int i = 0; i < jb.Length; ++i)
            {
                var j = _Joints[i].Joint;
                var m = _Joints[i].InverseBindMatrix ?? Transforms.SkinnedTransform.CalculateInverseBinding(meshPoseWorld, j.WorldMatrix);

                jb[i] = (j, m);
            }

            return jb;
        }

        public override NodeBuilder GetArmatureRoot()
        {
            return _Joints
                .Select(item => item.Joint.Root)
                .Distinct()
                .FirstOrDefault();
        }

        public Transforms.IGeometryTransform GetWorldTransformer(string animationTrack, float time)
        {
            var jb = GetJointBindings();

            return new Transforms.SkinnedTransform
                (
                jb.Length,
                idx => jb[idx].InverseBindMatrix,
                idx => jb[idx].Joint.GetWorldMatrix(animationTrack, time),
                default, false
                );
        }

        public override Matrix4x4 GetPoseWorldMatrix() => _MeshPoseWorldTransform?.Matrix ?? Matrix4x4.Identity;

        #endregion
    }
}
