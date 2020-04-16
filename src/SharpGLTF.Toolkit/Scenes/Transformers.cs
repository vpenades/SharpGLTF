using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

using MESHBUILDER = SharpGLTF.Geometry.IMeshBuilder<SharpGLTF.Materials.MaterialBuilder>;

namespace SharpGLTF.Scenes
{
    /// <summary>
    /// Applies a transform to the underlaying content object (usually a Mesh, a Camera or a light)
    /// </summary>
    public abstract class ContentTransformer
    {
        #region lifecycle

        protected ContentTransformer(Object content)
        {
            Guard.NotNull(content, nameof(content));

            if (content is MESHBUILDER mesh) content = new MeshContent(mesh);

            _Content = content;
        }

        public abstract ContentTransformer DeepClone();

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

        private Object _Content;

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

        #endregion
    }

    /// <summary>
    /// Applies a fixed <see cref="Matrix4x4"/> transform to the underlaying content.
    /// </summary>
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

        public override ContentTransformer DeepClone()
        {
            return new FixedTransformer(this);
        }

        #endregion

        #region data

        private String _NodeName;

        private Matrix4x4 _WorldTransform;

        #endregion

        #region properties

        public override String Name => _NodeName;

        public Matrix4x4 WorldTransform
        {
            get => _WorldTransform;
            set => _WorldTransform = value;
        }

        #endregion

        #region API

        public override NodeBuilder GetArmatureRoot() { return null; }

        #endregion
    }

    /// <summary>
    /// Applies the transform of a single <see cref="NodeBuilder"/> to the underlaying content.
    /// </summary>
    public partial class RigidTransformer : ContentTransformer
    {
        #region lifecycle

        internal RigidTransformer(Object content, NodeBuilder node)
            : base(content)
        {
            _Node = node;
        }

        protected RigidTransformer(RigidTransformer other)
            : base(other)
        {
            Guard.NotNull(other, nameof(other));

            this._Node = other._Node;
        }

        public override ContentTransformer DeepClone()
        {
            return new RigidTransformer(this);
        }

        #endregion

        #region data

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

        #endregion
    }

    /// <summary>
    /// Applies the transforms of many <see cref="NodeBuilder"/> to the underlaying content.
    /// </summary>
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

        protected SkinnedTransformer(SkinnedTransformer other)
            : base(other)
        {
            Guard.NotNull(other, nameof(other));

            this._NodeName = other._NodeName;
            this._TargetBindMatrix = other._TargetBindMatrix;
            this._Joints.AddRange(other._Joints);
        }

        public override ContentTransformer DeepClone()
        {
            return new SkinnedTransformer(this);
        }

        #endregion

        #region data

        private String _NodeName;

        private Matrix4x4? _TargetBindMatrix;

        // condition: all NodeBuilder objects must have the same root.
        private readonly List<(NodeBuilder Joints, Matrix4x4? InverseBindMatrix)> _Joints = new List<(NodeBuilder, Matrix4x4?)>();

        #endregion

        #region properties

        public override String Name => _NodeName;

        #endregion

        #region API

        private void SetJoints(Matrix4x4 meshWorldMatrix, NodeBuilder[] joints)
        {
            Guard.NotNull(joints, nameof(joints));
            Guard.IsTrue(NodeBuilder.IsValidArmature(joints), nameof(joints));

            _TargetBindMatrix = meshWorldMatrix;
            _Joints.Clear();
            _Joints.AddRange(joints.Select(item => (item, (Matrix4x4?)null)));
        }

        private void SetJoints((NodeBuilder Joint, Matrix4x4 InverseBindMatrix)[] joints)
        {
            Guard.NotNull(joints, nameof(joints));
            Guard.IsTrue(NodeBuilder.IsValidArmature(joints.Select(item => item.Joint)), nameof(joints));

            _TargetBindMatrix = null;
            _Joints.Clear();
            _Joints.AddRange(joints.Select(item => (item.Joint, (Matrix4x4?)item.InverseBindMatrix)));
        }

        public (NodeBuilder Joint, Matrix4x4 InverseBindMatrix)[] GetJointBindings()
        {
            var jb = new (NodeBuilder Joint, Matrix4x4 InverseBindMatrix)[_Joints.Count];

            for (int i = 0; i < jb.Length; ++i)
            {
                var j = _Joints[i].Joints;
                var m = _Joints[i].InverseBindMatrix ?? Transforms.SkinnedTransform.CalculateInverseBinding(_TargetBindMatrix ?? Matrix4x4.Identity, j.WorldMatrix);

                jb[i] = (j, m);
            }

            return jb;
        }

        public override NodeBuilder GetArmatureRoot()
        {
            return _Joints
                .Select(item => item.Joints.Root)
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

        #endregion
    }
}
