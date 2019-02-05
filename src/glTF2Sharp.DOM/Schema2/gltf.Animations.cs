using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;

namespace glTF2Sharp.Schema2
{
    using Collections;    

    [System.Diagnostics.DebuggerDisplay("Animation[{LogicalIndex}] {Name}")]
    public partial class Animation
    {
        #region lifecycle

        public Animation()
        {
            _channels = new ChildrenCollection<AnimationChannel, Animation>(this);
            _samplers = new ChildrenCollection<AnimationSampler, Animation>(this);
        }

        #endregion

        #region properties

        public int LogicalIndex => this.LogicalParent.Animations.IndexOfReference(this);

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

    public partial class AnimationChannelTarget
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

    public partial class AnimationChannel : IChildOf<Animation>
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
                return this.LogicalParent.LogicalParent._LogicalNodes[idx];
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

    public partial class AnimationSampler : IChildOf<Animation>
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

        public Animation LogicalParent { get; private set; }

        public int LogicalIndex => LogicalParent._Samplers.IndexOfReference(this);

        void IChildOf<Animation>._SetLogicalParent(Animation parent) { LogicalParent = parent; }

        public AnimationInterpolationMode Mode => _interpolation ?? _interpolationDefault;

        public Accessor Input => this.LogicalParent.LogicalParent._LogicalAccessors[this._input];

        public Accessor Output => this.LogicalParent.LogicalParent._LogicalAccessors[this._output];

        #endregion
    }

    public partial class ModelRoot
    {
        public Animation CreateAnimation()
        {
            var anim = new Animation();

            _animations.Add(anim);

            return anim;
        }        
    }
}



