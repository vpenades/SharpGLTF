using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;

namespace SharpGLTF.Schema2
{
    public static partial class Schema2Toolkit
    {
        public static Animation UseAnimation(this ModelRoot root, string name)
        {
            var animation = root.LogicalAnimations.FirstOrDefault(item => item.Name == name);

            return animation ?? root.CreateAnimation(name);
        }

        public static Node WithScaleAnimation(this Node node, string animationName, IReadOnlyDictionary<Single, Vector3> keyframes)
        {
            var root = node.LogicalParent;

            var animation = root.UseAnimation(animationName);

            animation.CreateScaleChannel(node, keyframes);

            return node;
        }

        public static Node WithRotationAnimation(this Node node, string animationName, IReadOnlyDictionary<Single, Quaternion> keyframes)
        {
            var root = node.LogicalParent;

            var animation = root.UseAnimation(animationName);

            animation.CreateRotationChannel(node, keyframes);

            return node;
        }

        public static Node WithTranslationAnimation(this Node node, string animationName, IReadOnlyDictionary<Single, Vector3> keyframes)
        {
            var root = node.LogicalParent;

            var animation = root.UseAnimation(animationName);

            animation.CreateTranslationChannel(node, keyframes);

            return node;
        }
    }
}
