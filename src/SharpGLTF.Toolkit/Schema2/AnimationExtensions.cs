using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;

namespace SharpGLTF.Schema2
{
    public static partial class Toolkit
    {
        public static Animation UseAnimation(this ModelRoot root, string name)
        {
            Guard.NotNull(root, nameof(root));

            var animation = root.LogicalAnimations.FirstOrDefault(item => item.Name == name);

            return animation ?? root.CreateAnimation(name);
        }

        public static Node WithScaleAnimation(this Node node, string animationName, Animations.ICurveSampler<Vector3> sampler)
        {
            Guard.NotNull(node, nameof(node));

            if (sampler is Animations.IConvertibleCurve<Vector3> curve)
            {
                var animation = node.LogicalParent.UseAnimation(animationName);

                var degree = curve.MaxDegree;
                if (degree == 0) animation.CreateScaleChannel(node, curve.ToStepCurve(), false);
                if (degree == 1) animation.CreateScaleChannel(node, curve.ToLinearCurve(), true);
                if (degree == 3) animation.CreateScaleChannel(node, curve.ToSplineCurve());
            }
            else
            {
                throw new ArgumentException("Must implement IConvertibleCurve<Vector3>", nameof(sampler));
            }

            return node;
        }

        public static Node WithTranslationAnimation(this Node node, string animationName, Animations.ICurveSampler<Vector3> sampler)
        {
            Guard.NotNull(node, nameof(node));

            if (sampler is Animations.IConvertibleCurve<Vector3> curve)
            {
                var animation = node.LogicalParent.UseAnimation(animationName);

                var degree = curve.MaxDegree;
                if (degree == 0) animation.CreateTranslationChannel(node, curve.ToStepCurve(), false);
                if (degree == 1) animation.CreateTranslationChannel(node, curve.ToLinearCurve(), true);
                if (degree == 3) animation.CreateTranslationChannel(node, curve.ToSplineCurve());
            }
            else
            {
                throw new ArgumentException("Must implement IConvertibleCurve<Vector3>", nameof(sampler));
            }

            return node;
        }

        public static Node WithMorphingAnimation(this Node node, string animationName, Animations.ICurveSampler<Transforms.SparseWeight8> sampler)
        {
            Guard.NotNull(node, nameof(node));
            Guard.NotNull(node.MorphWeights, nameof(node.MorphWeights), "Set node.MorphWeights before setting morphing animation");
            Guard.MustBeGreaterThanOrEqualTo(node.MorphWeights.Count, 0, nameof(node.MorphWeights));

            if (sampler is Animations.IConvertibleCurve<Transforms.SparseWeight8> curve)
            {
                var animation = node.LogicalParent.UseAnimation(animationName);

                var degree = curve.MaxDegree;
                if (degree == 0) animation.CreateMorphChannel(node, curve.ToStepCurve(), node.MorphWeights.Count, false);
                if (degree == 1) animation.CreateMorphChannel(node, curve.ToLinearCurve(), node.MorphWeights.Count, true);
                if (degree == 3) animation.CreateMorphChannel(node, curve.ToSplineCurve(), node.MorphWeights.Count);
            }

            return node;
        }

        public static Node WithRotationAnimation(this Node node, string animationName, Animations.ICurveSampler<Quaternion> sampler)
        {
            Guard.NotNull(node, nameof(node));

            if (sampler is Animations.IConvertibleCurve<Quaternion> curve)
            {
                var animation = node.LogicalParent.UseAnimation(animationName);

                var degree = curve.MaxDegree;
                if (degree == 0) animation.CreateRotationChannel(node, curve.ToStepCurve(), false);
                if (degree == 1) animation.CreateRotationChannel(node, curve.ToLinearCurve(), true);
                if (degree == 3) animation.CreateRotationChannel(node, curve.ToSplineCurve());
            }
            else
            {
                throw new ArgumentException("Must implement IConvertibleCurve<Quaternion>", nameof(sampler));
            }

            return node;
        }

        public static Node WithScaleAnimation(this Node node, string animationName, params (Single Key, Vector3 Value)[] keyframes)
        {
            Guard.NotNull(node, nameof(node));
            Guard.NotNullOrEmpty(keyframes, nameof(keyframes));

            var keys = keyframes.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

            return node.WithScaleAnimation(animationName, keys);
        }

        public static Node WithRotationAnimation(this Node node, string animationName, params (Single Key, Quaternion Value)[] keyframes)
        {
            Guard.NotNull(node, nameof(node));
            Guard.NotNullOrEmpty(keyframes, nameof(keyframes));

            var keys = keyframes.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

            return node.WithRotationAnimation(animationName, keys);
        }

        public static Node WithTranslationAnimation(this Node node, string animationName, params (Single Key, Vector3 Value)[] keyframes)
        {
            Guard.NotNull(node, nameof(node));
            Guard.NotNullOrEmpty(keyframes, nameof(keyframes));

            var keys = keyframes.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

            return node.WithTranslationAnimation(animationName, keys);
        }

        public static Node WithScaleAnimation(this Node node, string animationName, IReadOnlyDictionary<Single, Vector3> keyframes)
        {
            Guard.NotNull(node, nameof(node));
            Guard.NotNullOrEmpty(keyframes, nameof(keyframes));

            var root = node.LogicalParent;

            var animation = root.UseAnimation(animationName);

            animation.CreateScaleChannel(node, keyframes);

            return node;
        }

        public static Node WithRotationAnimation(this Node node, string animationName, IReadOnlyDictionary<Single, Quaternion> keyframes)
        {
            Guard.NotNull(node, nameof(node));
            Guard.NotNullOrEmpty(keyframes, nameof(keyframes));

            var root = node.LogicalParent;

            var animation = root.UseAnimation(animationName);

            animation.CreateRotationChannel(node, keyframes);

            return node;
        }

        public static Node WithTranslationAnimation(this Node node, string animationName, IReadOnlyDictionary<Single, Vector3> keyframes)
        {
            Guard.NotNull(node, nameof(node));
            Guard.NotNullOrEmpty(keyframes, nameof(keyframes));

            var root = node.LogicalParent;

            var animation = root.UseAnimation(animationName);

            animation.CreateTranslationChannel(node, keyframes);

            return node;
        }
    }
}
