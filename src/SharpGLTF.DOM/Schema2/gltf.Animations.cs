using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;

namespace SharpGLTF.Schema2
{
    using Collections;

    [System.Diagnostics.DebuggerDisplay("Animation[{LogicalIndex}] {Name}")]
    public sealed partial class Animation
    {
        #region lifecycle

        public Animation()
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

        public IReadOnlyList<AnimationChannel> Channels => _channels;

        #endregion

        #region API

        public AnimationSampler CreateSampler(Accessor input, Accessor output, AnimationInterpolationMode interpolation)
        {
            var sampler = new AnimationSampler(input, output, interpolation);

            _samplers.Add(sampler);

            return sampler;
        }

        /*
        public AnimationSampler CreateSampler(IReadOnlyList<Single> input, IReadOnlyList<Single> output, AnimationInterpolationMode interpolation)
        {
            var inputData = input.ToArray().ToByteArray();
            var outputData = output.ToArray().ToByteArray();

            var inputAccessor = LogicalParent._CreateDataAccessor(inputData, Runtime.Encoding.DimensionType.Scalar, input.Count);
            var outputAccesor = LogicalParent._CreateDataAccessor(outputData, Runtime.Encoding.DimensionType.Scalar, output.Count);

            return CreateSampler(inputAccessor, outputAccesor, interpolation);
        }

        public AnimationSampler CreateSampler(IReadOnlyList<Single> input, IReadOnlyList<System.Numerics.Vector3> output, AnimationInterpolationMode interpolation)
        {
            var inputData = input.ToArray().ToByteArray();
            var outputData = output.ToArray().ToByteArray();

            var inputAccessor = LogicalParent._CreateDataAccessor(inputData, Runtime.Encoding.DimensionType.Scalar, input.Count);
            var outputAccesor = LogicalParent._CreateDataAccessor(outputData, Runtime.Encoding.DimensionType.Vector3, output.Count);

            return CreateSampler(inputAccessor, outputAccesor, interpolation);
        }

        public AnimationSampler CreateSampler(IReadOnlyList<Single> input, IReadOnlyList<System.Numerics.Quaternion> output, AnimationInterpolationMode interpolation)
        {
            var inputData = input.ToArray().ToByteArray();
            var outputData = output.ToArray().ToByteArray();

            var inputAccessor = LogicalParent._CreateDataAccessor(inputData, Runtime.Encoding.DimensionType.Scalar, input.Count);
            var outputAccesor = LogicalParent._CreateDataAccessor(outputData, Runtime.Encoding.DimensionType.Vector4, output.Count);

            return CreateSampler(inputAccessor, outputAccesor, interpolation);
        }*/

        public void AddChannel(Node node, PathType path, AnimationSampler sampler)
        {
            var channel = new AnimationChannel(node, path, sampler);

            _channels.Add(channel);
        }

        #endregion
    }

    public sealed partial class AnimationChannelTarget
    {
        #region lifecycle

        public AnimationChannelTarget() { }

        public AnimationChannelTarget(Node targetNode, PathType targetPath)
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

        public AnimationChannel() { }

        public AnimationChannel(Node targetNode, PathType targetPath, AnimationSampler sampler)
        {
            _target = new AnimationChannelTarget(targetNode, targetPath);
            _sampler = sampler.LogicalIndex;
        }

        #endregion

        #region properties

        public Animation LogicalParent { get; private set; }

        void IChildOf<Animation>._SetLogicalParent(Animation parent) { LogicalParent = parent; }

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

        public int OutputStride
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

        public AnimationSampler() { }

        public AnimationSampler(Accessor input, Accessor output, AnimationInterpolationMode interpolation)
        {
            _interpolation = interpolation;
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