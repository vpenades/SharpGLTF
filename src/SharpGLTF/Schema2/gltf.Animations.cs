using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.Numerics;

namespace SharpGLTF.Schema2
{
    using Collections;

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

        #endregion

        #region API

        private AnimationSampler _CreateSampler(AnimationInterpolationMode interpolation)
        {
            var sampler = new AnimationSampler(interpolation);

            _samplers.Add(sampler);

            return sampler;
        }

        /// <remarks>
        /// There can only be one <see cref="AnimationChannel"/> for every node and path
        /// </remarks>
        private AnimationChannel _UseChannel(Node node, PathType path)
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
            var sampler = this._CreateSampler(linear ? AnimationInterpolationMode.LINEAR : AnimationInterpolationMode.STEP)
                .WithVector3Keys(keyframes);

            this._UseChannel(node, PathType.scale)
                .WithSampler(sampler);
        }

        public void CreateScaleChannel(Node node, IReadOnlyDictionary<Single, (Vector3, Vector3, Vector3)> keyframes)
        {
            var sampler = this._CreateSampler(AnimationInterpolationMode.CUBICSPLINE)
                .WithVector3Keys(keyframes);

            this._UseChannel(node, PathType.scale)
                .WithSampler(sampler);
        }

        public void CreateRotationChannel(Node node, IReadOnlyDictionary<Single, Quaternion> keyframes, bool linear = true)
        {
            var sampler = this._CreateSampler(linear ? AnimationInterpolationMode.LINEAR : AnimationInterpolationMode.STEP)
                .WithQuaternionKeys(keyframes);

            this._UseChannel(node, PathType.rotation)
                .WithSampler(sampler);
        }

        public void CreateRotationChannel(Node node, IReadOnlyDictionary<Single, (Quaternion, Quaternion, Quaternion)> keyframes)
        {
            var sampler = this._CreateSampler(AnimationInterpolationMode.CUBICSPLINE)
                .WithQuaternionKeys(keyframes);

            this._UseChannel(node, PathType.rotation)
                .WithSampler(sampler);
        }

        public void CreateTranslationChannel(Node node, IReadOnlyDictionary<Single, Vector3> keyframes, bool linear = true)
        {
            var sampler = this._CreateSampler(linear ? AnimationInterpolationMode.LINEAR : AnimationInterpolationMode.STEP)
                .WithVector3Keys(keyframes);

            this._UseChannel(node, PathType.translation)
                .WithSampler(sampler);
        }

        public void CreateTranslationChannel(Node node, IReadOnlyDictionary<Single, (Vector3, Vector3, Vector3)> keyframes)
        {
            var sampler = this._CreateSampler(AnimationInterpolationMode.CUBICSPLINE)
                .WithVector3Keys(keyframes);

            this._UseChannel(node, PathType.translation)
                .WithSampler(sampler);
        }

        public void CreateMorphChannel(Node node, AnimationInterpolationMode mode, IReadOnlyDictionary<Single, Single[]> keyframes)
        {
            throw new NotImplementedException();
        }

        public IReadOnlyDictionary<Single, Vector3> FindScaleChannel(Node node)
        {
            var channel = _channels.FirstOrDefault(item => item.TargetNode == node && item.TargetNodePath == PathType.scale);
            if (channel == null) return null;

            return channel.Sampler.AsVector3KeyFrames();
        }

        public IReadOnlyDictionary<Single, Quaternion> FindRotationChannel(Node node)
        {
            var channel = _channels.FirstOrDefault(item => item.TargetNode == node && item.TargetNodePath == PathType.rotation);
            if (channel == null) return null;

            return channel.Sampler.AsQuaternionKeyFrames();
        }

        public IReadOnlyDictionary<Single, Vector3> FindTranslationChannel(Node node)
        {
            var channel = _channels.FirstOrDefault(item => item.TargetNode == node && item.TargetNodePath == PathType.translation);
            if (channel == null) return null;

            return channel.Sampler.AsVector3KeyFrames();
        }

        #endregion
    }

    sealed partial class AnimationChannelTarget
    {
        #region lifecycle

        internal AnimationChannelTarget() { }

        internal AnimationChannelTarget(Node targetNode, PathType targetPath)
        {
            _node = targetNode.LogicalIndex;
            _path = targetPath;
        }

        #endregion

        #region data

        internal int? _NodeId => this._node;

        internal PathType _NodePath => this._path;

        #endregion
    }

    sealed partial class AnimationChannel : IChildOf<Animation>
    {
        #region lifecycle

        internal AnimationChannel() { }

        internal AnimationChannel(Node targetNode, PathType targetPath)
        {
            _target = new AnimationChannelTarget(targetNode, targetPath);
            _sampler = -1;
        }

        internal AnimationChannel WithSampler(AnimationSampler sampler)
        {
            Guard.NotNull(sampler, nameof(sampler));
            Guard.IsTrue(this.LogicalParent == sampler.LogicalParent, nameof(sampler));

            _sampler = sampler.LogicalIndex;

            return this;
        }

        #endregion

        #region properties

        /// <summary>
        /// Gets the <see cref="Animation"/> instance that owns this object.
        /// </summary>
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

        public PathType TargetNodePath => this._target?._NodePath ?? PathType.translation;

        public int OutputByteStride
        {
            get
            {
                switch (TargetNodePath)
                {
                    case PathType.translation: return 3;
                    case PathType.rotation: return 4;
                    case PathType.scale: return 3;
                    case PathType.weights: return TargetNode.MorphWeights.Count;
                    default: throw new NotImplementedException();
                }
            }
        }

        #endregion
    }

    sealed partial class AnimationSampler : IChildOf<Animation>
    {
        #region lifecycle

        internal AnimationSampler() { }

        internal AnimationSampler(AnimationInterpolationMode interpolation)
        {
            _interpolation = interpolation.AsNullable(_interpolationDefault);
        }

        #endregion

        #region properties

        /// <summary>
        /// Gets the <see cref="Animation"/> instance that owns this <see cref="AnimationSampler"/> instance.
        /// </summary>
        public Animation LogicalParent { get; private set; }

        /// <summary>
        /// Gets the zero-based index of this <see cref="AnimationSampler"/> at <see cref="Animation._samplers"/>.
        /// </summary>
        public int LogicalIndex => LogicalParent._Samplers.IndexOfReference(this);

        void IChildOf<Animation>._SetLogicalParent(Animation parent) { LogicalParent = parent; }

        public AnimationInterpolationMode InterpolationMode
        {
            get => _interpolation.AsValue(_interpolationDefault);
            set => _interpolation = value.AsNullable(_interpolationDefault);
        }

        public Accessor Input => this.LogicalParent.LogicalParent.LogicalAccessors[this._input];

        public Accessor Output => this.LogicalParent.LogicalParent.LogicalAccessors[this._output];

        #endregion

        #region API

        private Accessor _CreateInputAccessor(IReadOnlyList<Single> input)
        {
            var root = LogicalParent.LogicalParent;

            var buffer = root.UseBufferView(new Byte[input.Count * 4]);
            var accessor = root.CreateAccessor("Animation.Input")
                .WithData(buffer, 0, input.Count, ElementType.SCALAR, ComponentType.FLOAT, false);

            Memory.EncodedArrayUtils.FillFrom(accessor.AsScalarArray(), 0, input);

            accessor.UpdateBounds();

            return accessor;
        }

        private Accessor _CreateOutputAccessor(IReadOnlyList<Vector3> output)
        {
            var root = LogicalParent.LogicalParent;

            var buffer = root.UseBufferView(new Byte[output.Count * 4 * 3]);

            System.Diagnostics.Debug.Assert(buffer.ByteStride == 0);

            var accessor = root.CreateAccessor("Animation.Output")
                .WithData(buffer, 0, output.Count, ElementType.VEC3, ComponentType.FLOAT, false);

            Memory.EncodedArrayUtils.FillFrom(accessor.AsVector3Array(), 0, output);

            accessor.UpdateBounds();

            return accessor;
        }

        private Accessor _CreateOutputAccessor(IReadOnlyList<Quaternion> output)
        {
            var root = LogicalParent.LogicalParent;

            var buffer = root.UseBufferView(new Byte[output.Count * 4 * 4]);
            var accessor = root.CreateAccessor("Animation.Output")
                .WithData(buffer, 0, output.Count, ElementType.VEC4, ComponentType.FLOAT, false);

            Memory.EncodedArrayUtils.FillFrom(accessor.AsQuaternionArray(), 0, output);

            accessor.UpdateBounds();

            return accessor;
        }

        private static (Single[], TValue[]) _Split<TValue>(IReadOnlyDictionary<Single, TValue> keyframes)
        {
            var sorted = keyframes.OrderBy(item => item.Key).ToList();

            var keys = new Single[sorted.Count];
            var vals = new TValue[sorted.Count];

            for (int i = 0; i < keys.Length; ++i)
            {
                keys[i] = sorted[i].Key;
                vals[i] = sorted[i].Value;
            }

            return (keys, vals);
        }

        private static (Single[], TValue[]) _Split<TValue>(IReadOnlyDictionary<Single, (TValue,TValue,TValue)> keyframes)
        {
            var sorted = keyframes.OrderBy(item => item.Key).ToList();

            var keys = new Single[sorted.Count];
            var vals = new TValue[sorted.Count * 3];

            for (int i = 0; i < keys.Length; ++i)
            {
                keys[i] = sorted[i].Key;
                vals[(i * 3) + 0] = sorted[i].Value.Item1;
                vals[(i * 3) + 1] = sorted[i].Value.Item2;
                vals[(i * 3) + 2] = sorted[i].Value.Item3;
            }

            return (keys, vals);
        }

        public AnimationSampler WithVector3Keys(IReadOnlyDictionary<Single, Vector3> keyframes)
        {
            var kv = _Split(keyframes);
            _input = this._CreateInputAccessor(kv.Item1).LogicalIndex;
            _output = this._CreateOutputAccessor(kv.Item2).LogicalIndex;

            return this;
        }

        public AnimationSampler WithVector3Keys(IReadOnlyDictionary<Single, (Vector3, Vector3, Vector3)> keyframes)
        {
            var kv = _Split(keyframes);
            _input = this._CreateInputAccessor(kv.Item1).LogicalIndex;
            _output = this._CreateOutputAccessor(kv.Item2).LogicalIndex;

            return this;
        }

        public AnimationSampler WithQuaternionKeys(IReadOnlyDictionary<Single, Quaternion> keyframes)
        {
            var kv = _Split(keyframes);
            _input = this._CreateInputAccessor(kv.Item1).LogicalIndex;
            _output = this._CreateOutputAccessor(kv.Item2).LogicalIndex;

            return this;
        }

        public AnimationSampler WithQuaternionKeys(IReadOnlyDictionary<Single, (Quaternion, Quaternion, Quaternion)> keyframes)
        {
            var kv = _Split(keyframes);
            _input = this._CreateInputAccessor(kv.Item1).LogicalIndex;
            _output = this._CreateOutputAccessor(kv.Item2).LogicalIndex;

            return this;
        }

        public IReadOnlyDictionary<Single, Vector3> AsVector3KeyFrames()
        {
            if (this.InterpolationMode == AnimationInterpolationMode.CUBICSPLINE) throw new ArgumentException();

            var dict = new Dictionary<Single, Vector3>();

            var keys = this.Input.AsScalarArray();
            var frames = this.Output.AsVector3Array();

            return keys
                .Zip(frames, (key, val) => (key, val))
                .ToDictionary(item => item.key, item => item.val);
        }

        public IReadOnlyDictionary<Single, Quaternion> AsQuaternionKeyFrames()
        {
            var dict = new Dictionary<Single, Quaternion>();

            var keys = this.Input.AsScalarArray();
            var frames = this.Output.AsQuaternionArray();

            return keys
                .Zip(frames, (key, val) => (key, val))
                .ToDictionary(item => item.key, item => item.val);
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