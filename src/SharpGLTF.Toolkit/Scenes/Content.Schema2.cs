using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using SharpGLTF.Schema2;

using SCHEMA2NODE = SharpGLTF.Scenes.Schema2SceneBuilder.IOperator<SharpGLTF.Schema2.Node>;

namespace SharpGLTF.Scenes
{
    partial class MorphableMeshContent : SCHEMA2NODE
    {
        void SCHEMA2NODE.Setup(Node dstNode, Schema2SceneBuilder context)
        {
            if (!(_Target is SCHEMA2NODE schema2Target)) return;

            schema2Target.Setup(dstNode, context);

            // setup morphs here!
        }
    }

    partial class MeshContent : SCHEMA2NODE
    {
        void SCHEMA2NODE.Setup(Node dstNode, Schema2SceneBuilder context)
        {
            dstNode.Mesh = context.GetMesh(_Mesh);
        }
    }

    partial class OrthographicCameraContent : SCHEMA2NODE
    {
        void SCHEMA2NODE.Setup(Node dstNode, Schema2SceneBuilder context)
        {
            dstNode.WithOrthographicCamera(_XMag, _YMag, _ZNear, _ZFar);
        }
    }

    partial class PerspectiveCameraContent : SCHEMA2NODE
    {
        void SCHEMA2NODE.Setup(Node dstNode, Schema2SceneBuilder context)
        {
            dstNode.WithPerspectiveCamera(_AspectRatio, _FovY, _ZNear, _ZFar);
        }
    }
}
