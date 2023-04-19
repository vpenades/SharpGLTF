using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.Numerics;

using SharpGLTF.Collections;
using SharpGLTF.Transforms;
using SharpGLTF.Animations;
using SharpGLTF.Validation;

using WEIGHTS = System.Collections.Generic.IReadOnlyList<float>;

namespace SharpGLTF.Schema2
{
    [System.Diagnostics.DebuggerDisplay("Animation[{LogicalIndex}] {Name}")]
    public sealed partial class Animation
    {
        #region lifecycle

        internal Animation()
        {
            _channels = new ChildrenList<AnimationChannel, Animation>(this);
            _samplers = new ChildrenList<AnimationSampler, Animation>(this);
        }

        #endregion

        #region properties

        internal IReadOnlyList<AnimationSampler> _Samplers => _samplers;

        public IReadOnlyList<AnimationChannel> Channels => _channels;

        public float Duration => _samplers.Select(item => item.Duration).Max();

        #endregion

        #region API

        protected override IEnumerable<ExtraProperties> GetLogicalChildren()
        {
            return base.GetLogicalChildren().Concat(_samplers).Concat(_channels);
        }

        public IEnumerable<AnimationChannel> FindChannels(Node node)
        {
            Guard.NotNull(node, nameof(node));
            Guard.MustShareLogicalParent(this, node, nameof(node));

            return Channels.Where(item => item.TargetNode == node);
        }

        private AnimationChannel _FindChannel(Node node, PropertyPath path)
        {
            return FindChannels(node).FirstOrDefault(item => item.TargetNodePath == path);
        }

        public AnimationChannel FindScaleChannel(Node node) => _FindChannel(node, PropertyPath.scale);
        public AnimationChannel FindRotationChannel(Node node) => _FindChannel(node, PropertyPath.rotation);
        public AnimationChannel FindTranslationChannel(Node node) => _FindChannel(node, PropertyPath.translation);
        public AnimationChannel FindMorphChannel(Node node) => _FindChannel(node, PropertyPath.weights);

        #endregion

        #region API - Create

        private AnimationSampler _CreateSampler(AnimationInterpolationMode interpolation)
        {
            var sampler = new AnimationSampler(interpolation);

            _samplers.Add(sampler);

            return sampler;
        }

        /// <remarks>
        /// There can only be one <see cref="AnimationChannel"/> for every node and path
        /// </remarks>
        private AnimationChannel _UseChannel(Node node, PropertyPath path)
        {
            Guard.NotNull(node, nameof(node));
            Guard.MustShareLogicalParent(this, node, nameof(node));

            var channel = _channels.FirstOrDefault(item => item.TargetNode == node && item.TargetNodePath == path);
            if (channel != null) return channel;

            channel = new AnimationChannel(node, path);

            _channels.Add(channel);

            return channel;
        }

        public void CreateScaleChannel(Node node, IReadOnlyDictionary<Single, Vector3> keyframes, bool linear = true)
        {
            Guard.NotNull(node, nameof(node));
            Guard.MustShareLogicalParent(this, node, nameof(node));
            Guard.NotNullOrEmpty(keyframes, nameof(keyframes));

            var sampler = this._CreateSampler(linear ? AnimationInterpolationMode.LINEAR : AnimationInterpolationMode.STEP);

            sampler.SetKeys(keyframes);

            this._UseChannel(node, PropertyPath.scale)
                .SetSampler(sampler);
        }

        public void CreateScaleChannel(Node node, IReadOnlyDictionary<Single, (Vector3 TangentIn, Vector3 Value, Vector3 TangentOut)> keyframes)
        {
            Guard.NotNull(node, nameof(node));
            Guard.MustShareLogicalParent(this, node, nameof(node));
            Guard.NotNullOrEmpty(keyframes, nameof(keyframes));

            var sampler = this._CreateSampler(AnimationInterpolationMode.CUBICSPLINE);

            sampler.SetCubicKeys(keyframes);

            this._UseChannel(node, PropertyPath.scale)
                .SetSampler(sampler);
        }

        public void CreateRotationChannel(Node node, IReadOnlyDictionary<Single, Quaternion> keyframes, bool linear = true)
        {
            Guard.NotNull(node, nameof(node));
            Guard.MustShareLogicalParent(this, node, nameof(node));
            Guard.NotNullOrEmpty(keyframes, nameof(keyframes));

            var sampler = this._CreateSampler(linear ? AnimationInterpolationMode.LINEAR : AnimationInterpolationMode.STEP);

            sampler.SetKeys(keyframes);

            this._UseChannel(node, PropertyPath.rotation)
                .SetSampler(sampler);
        }

        public void CreateRotationChannel(Node node, IReadOnlyDictionary<Single, (Quaternion TangentIn, Quaternion Value, Quaternion TangentOut)> keyframes)
        {
            Guard.NotNull(node, nameof(node));
            Guard.MustShareLogicalParent(this, node, nameof(node));
            Guard.NotNullOrEmpty(keyframes, nameof(keyframes));

            var sampler = this._CreateSampler(AnimationInterpolationMode.CUBICSPLINE);

            sampler.SetCubicKeys(keyframes);

            this._UseChannel(node, PropertyPath.rotation)
                .SetSampler(sampler);
        }

        public void CreateTranslationChannel(Node node, IReadOnlyDictionary<Single, Vector3> keyframes, bool linear = true)
        {
            Guard.NotNull(node, nameof(node));
            Guard.MustShareLogicalParent(this, node, nameof(node));
            Guard.NotNullOrEmpty(keyframes, nameof(keyframes));

            var sampler = this._CreateSampler(linear ? AnimationInterpolationMode.LINEAR : AnimationInterpolationMode.STEP);

            sampler.SetKeys(keyframes);

            this._UseChannel(node, PropertyPath.translation)
                .SetSampler(sampler);
        }

        public void CreateTranslationChannel(Node node, IReadOnlyDictionary<Single, (Vector3 TangentIn, Vector3 Value, Vector3 TangentOut)> keyframes)
        {
            Guard.NotNull(node, nameof(node));
            Guard.MustShareLogicalParent(this, node, nameof(node));
            Guard.NotNullOrEmpty(keyframes, nameof(keyframes));

            var sampler = this._CreateSampler(AnimationInterpolationMode.CUBICSPLINE);

            sampler.SetCubicKeys(keyframes);

            this._UseChannel(node, PropertyPath.translation)
                .SetSampler(sampler);
        }

        public void CreateMorphChannel<TWeights>(Node node, IReadOnlyDictionary<Single, TWeights> keyframes, int morphCount, bool linear = true)
            where TWeights : WEIGHTS
        {
            Guard.NotNull(node, nameof(node));
            Guard.MustShareLogicalParent(this, node, nameof(node));
            Guard.NotNullOrEmpty(keyframes, nameof(keyframes));

            var sampler = this._CreateSampler(linear ? AnimationInterpolationMode.LINEAR : AnimationInterpolationMode.STEP);

            sampler.SetKeys(keyframes, morphCount);

            this._UseChannel(node, PropertyPath.weights)
                .SetSampler(sampler);
        }

        public void CreateMorphChannel<TWeights>(Node node, IReadOnlyDictionary<Single, (TWeights TangentIn, TWeights Value, TWeights TangentOut)> keyframes, int morphCount)
            where TWeights : WEIGHTS
        {
            Guard.NotNull(node, nameof(node));
            Guard.MustShareLogicalParent(this, node, nameof(node));
            Guard.NotNullOrEmpty(keyframes, nameof(keyframes));

            var sampler = this._CreateSampler(AnimationInterpolationMode.CUBICSPLINE);

            sampler.SetCubicKeys(keyframes, morphCount);

            this._UseChannel(node, PropertyPath.weights)
                .SetSampler(sampler);
        }

        public void CreateMorphChannel(Node node, IReadOnlyDictionary<Single, SparseWeight8> keyframes, int morphCount, bool linear = true)
        {
            Guard.NotNull(node, nameof(node));
            Guard.MustShareLogicalParent(this, node, nameof(node));
            Guard.NotNullOrEmpty(keyframes, nameof(keyframes));

            var sampler = this._CreateSampler(linear ? AnimationInterpolationMode.LINEAR : AnimationInterpolationMode.STEP);

            sampler.SetKeys(keyframes, morphCount);

            this._UseChannel(node, PropertyPath.weights)
                .SetSampler(sampler);
        }

        public void CreateMorphChannel(Node node, IReadOnlyDictionary<Single, (SparseWeight8 TangentIn, SparseWeight8 Value, SparseWeight8 TangentOut)> keyframes, int morphCount)
        {
            Guard.NotNull(node, nameof(node));
            Guard.MustShareLogicalParent(this, node, nameof(node));
            Guard.NotNullOrEmpty(keyframes, nameof(keyframes));

            var sampler = this._CreateSampler(AnimationInterpolationMode.CUBICSPLINE);

            sampler.SetCubicKeys(keyframes, morphCount);

            this._UseChannel(node, PropertyPath.weights)
                .SetSampler(sampler);
        }

        #endregion

        #region Validation

        protected override void OnValidateReferences(ValidationContext validate)
        {
            validate
                .IsSetCollection("Samplers", _samplers)
                .IsSetCollection("Channels", _channels);

            base.OnValidateReferences(validate);
        }

        protected override void OnValidateContent(ValidationContext validate)
        {
            base.OnValidateContent(validate);
        }

        #endregion
    }

    public sealed partial class ModelRoot
    {
        /// <summary>
        /// Creates a new <see cref="Animation"/> instance and adds it to <see cref="ModelRoot.LogicalAnimations"/>.
        /// </summary>
        /// <param name="name">The name of the instance.</param>
        /// <returns>A <see cref="Animation"/> instance.</returns>
        public Animation CreateAnimation(string name = null)
        {
            var anim = new Animation
            {
                Name = name
            };

            _animations.Add(anim);

            return anim;
        }
    }
}