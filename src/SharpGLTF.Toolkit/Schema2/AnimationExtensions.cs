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

        public static Node WithScaleAnimation(this Node node, string animationName, Animations.ICurveSampler<Vector3> curve)
        {
            var animation = node
                .LogicalParent
                .UseAnimation(animationName);

            if (curve is Animations.ISplineCurve<Vector3> spline)
            {
                animation.CreateScaleChannel(node, spline.ToDictionary());
                return node;
            }

            if (curve is Animations.ILinearCurve<Vector3> linear)
            {
                animation.CreateScaleChannel(node, linear.ToDictionary());
                return node;
            }

            throw new ArgumentException("Not supported", nameof(curve));
        }

        public static Node WithTranslationAnimation(this Node node, string animationName, Animations.ICurveSampler<Vector3> curve)
        {
            var animation = node
                .LogicalParent
                .UseAnimation(animationName);

            if (curve is Animations.ISplineCurve<Vector3> spline)
            {
                animation.CreateTranslationChannel(node, spline.ToDictionary());
                return node;
            }

            if (curve is Animations.ILinearCurve<Vector3> linear)
            {
                animation.CreateTranslationChannel(node, linear.ToDictionary());
                return node;
            }

            throw new ArgumentException("Not supported", nameof(curve));
        }

        public static Node WithRotationAnimation(this Node node, string animationName, Animations.ICurveSampler<Quaternion> curve)
        {
            var animation = node
                .LogicalParent
                .UseAnimation(animationName);

            if (curve is Animations.ISplineCurve<Quaternion> spline)
            {
                animation.CreateRotationChannel(node, spline.ToDictionary());
                return node;
            }

            if (curve is Animations.ILinearCurve<Quaternion> linear)
            {
                animation.CreateRotationChannel(node, linear.ToDictionary());
                return node;
            }

            throw new ArgumentException("Not supported", nameof(curve));
        }

        public static Node WithScaleAnimation(this Node node, string animationName, params (Single, Vector3)[] keyframes)
        {
            return node.WithScaleAnimation(animationName, keyframes.ToDictionary(kvp => kvp.Item1, kvp => kvp.Item2));
        }

        public static Node WithRotationAnimation(this Node node, string animationName, params (Single, Quaternion)[] keyframes)
        {
            return node.WithRotationAnimation(animationName, keyframes.ToDictionary(kvp => kvp.Item1, kvp => kvp.Item2));
        }

        public static Node WithTranslationAnimation(this Node node, string animationName, params (Single, Vector3)[] keyframes)
        {
            return node.WithTranslationAnimation(animationName, keyframes.ToDictionary(kvp => kvp.Item1, kvp => kvp.Item2));
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
