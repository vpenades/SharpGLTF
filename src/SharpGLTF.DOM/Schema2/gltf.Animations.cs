using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;

namespace SharpGLTF.Schema2
{
    using Collections;
    using System.Numerics;

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

        /// <summary>
        /// Gets the list of <see cref="AnimationChannel"/> instances.
        /// </summary>
        public IReadOnlyList<AnimationChannel> Channels => _channels;

        #endregion

        #region API

        public Accessor CreateInputAccessor(IReadOnlyList<Single> input)
        {
            var buffer = LogicalParent.UseBufferView(new Byte[input.Count * 4]);
            var accessor = LogicalParent.CreateAccessor("Animation.Input")
                .WithData(buffer, 0, input.Count, ElementType.SCALAR, ComponentType.FLOAT, false);

            Memory.EncodedArrayUtils.FillFrom(accessor.AsScalarArray(), 0, input);

            accessor.UpdateBounds();

            return accessor;
        }

        public Accessor CreateOutputAccessor(IReadOnlyList<Vector3> output)
        {
            var buffer = LogicalParent.UseBufferView(new Byte[output.Count * 4 * 3]);
            var accessor = LogicalParent.CreateAccessor("Animation.Output")
                .WithData(buffer, 0, output.Count, ElementType.VEC3, ComponentType.FLOAT, false);

            Memory.EncodedArrayUtils.FillFrom(accessor.AsVector3Array(), 0, output);

            accessor.UpdateBounds();

            return accessor;
        }

        /// <summary>
        /// Creates a new <see cref="AnimationSampler"/> instance and adds it to this <see cref="Animation"/>.
        /// </summary>
        /// <param name="input">An <see cref="Accessor"/> containing input (TIME) values.</param>
        /// <param name="output">An <see cref="Accessor"/> containing output (TRS) values.</param>
        /// <param name="interpolation">how the output values are interpolated.</param>
        /// <returns>An <see cref="AnimationSampler"/> instance.</returns>
        public AnimationSampler CreateSampler(Accessor input, Accessor output, AnimationInterpolationMode interpolation)
        {
            Guard.MustShareLogicalParent(this, input, nameof(input));
            Guard.MustShareLogicalParent(this, output, nameof(output));

            var sampler = new AnimationSampler(input, output, interpolation);

            _samplers.Add(sampler);

            return sampler;
        }

        public AnimationChannel CreateChannel(Node node, PathType path, AnimationSampler sampler)
        {
            Guard.MustShareLogicalParent(this, node, nameof(node));
            Guard.NotNull(sampler, nameof(sampler));
            Guard.IsTrue(Object.ReferenceEquals(this, sampler.LogicalParent), nameof(sampler));

            var channel = new AnimationChannel(node, path, sampler);

            _channels.Add(channel);

            return channel;
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

    public sealed partial class AnimationChannel : IChildOf<Animation>
    {
        #region lifecycle

        internal AnimationChannel() { }

        internal AnimationChannel(Node targetNode, PathType targetPath, AnimationSampler sampler)
        {
            _target = new AnimationChannelTarget(targetNode, targetPath);
            _sampler = sampler.LogicalIndex;
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

    public sealed partial class AnimationSampler : IChildOf<Animation>
    {
        #region lifecycle

        internal AnimationSampler() { }

        internal AnimationSampler(Accessor input, Accessor output, AnimationInterpolationMode interpolation)
        {
            _interpolation = interpolation.AsNullable(_interpolationDefault);
            _input = input.LogicalIndex;
            _output = output.LogicalIndex;
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

        public AnimationInterpolationMode Mode
        {
            get => _interpolation.AsValue(_interpolationDefault);
            set => _interpolation = value.AsNullable(_interpolationDefault);
        }

        public Accessor Input => this.LogicalParent.LogicalParent.LogicalAccessors[this._input];

        public Accessor Output => this.LogicalParent.LogicalParent.LogicalAccessors[this._output];

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