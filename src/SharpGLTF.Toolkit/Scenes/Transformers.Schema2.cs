using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using SharpGLTF.Schema2;

using SCHEMA2NODE = SharpGLTF.Scenes.Schema2SceneBuilder.IOperator<SharpGLTF.Schema2.Node>;
using SCHEMA2SCENE = SharpGLTF.Scenes.Schema2SceneBuilder.IOperator<SharpGLTF.Schema2.Scene>;

namespace SharpGLTF.Scenes
{
    partial class StaticTransformer : SCHEMA2SCENE
    {
        void SCHEMA2SCENE.Setup(Scene dstScene, Schema2SceneBuilder context)
        {
            if (!(Content is SCHEMA2NODE schema2Target)) return;

            var dstNode = dstScene.CreateNode();

            dstNode.LocalMatrix = _WorldTransform;

            schema2Target.Setup(dstNode, context);
        }
    }

    partial class NodeTransformer : SCHEMA2SCENE
    {
        void SCHEMA2SCENE.Setup(Scene dstScene, Schema2SceneBuilder context)
        {
            if (!(Content is SCHEMA2NODE schema2Target)) return;

            var dstNode = context.GetNode(_Node);

            schema2Target.Setup(dstNode, context);

            context.SetMorphAnimation(dstNode, this.Morphings);
        }
    }

    partial class SkinTransformer : SCHEMA2SCENE
    {
        void SCHEMA2SCENE.Setup(Scene dstScene, Schema2SceneBuilder context)
        {
            if (!(Content is SCHEMA2NODE schema2Target)) return;

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
                .Select(j => (context.GetNode(j.Item1), j.Item2.Value))
                .ToArray();

                skinnedMeshNode.WithSkinBinding(skinnedJoints);
            }

            // set skeleton
            // var root = _Joints[0].Item1.Root;
            // skinnedMeshNode.Skin.Skeleton = context.GetNode(root);

            schema2Target.Setup(skinnedMeshNode, context);

            context.SetMorphAnimation(skinnedMeshNode, this.Morphings);
        }
    }
}