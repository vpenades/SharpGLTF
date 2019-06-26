using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace SharpGLTF.Scenes
{
    interface IContentRoot { }

    interface IContent { }

    interface IRenderableContent : IContent { }

    class StaticController : IContentRoot
    {
        private IContent _target;// Can be either a morphController or a mesh, or light or camera

        private Matrix4x4 _Transform;
    }

    class TransformController : IContentRoot
    {
        private IContent _target;// Can be either a morphController or a mesh, or light or camera

        private NodeBuilder _Node;
    }

    class SkinController : IContentRoot
    {
        private IRenderableContent _Target; // Can be either a morphController or a mesh

        // condition: all NodeBuilder objects must have the same root.
        private readonly List<NodeBuilder> _Joints = new List<NodeBuilder>();
    }

    class MorphModifier : IRenderableContent // must be a child of a controller, and the parent of a mesh
    {
        private IRenderableContent _Target; // must be a mesh

        // morph targets here

        private readonly Animations.Animatable<Vector4> _MorphWeights = new Animations.Animatable<Vector4>();
    }

    class MeshContent : IRenderableContent
    {
        private Geometry.IMeshBuilder<Materials.MaterialBuilder> _Geometry;
    }

    class LightContent : IContent
    {

    }

    class CameraContent : IContent
    {

    }
}
