using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using SharpGLTF.Schema2;

using SCHEMA2NODE = SharpGLTF.Scenes.Schema2SceneBuilder.IOperator<SharpGLTF.Schema2.Node>;
using SCHEMA2SCENE = SharpGLTF.Scenes.Schema2SceneBuilder.IOperator<SharpGLTF.Schema2.Scene>;

namespace SharpGLTF.Scenes
{
    partial class FixedTransformer : SCHEMA2SCENE
    {
        void SCHEMA2SCENE.Setup(Scene dstScene, Schema2SceneBuilder context)
        {
            if (!(Content is SCHEMA2NODE schema2Target)) return;

            var dstNode = dstScene.CreateNode();

            dstNode.Name = _NodeName;
            dstNode.LocalMatrix = _WorldTransform;

            schema2Target.Setup(dstNode, context);

            Schema2SceneBuilder.SetMorphAnimation(dstNode, this.Morphings);
        }
    }

    partial class RigidTransformer : SCHEMA2SCENE
    {
        void SCHEMA2SCENE.Setup(Scene dstScene, Schema2SceneBuilder context)
        {
            if (!(Content is SCHEMA2NODE schema2Target)) return;

            var dstNode = context.GetNode(_Node);

            schema2Target.Setup(dstNode, context);

            Schema2SceneBuilder.SetMorphAnimation(dstNode, this.Morphings);
        }
    }

    partial class SkinnedTransformer : SCHEMA2SCENE
    {
        void SCHEMA2SCENE.Setup(Scene dstScene, Schema2SceneBuilder context)
        {
            if (!(Content is SCHEMA2NODE schema2Target)) return;

            var skinnedMeshNode = dstScene.CreateNode();
            skinnedMeshNode.Name = _NodeName;

            if (_MeshPoseWorldMatrix.HasValue)
            {
                var dstNodes = new Node[_Joints.Count];

                for (int i = 0; i < dstNodes.Length; ++i)
                {
                    var (joints, inverseBindMatrix) = _Joints[i];

                    System.Diagnostics.Debug.Assert(!inverseBindMatrix.HasValue);

                    dstNodes[i] = context.GetNode(joints);
                }

                #if DEBUG
                for (int i = 0; i < dstNodes.Length; ++i)
                {
                    var (joints, inverseBindMatrix) = _Joints[i];
                    System.Diagnostics.Debug.Assert(dstNodes[i].WorldMatrix == joints.WorldMatrix);
                }
                #endif

                skinnedMeshNode.WithSkinBinding(_MeshPoseWorldMatrix.Value, dstNodes);
            }
            else
            {
                var skinnedJoints = _Joints
                .Select(j => (context.GetNode(j.Joint), j.InverseBindMatrix.Value))
                .ToArray();

                skinnedMeshNode.WithSkinBinding(skinnedJoints);
            }

            // set skeleton
            // var root = _Joints[0].Joint.Root;
            // skinnedMeshNode.Skin.Skeleton = context.GetNode(root);

            schema2Target.Setup(skinnedMeshNode, context);

            Schema2SceneBuilder.SetMorphAnimation(skinnedMeshNode, this.Morphings);
        }
    }
}