using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;

using MESHBUILDER = SharpGLTF.Geometry.IMeshBuilder<SharpGLTF.Materials.MaterialBuilder>;

namespace SharpGLTF.Scenes
{
    /// <summary>
    /// Applies a transform to the underlaying content object (usually a Mesh, a Camera or a light)
    /// </summary>
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

            if (other._Content is ICloneable cloneable)
            {
                this._Content = cloneable.Clone();
            }
            else
            {
                this._Content = other._Content;
            }

            this._Morphings = other._Morphings?.Clone();
        }

        #endregion

        #region data

        [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
        private Object _Content;

        [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
        private Animations.AnimatableProperty<Transforms.SparseWeight8> _Morphings;

        #endregion

        #region properties

        public abstract String Name { get; }

        public Object Content => _Content;

        public Animations.AnimatableProperty<Transforms.SparseWeight8> Morphings => _Morphings;

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

        public Animations.AnimatableProperty<Transforms.SparseWeight8> UseMorphing()
        {
            if (_Morphings == null)
            {
                _Morphings = new Animations.AnimatableProperty<Transforms.SparseWeight8>();
                _Morphings.Value = default;
            }

            return _Morphings;
        }

        public Animations.CurveBuilder<Transforms.SparseWeight8> UseMorphing(string animationTrack)
        {
            return UseMorphing().UseTrackBuilder(animationTrack);
        }

        public abstract Matrix4x4 GetPoseWorldMatrix();

        #endregion

        #region nestedTypes

        public readonly struct DeepCloneContext
        {
            public DeepCloneContext(IReadOnlyDictionary<NodeBuilder, NodeBuilder> nmap)
            {
                _NodeMap = nmap;
            }

            private readonly IReadOnlyDictionary<NodeBuilder, NodeBuilder> _NodeMap;

            public NodeBuilder GetNode(NodeBuilder node)
            {
                if (_NodeMap == null) return node;
                return _NodeMap.TryGetValue(node, out NodeBuilder clone) ? clone : node;
            }
        }

        #endregion
    }

    /// <summary>
    /// Applies a fixed <see cref="Matrix4x4"/> transform to the underlaying content.
    /// </summary>
    [System.Diagnostics.DebuggerDisplay("Fixed Node[{_DebugName,nq}] = {Content}")]
    public partial class FixedTransformer : ContentTransformer
    {
        #region lifecycle

        internal FixedTransformer(Object content, Matrix4x4 xform, string nodeName = null)
            : base(content)
        {
            _WorldTransform = xform;
            _NodeName = nodeName;
        }

        protected FixedTransformer(FixedTransformer other)
            : base(other)
        {
            Guard.NotNull(other, nameof(other));

            this._NodeName = other._NodeName;
            this._WorldTransform = other._WorldTransform;
        }

        public override ContentTransformer DeepClone(DeepCloneContext args)
        {
            return new FixedTransformer(this);
        }

        #endregion

        #region data

        [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
        private String _NodeName;

        [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
        private Matrix4x4 _WorldTransform;

        #endregion

        #region properties

        public override String Name => _NodeName;

        public Matrix4x4 WorldMatrix
        {
            get => _WorldTransform;
            set => _WorldTransform = value;
        }

        #endregion

        #region API

        public override NodeBuilder GetArmatureRoot() { return null; }

        public override Matrix4x4 GetPoseWorldMatrix() => WorldMatrix;

        #endregion

    }

    /// <summary>
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

        public override String Name => _Node.Name;

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
    /// Applies the transforms of many <see cref="NodeBuilder"/> to the underlaying content.
    /// </summary>
    [System.Diagnostics.DebuggerDisplay("Skinned Node[{_DebugName,nq}] = {Content}")]
    public partial class SkinnedTransformer : ContentTransformer
    {
        #region lifecycle

        internal SkinnedTransformer(MESHBUILDER mesh, Matrix4x4 meshWorldMatrix, NodeBuilder[] joints, string nodeName = null)
            : base(mesh)
        {
            _NodeName = nodeName;
            SetJoints(meshWorldMatrix, joints);
        }

        internal SkinnedTransformer(MESHBUILDER mesh, (NodeBuilder Joint, Matrix4x4 InverseBindMatrix)[] joints, string nodeName = null)
            : base(mesh)
        {
            _NodeName = nodeName;
            SetJoints(joints);
        }

        protected SkinnedTransformer(SkinnedTransformer other, DeepCloneContext args)
            : base(other)
        {
            Guard.NotNull(other, nameof(other));

            this._NodeName = other._NodeName;
            this._MeshPoseWorldMatrix = other._MeshPoseWorldMatrix;

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

        /// <summary>
        /// Defines the world matrix of the mesh at the time of binding.
        /// </summary>
        [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
        private Matrix4x4? _MeshPoseWorldMatrix;

        // condition: all NodeBuilder objects must have the same root.
        [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
        private readonly List<(NodeBuilder Joint, Matrix4x4? InverseBindMatrix)> _Joints = new List<(NodeBuilder, Matrix4x4?)>();

        #endregion

        #region properties

        public override String Name => _NodeName;

        #endregion

        #region API

        private void SetJoints(Matrix4x4 meshWorldMatrix, NodeBuilder[] joints)
        {
            Guard.NotNull(joints, nameof(joints));
            Guard.IsTrue(NodeBuilder.IsValidArmature(joints), nameof(joints));

            _MeshPoseWorldMatrix = meshWorldMatrix;
            _Joints.Clear();
            _Joints.AddRange(joints.Select(item => (item, (Matrix4x4?)null)));
        }

        private void SetJoints((NodeBuilder Joint, Matrix4x4 InverseBindMatrix)[] joints)
        {
            Guard.NotNull(joints, nameof(joints));
            Guard.IsTrue(NodeBuilder.IsValidArmature(joints.Select(item => item.Joint)), nameof(joints));

            _MeshPoseWorldMatrix = null;
            _Joints.Clear();
            _Joints.AddRange(joints.Select(item => (item.Joint, (Matrix4x4?)item.InverseBindMatrix)));
        }

        public (NodeBuilder Joint, Matrix4x4 InverseBindMatrix)[] GetJointBindings()
        {
            var jb = new (NodeBuilder Joint, Matrix4x4 InverseBindMatrix)[_Joints.Count];

            for (int i = 0; i < jb.Length; ++i)
            {
                var j = _Joints[i].Joint;
                var m = _Joints[i].InverseBindMatrix ?? Transforms.SkinnedTransform.CalculateInverseBinding(_MeshPoseWorldMatrix ?? Matrix4x4.Identity, j.WorldMatrix);

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

        public override Matrix4x4 GetPoseWorldMatrix() => _MeshPoseWorldMatrix ?? Matrix4x4.Identity;

        #endregion
    }
}
