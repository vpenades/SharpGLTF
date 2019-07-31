using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

using MESHBUILDER = SharpGLTF.Geometry.IMeshBuilder<SharpGLTF.Materials.MaterialBuilder>;

namespace SharpGLTF.Scenes
{
    /// <summary>
    /// Wraps a content object (usually a Mesh, a Camera or a light)
    /// </summary>
    public abstract class ContentTransformer
    {
        #region lifecycle

        protected ContentTransformer(Object content)
        {
            Guard.NotNull(content, nameof(content));

            _Content = content;
        }

        protected ContentTransformer(MESHBUILDER mesh)
        {
            Guard.NotNull(mesh, nameof(mesh));

            _Content = new MeshContent(mesh);
        }

        #endregion

        #region data

        private Object _Content;

        #endregion

        #region properties

        public Object Content => _Content;

        #endregion

        #region API

        public virtual MESHBUILDER GetGeometryAsset() { return (_Content as IRenderableContent)?.GetGeometryAsset(); }

        public abstract NodeBuilder GetArmatureAsset();

        #endregion
    }

    public partial class StaticTransformer : ContentTransformer
    {
        #region lifecycle

        public StaticTransformer(Object content, Matrix4x4 xform)
            : base(content)
        {
            _WorldTransform = xform;
        }

        public StaticTransformer(MESHBUILDER mesh, Matrix4x4 xform)
            : base(mesh)
        {
            _WorldTransform = xform;
        }

        #endregion

        #region data

        private Matrix4x4 _WorldTransform;

        #endregion

        #region properties

        public Matrix4x4 WorldTransform
        {
            get => _WorldTransform;
            set => _WorldTransform = value;
        }

        #endregion

        #region API

        public override NodeBuilder GetArmatureAsset() { return null; }

        #endregion
    }

    public partial class NodeTransformer : ContentTransformer
    {
        #region lifecycle

        public NodeTransformer(Object content, NodeBuilder node)
            : base(content)
        {
            _Node = node;
        }

        public NodeTransformer(MESHBUILDER mesh, NodeBuilder node)
            : base(mesh)
        {
            _Node = node;
        }

        #endregion

        #region data

        private NodeBuilder _Node;

        #endregion

        #region properties

        public NodeBuilder Transform
        {
            get => _Node;
            set => _Node = value;
        }

        #endregion

        #region API

        public override NodeBuilder GetArmatureAsset() { return _Node.Root; }

        #endregion
    }

    public partial class SkinTransformer : ContentTransformer
    {
        #region lifecycle

        public SkinTransformer(MESHBUILDER mesh, Matrix4x4 meshWorldMatrix, NodeBuilder[] joints)
            : base(mesh)
        {
            SetJoints(meshWorldMatrix, joints);
        }

        public SkinTransformer(MESHBUILDER mesh, (NodeBuilder, Matrix4x4)[] joints)
            : base(mesh)
        {
            SetJoints(joints);
        }

        #endregion

        #region data

        private Matrix4x4? _TargetBindMatrix;

        // condition: all NodeBuilder objects must have the same root.
        private readonly List<(NodeBuilder, Matrix4x4?)> _Joints = new List<(NodeBuilder, Matrix4x4?)>();

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

        private void SetJoints((NodeBuilder, Matrix4x4)[] joints)
        {
            Guard.NotNull(joints, nameof(joints));
            Guard.IsTrue(NodeBuilder.IsValidArmature(joints.Select(item => item.Item1)), nameof(joints));

            _TargetBindMatrix = null;
            _Joints.Clear();
            _Joints.AddRange(joints.Select(item => (item.Item1, (Matrix4x4?)item.Item2)));
        }

        public (NodeBuilder, Matrix4x4)[] GetJointBindings()
        {
            var jb = new (NodeBuilder, Matrix4x4)[_Joints.Count];

            for (int i = 0; i < jb.Length; ++i)
            {
                var j = _Joints[i].Item1;
                var m = _Joints[i].Item2 ?? Transforms.SkinTransform.CalculateInverseBinding(_TargetBindMatrix ?? Matrix4x4.Identity, j.WorldMatrix);

                jb[i] = (j, m);
            }

            return jb;
        }

        public override NodeBuilder GetArmatureAsset()
        {
            return _Joints
                .Select(item => item.Item1.Root)
                .Distinct()
                .FirstOrDefault();
        }

        public Transforms.ITransform GetWorldTransformer(string animationTrack, float time)
        {
            var jb = GetJointBindings();

            var ww = jb.Select(item => item.Item1.GetWorldMatrix(animationTrack, time)).ToArray();
            var bb = jb.Select(item => item.Item2).ToArray();

            return new Transforms.SkinTransform(bb, ww, default, false);
        }

        #endregion
    }
}
