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
        void Setup(Scene dstScene, Schema2SceneBuilder context);
    }

    interface IContent
    {
        void Setup(Node dstNode, Schema2SceneBuilder context);
    }

    interface IRenderableContent : IContent
    {
        MESHBUILDER GeometryAsset { get; }
    }

    class StaticController : IContentRoot
    {
        private IContent _target;// Can be either a morphController or a mesh, or light or camera

        private Matrix4x4 _Transform;

        public void Setup(Scene dstScene, Schema2SceneBuilder context)
        {
            var node = dstScene.CreateNode();
            node.LocalMatrix = _Transform;

            _target.Setup(node, context);
        }
    }

    class TransformController : IContentRoot
    {
        private IContent _target;// Can be either a morphController or a mesh, or light or camera

        private NodeBuilder _Node;

        public void Setup(Schema2.Scene dstScene, Schema2SceneBuilder context)
        {
            var node = context.GetNode(_Node);

            if (node == null) dstScene.CreateNode();

            _target.Setup(node, context);
        }
    }

    class SkinController : IContentRoot
    {
        private IRenderableContent _Target; // Can be either a morphController or a mesh

        // condition: all NodeBuilder objects must have the same root.
        private readonly List<NodeBuilder> _Joints = new List<NodeBuilder>();

        public void Setup(Scene dstScene, Schema2SceneBuilder context)
        {
            var skinnedMeshNode = dstScene.CreateNode();

            var skinnedJoints = _Joints.Select(j => context.GetNode(j)).ToArray();

            skinnedMeshNode.WithSkinBinding(skinnedJoints);

            _Target.Setup(skinnedMeshNode, context);
        }
    }


    // We really have two options here: Either implement this here, or as a derived of IMeshBuilder<MaterialBuilder>

    class MorphModifier : IRenderableContent // must be a child of a controller, and the parent of a mesh
    {
        private IRenderableContent _Target; // must be a mesh

        // morph targets here

        private readonly Animations.Animatable<Vector4> _MorphWeights = new Animations.Animatable<Vector4>();

        public MESHBUILDER GeometryAsset => _Target?.GeometryAsset;

        public void Setup(Node dstNode, Schema2SceneBuilder context)
        {
            _Target.Setup(dstNode, context);

            // setup morphs here!
        }
    }

    class MeshContent : IRenderableContent
    {
        private MESHBUILDER _Geometry;

        public MESHBUILDER GeometryAsset
        {
            get => _Geometry;
            set => _Geometry = value;
        }

        public void Setup(Node dstNode, Schema2SceneBuilder context)
        {
            dstNode.Mesh = context.GetMesh(_Geometry);
        }
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
