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

        public static Node WithScaleAnimation(this Node node, string animationName, Animations.ICurveSampler<Vector3> sampler)
        {
            if (sampler is Animations.IConvertibleCurve<Vector3> curve)
            {
                var animation = node.LogicalParent.UseAnimation(animationName);

                var degree = curve.Degree;
                if (degree == 0) animation.CreateScaleChannel(node, curve.ToStepCurve(), false);
                if (degree == 1) animation.CreateScaleChannel(node, curve.ToLinearCurve(), true);
                if (degree == 3) animation.CreateScaleChannel(node, curve.ToSplineCurve());
            }

            return node;
        }

        public static Node WithTranslationAnimation(this Node node, string animationName, Animations.ICurveSampler<Vector3> sampler)
        {
            if (sampler is Animations.IConvertibleCurve<Vector3> curve)
            {
                var animation = node.LogicalParent.UseAnimation(animationName);

                var degree = curve.Degree;
                if (degree == 0) animation.CreateTranslationChannel(node, curve.ToStepCurve(), false);
                if (degree == 1) animation.CreateTranslationChannel(node, curve.ToLinearCurve(), true);
                if (degree == 3) animation.CreateTranslationChannel(node, curve.ToSplineCurve());
            }

            return node;
        }

        public static Node WithRotationAnimation(this Node node, string animationName, Animations.ICurveSampler<Quaternion> sampler)
        {
            if (sampler is Animations.IConvertibleCurve<Quaternion> curve)
            {
                var animation = node.LogicalParent.UseAnimation(animationName);

                var degree = curve.Degree;
                if (degree == 0) animation.CreateRotationChannel(node, curve.ToStepCurve(), false);
                if (degree == 1) animation.CreateRotationChannel(node, curve.ToLinearCurve(), true);
                if (degree == 3) animation.CreateRotationChannel(node, curve.ToSplineCurve());
            }

            return node;
        }

        public static Node WithScaleAnimation(this Node node, string animationName, params (Single, Vector3)[] keyframes)
        {
            var keys = keyframes.ToDictionary(kvp => kvp.Item1, kvp => kvp.Item2);

            return node.WithScaleAnimation(animationName, keys);
        }

        public static Node WithRotationAnimation(this Node node, string animationName, params (Single, Quaternion)[] keyframes)
        {
            var keys = keyframes.ToDictionary(kvp => kvp.Item1, kvp => kvp.Item2);

            return node.WithRotationAnimation(animationName, keys);
        }

        public static Node WithTranslationAnimation(this Node node, string animationName, params (Single, Vector3)[] keyframes)
        {
            var keys = keyframes.ToDictionary(kvp => kvp.Item1, kvp => kvp.Item2);

            return node.WithTranslationAnimation(animationName, keys);
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
