using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace SharpGLTF.Schema2
{
    public static partial class Toolkit
    {
        public static Node WithLocalTranslation(this Node node, Vector3 translation)
        {
            var xform = node.LocalTransform;
            xform.Translation = translation;
            node.LocalTransform = xform;

            return node;
        }

        public static Node WithLocalRotation(this Node node, Quaternion rotation)
        {
            var xform = node.LocalTransform;
            xform.Rotation = rotation;
            node.LocalTransform = xform;

            return node;
        }

        public static Node WithLocalScale(this Node node, Vector3 scale)
        {
            var xform = node.LocalTransform;
            xform.Scale = scale;
            node.LocalTransform = xform;

            return node;
        }
    }
}
