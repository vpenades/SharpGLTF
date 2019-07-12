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

        private IContent _Target;// Can be either a morphController or a mesh, or light or camera

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

        public SkinTransformer(MESHBUILDER mesh, NodeBuilder[] joints)
        {
            _Target = new MeshContent(mesh);
            _Joints.AddRange(joints);
        }

        #endregion

        #region data

        private IRenderableContent _Target; // Can be either a morphController or a mesh

        // condition: all NodeBuilder objects must have the same root.
        private readonly List<NodeBuilder> _Joints = new List<NodeBuilder>();

        #endregion

        #region API

        public MESHBUILDER GetGeometryAsset() { return (_Target as IRenderableContent)?.GetGeometryAsset(); }

        public NodeBuilder GetArmatureAsset() { return _Joints.Select(item => item.Root).Distinct().FirstOrDefault(); }

        public void Setup(Scene dstScene, Schema2SceneBuilder context)
        {
            var skinnedMeshNode = dstScene.CreateNode();

            var skinnedJoints = _Joints.Select(j => context.GetNode(j)).ToArray();

            skinnedMeshNode.WithSkinBinding(skinnedJoints);

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
