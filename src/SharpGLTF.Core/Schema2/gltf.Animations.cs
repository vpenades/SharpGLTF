using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.Numerics;

using SharpGLTF.Collections;
using SharpGLTF.Transforms;
using SharpGLTF.Animations;
using SharpGLTF.Validation;

namespace SharpGLTF.Schema2
{
    [System.Diagnostics.DebuggerDisplay("Animation[{LogicalIndex}] {Name}")]
    public sealed partial class Animation
    {
        #region lifecycle

        internal Animation()
        {
            _channels = new ChildrenCollection<AnimationChannel, Animation>(this);
            _samplers = new ChildrenCollection<AnimationSampler, Animation>(this);
        }

        #endregion

        #region properties

        /// <summary>
        /// Gets the zero-based index of this <see cref="Animation"/> at <see cref="ModelRoot.LogicalAnimations"/>
        /// </summary>
        public int LogicalIndex => this.LogicalParent.LogicalAnimations.IndexOfReference(this);

        internal IReadOnlyList<AnimationSampler> _Samplers => _samplers;

        internal IReadOnlyList<AnimationChannel> _Channels => _channels;

        public float Duration => _samplers.Select(item => item.Duration).Max();

        #endregion

        #region API

        protected override IEnumerable<ExtraProperties> GetLogicalChildren()
        {
            return base.GetLogicalChildren().Concat(_samplers).Concat(_channels);
        }

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
            Guard.MustShareLogicalParent(this, node, nameof(node));

            var channel = _channels.FirstOrDefault(item => item.TargetNode == node && item.TargetNodePath == path);
            if (channel != null) return channel;

            channel = new AnimationChannel(node, path);

            _channels.Add(channel);

            return channel;
        }

        public void CreateScaleChannel(Node node, IReadOnlyDictionary<Single, Vector3> keyframes, bool linear = true)
        {
            var sampler = this._CreateSampler(linear ? AnimationInterpolationMode.LINEAR : AnimationInterpolationMode.STEP);

            sampler.SetKeys(keyframes);

            this._UseChannel(node, PropertyPath.scale)
                .SetSampler(sampler);
        }

        public void CreateScaleChannel(Node node, IReadOnlyDictionary<Single, (Vector3 TangentIn, Vector3 Value, Vector3 TangentOut)> keyframes)
        {
            var sampler = this._CreateSampler(AnimationInterpolationMode.CUBICSPLINE);

            sampler.SetKeys(keyframes);

            this._UseChannel(node, PropertyPath.scale)
                .SetSampler(sampler);
        }

        public void CreateRotationChannel(Node node, IReadOnlyDictionary<Single, Quaternion> keyframes, bool linear = true)
        {
            var sampler = this._CreateSampler(linear ? AnimationInterpolationMode.LINEAR : AnimationInterpolationMode.STEP);

            sampler.SetKeys(keyframes);

            this._UseChannel(node, PropertyPath.rotation)
                .SetSampler(sampler);
        }

        public void CreateRotationChannel(Node node, IReadOnlyDictionary<Single, (Quaternion TangentIn, Quaternion Value, Quaternion TangentOut)> keyframes)
        {
            var sampler = this._CreateSampler(AnimationInterpolationMode.CUBICSPLINE);

            sampler.SetKeys(keyframes);

            this._UseChannel(node, PropertyPath.rotation)
                .SetSampler(sampler);
        }

        public void CreateTranslationChannel(Node node, IReadOnlyDictionary<Single, Vector3> keyframes, bool linear = true)
        {
            var sampler = this._CreateSampler(linear ? AnimationInterpolationMode.LINEAR : AnimationInterpolationMode.STEP);

            sampler.SetKeys(keyframes);

            this._UseChannel(node, PropertyPath.translation)
                .SetSampler(sampler);
        }

        public void CreateTranslationChannel(Node node, IReadOnlyDictionary<Single, (Vector3 TangentIn, Vector3 Value, Vector3 TangentOut)> keyframes)
        {
            var sampler = this._CreateSampler(AnimationInterpolationMode.CUBICSPLINE);

            sampler.SetKeys(keyframes);

            this._UseChannel(node, PropertyPath.translation)
                .SetSampler(sampler);
        }

        public void CreateMorphChannel(Node node, IReadOnlyDictionary<Single, SparseWeight8> keyframes, int morphCount, bool linear = true)
        {
            var sampler = this._CreateSampler(linear ? AnimationInterpolationMode.LINEAR : AnimationInterpolationMode.STEP);

            sampler.SetKeys(keyframes, morphCount);

            this._UseChannel(node, PropertyPath.weights)
                .SetSampler(sampler);
        }

        public void CreateMorphChannel(Node node, IReadOnlyDictionary<Single, (SparseWeight8 TangentIn, SparseWeight8 Value, SparseWeight8 TangentOut)> keyframes, int morphCount)
        {
            var sampler = this._CreateSampler(AnimationInterpolationMode.CUBICSPLINE);

            sampler.SetKeys(keyframes, morphCount);

            this._UseChannel(node, PropertyPath.weights)
                .SetSampler(sampler);
        }

        private AnimationChannel FindChannel(Node node, PropertyPath path)
        {
            return _channels.FirstOrDefault(item => item.TargetNode == node && item.TargetNodePath == path);
        }

        public IAnimationSampler<Vector3> FindScaleSampler(Node node) { return FindChannel(node, PropertyPath.scale)?.Sampler; }

        public IAnimationSampler<Quaternion> FindRotationSampler(Node node) { return FindChannel(node, PropertyPath.rotation)?.Sampler; }

        public IAnimationSampler<Vector3> FindTranslationSampler(Node node) { return FindChannel(node, PropertyPath.translation)?.Sampler; }

        public IAnimationSampler<Single[]> FindMorphSampler(Node node) { return FindChannel(node, PropertyPath.weights)?.Sampler; }

        public IAnimationSampler<SparseWeight8> FindSparseMorphSampler(Node node) { return FindChannel(node, PropertyPath.weights)?.Sampler; }

        public AffineTransform GetLocalTransform(Node node, Single time)
        {
            Guard.NotNull(node, nameof(node));
            Guard.MustShareLogicalParent(this, node, nameof(node));

            var xform = node.LocalTransform;

            var sfunc = FindScaleSampler(node)?.CreateCurveSampler();
            var rfunc = FindRotationSampler(node)?.CreateCurveSampler();
            var tfunc = FindTranslationSampler(node)?.CreateCurveSampler();

            if (sfunc != null) xform.Scale = sfunc.GetPoint(time);
            if (rfunc != null) xform.Rotation = rfunc.GetPoint(time);
            if (tfunc != null) xform.Translation = tfunc.GetPoint(time);

            return xform;
        }

        public IReadOnlyList<float> GetMorphWeights(Node node, Single time)
        {
            Guard.NotNull(node, nameof(node));

            var morphWeights = node.MorphWeights;
            if (morphWeights == null || morphWeights.Count == 0) return morphWeights;

            Guard.MustShareLogicalParent(this, node, nameof(node));

            var mfunc = FindMorphSampler(node)?.CreateCurveSampler();
            if (mfunc == null) return morphWeights;

            return mfunc.GetPoint(time);
        }

        public SparseWeight8 GetSparseMorphWeights(Node node, Single time)
        {
            Guard.NotNull(node, nameof(node));

            var morphWeights = node.MorphWeights;
            if (morphWeights == null || morphWeights.Count == 0) return default;

            Guard.MustShareLogicalParent(this, node, nameof(node));

            var mfunc = FindSparseMorphSampler(node)?.CreateCurveSampler();
            if (mfunc == null) return default;

            return mfunc.GetPoint(time);
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

    sealed partial class AnimationChannelTarget
    {
        #region lifecycle

        internal AnimationChannelTarget() { }

        internal AnimationChannelTarget(Node targetNode, PropertyPath targetPath)
        {
            _node = targetNode.LogicalIndex;
            _path = targetPath;
        }

        #endregion

        #region data

        [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
        internal int? _NodeId => this._node;

        [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
        internal PropertyPath _NodePath => this._path;

        #endregion

        #region Validation

        protected override void OnValidateReferences(ValidationContext validate)
        {
            base.OnValidateReferences(validate);

            validate.IsNullOrIndex("Node", _node, validate.Root.LogicalNodes);
        }

        #endregion
    }

    [System.Diagnostics.DebuggerDisplay("AnimChannel LogicalNode[{TargetNode.LogicalIndex}].{TargetNodePath}")]
    sealed partial class AnimationChannel : IChildOf<Animation>
    {
        #region lifecycle

        internal AnimationChannel() { }

        internal AnimationChannel(Node targetNode, PropertyPath targetPath)
        {
            _target = new AnimationChannelTarget(targetNode, targetPath);
            _sampler = -1;
        }

        internal void SetSampler(AnimationSampler sampler)
        {
            Guard.NotNull(sampler, nameof(sampler));
            Guard.IsTrue(this.LogicalParent == sampler.LogicalParent, nameof(sampler));

            _sampler = sampler.LogicalIndex;
        }

        #endregion

        #region properties

        /// <summary>
        /// Gets the <see cref="Animation"/> instance that owns this object.
        /// </summary>
        [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
        public Animation LogicalParent { get; private set; }

        void IChildOf<Animation>._SetLogicalParent(Animation parent) { LogicalParent = parent; }

        /// <summary>
        /// Gets the <see cref="AnimationSampler"/> instance used by this <see cref="AnimationChannel"/>.
        /// </summary>
        public AnimationSampler Sampler => this.LogicalParent._Samplers[this._sampler];

        public Node TargetNode
        {
            get
            {
                var idx = this._target?._NodeId ?? -1;
                if (idx < 0) return null;
                return this.LogicalParent.LogicalParent.LogicalNodes[idx];
            }
        }

        public PropertyPath TargetNodePath => this._target?._NodePath ?? PropertyPath.translation;

        #endregion

        #region Validation

        protected override void OnValidateReferences(ValidationContext validate)
        {
            base.OnValidateReferences(validate);

            validate.IsNullOrIndex("Sampler", _sampler, this.LogicalParent._Samplers);
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