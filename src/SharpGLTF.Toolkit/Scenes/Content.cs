using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using SharpGLTF.Geometry;
using SharpGLTF.Materials;
using SharpGLTF.Schema2;

namespace SharpGLTF.Scenes
{
    using MESHBUILDER = IMeshBuilder<MaterialBuilder>;

    interface IContentRoot
    {
        MESHBUILDER GetGeometryAsset();

        NodeBuilder GetArmatureAsset();

        void Setup(Scene dstScene, Schema2SceneBuilder context);
    }

    interface IContent
    {
        void Setup(Node dstNode, Schema2SceneBuilder context);
    }

    interface IRenderableContent : IContent
    {
        MESHBUILDER GetGeometryAsset();
    }

    class StaticTransformer : IContentRoot
    {
        #region lifecycle

        public StaticTransformer(MESHBUILDER mesh, Matrix4x4 xform)
        {
            _Transform = xform;
            _Target = new MeshContent(mesh);
        }

        #endregion

        #region data

        private IContent _Target; // Can be either a morphController or a mesh, or light or camera

        private Matrix4x4 _Transform;

        #endregion

        #region API

        public NodeBuilder GetArmatureAsset() { return null; }

        public MESHBUILDER GetGeometryAsset() { return (_Target as IRenderableContent)?.GetGeometryAsset(); }

        public void Setup(Scene dstScene, Schema2SceneBuilder context)
        {
            var node = dstScene.CreateNode();
            node.LocalMatrix = _Transform;

            _Target.Setup(node, context);
        }

        #endregion
    }

    class NodeTransformer : IContentRoot
    {
        #region lifecycle

        public NodeTransformer(MESHBUILDER mesh, NodeBuilder node)
        {
            _Node = node;
            _Target = new MeshContent(mesh);
        }

        #endregion

        #region data

        private IContent _Target; // Can be either a morphController or a mesh, or light or camera

        private NodeBuilder _Node;

        #endregion

        #region API

        public NodeBuilder GetArmatureAsset() { return _Node.Root; }

        public MESHBUILDER GetGeometryAsset() { return (_Target as IRenderableContent)?.GetGeometryAsset(); }

        public void Setup(Schema2.Scene dstScene, Schema2SceneBuilder context)
        {
            var node = context.GetNode(_Node);

            if (node == null) dstScene.CreateNode();

            _Target.Setup(node, context);
        }

        #endregion
    }

    class SkinTransformer : IContentRoot
    {
        #region lifecycle

        public SkinTransformer(MESHBUILDER mesh, Matrix4x4 meshBindMatrix, NodeBuilder[] joints)
        {
            Guard.NotNull(mesh, nameof(mesh));
            Guard.NotNull(joints, nameof(joints));
            Guard.IsTrue(NodeBuilder.IsValidArmature(joints), nameof(joints));

            _Target = new MeshContent(mesh);
            _TargetBindMatrix = meshBindMatrix;
            _Joints.AddRange(joints.Select(item => (item, (Matrix4x4?)null)));
        }

        public SkinTransformer(MESHBUILDER mesh, (NodeBuilder, Matrix4x4)[] joints)
        {
            Guard.NotNull(mesh, nameof(mesh));
            Guard.NotNull(joints, nameof(joints));
            Guard.IsTrue(NodeBuilder.IsValidArmature(joints.Select(item => item.Item1)), nameof(joints));

            _Target = new MeshContent(mesh);
            _TargetBindMatrix = null;
            _Joints.AddRange(joints.Select(item => (item.Item1, (Matrix4x4?)item.Item2)));
        }

        #endregion

        #region data

        private IRenderableContent _Target; // Can be either a morphController or a mesh
        private Matrix4x4? _TargetBindMatrix;

        // condition: all NodeBuilder objects must have the same root.
        private readonly List<(NodeBuilder, Matrix4x4?)> _Joints = new List<(NodeBuilder, Matrix4x4?)>();

        #endregion

        #region API

        public MESHBUILDER GetGeometryAsset() { return (_Target as IRenderableContent)?.GetGeometryAsset(); }

        public NodeBuilder GetArmatureAsset() { return _Joints.Select(item => item.Item1.Root).Distinct().FirstOrDefault(); }

        public void Setup(Scene dstScene, Schema2SceneBuilder context)
        {
            var skinnedMeshNode = dstScene.CreateNode();

            if (_TargetBindMatrix.HasValue)
            {
                var dstNodes = new Node[_Joints.Count];

                for (int i = 0; i < dstNodes.Length; ++i)
                {
                    var srcNode = _Joints[i];

                    System.Diagnostics.Debug.Assert(!srcNode.Item2.HasValue);

                    dstNodes[i] = context.GetNode(srcNode.Item1);
                }

                #if DEBUG
                for (int i = 0; i < dstNodes.Length; ++i)
                {
                    var srcNode = _Joints[i];
                    System.Diagnostics.Debug.Assert(dstNodes[i].WorldMatrix == srcNode.Item1.WorldMatrix);
                }
                #endif

                skinnedMeshNode.WithSkinBinding(_TargetBindMatrix.Value, dstNodes);
            }
            else
            {
                var skinnedJoints = _Joints
                .Select(j => (context.GetNode(j.Item1), j.Item2.Value) )
                .ToArray();

                skinnedMeshNode.WithSkinBinding(skinnedJoints);
            }

            // set skeleton
            // var root = _Joints[0].Item1.Root;
            // skinnedMeshNode.Skin.Skeleton = context.GetNode(root);

            _Target.Setup(skinnedMeshNode, context);
        }

        #endregion
    }

    // We really have two options here: Either implement this here, or as a derived of IMeshBuilder<MaterialBuilder>

    class MorphModifier : IRenderableContent // must be a child of a controller, and the parent of a mesh
    {
        #region data

        private IRenderableContent _Target;

        private readonly List<Animations.AnimatableProperty<float>> _MorphWeights = new List<Animations.AnimatableProperty<float>>();

        #endregion

        #region API

        public MESHBUILDER GetGeometryAsset() => _Target?.GetGeometryAsset();

        public void Setup(Node dstNode, Schema2SceneBuilder context)
        {
            _Target.Setup(dstNode, context);

            // setup morphs here!
        }

        #endregion
    }

    class MeshContent : IRenderableContent
    {
        #region lifecycle

        public MeshContent(MESHBUILDER mesh)
        {
            _Mesh = mesh;
        }

        #endregion

        #region data

        private MESHBUILDER _Mesh;

        #endregion

        #region API

        public MESHBUILDER GetGeometryAsset() => _Mesh;

        public void Setup(Node dstNode, Schema2SceneBuilder context)
        {
            dstNode.Mesh = context.GetMesh(_Mesh);
        }

        #endregion
    }

    class LightContent : IContent
    {
        public void Setup(Node dstNode, Schema2SceneBuilder context)
        {
            throw new NotImplementedException();
        }
    }

    class CameraContent : IContent
    {
        public void Setup(Node dstNode, Schema2SceneBuilder context)
        {
            throw new NotImplementedException();
        }
    }
}
