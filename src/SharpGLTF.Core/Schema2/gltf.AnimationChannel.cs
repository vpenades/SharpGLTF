using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.Numerics;

using SharpGLTF.Collections;
using SharpGLTF.Transforms;
using SharpGLTF.Validation;

namespace SharpGLTF.Schema2
{
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
    public sealed partial class AnimationChannel : IChildOfList<Animation>
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

        void IChildOfList<Animation>.SetLogicalParent(Animation parent, int index)
        {
            LogicalParent = parent;
            LogicalIndex = index;
        }

        #endregion

        #region properties

        /// <summary>
        /// Gets the zero-based index of this <see cref="Animation"/> at <see cref="ModelRoot.LogicalAnimations"/>
        /// </summary>
        public int LogicalIndex { get; private set; } = -1;

        /// <summary>
        /// Gets the <see cref="Animation"/> instance that owns this object.
        /// </summary>
        [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
        public Animation LogicalParent { get; private set; }

        /// <summary>
        /// Gets the <see cref="Node"/> which property is to be bound with this animation.
        /// </summary>
        public Node TargetNode
        {
            get
            {
                var idx = this._target?._NodeId ?? -1;
                if (idx < 0) return null;
                return this.LogicalParent.LogicalParent.LogicalNodes[idx];
            }
        }

        /// <summary>
        /// Gets which property of the <see cref="Node"/> pointed by <see cref="TargetNode"/> is to be bound with this animation.
        /// </summary>
        public PropertyPath TargetNodePath => this._target?._NodePath ?? PropertyPath.translation;

        #endregion

        #region API

        internal AnimationSampler _GetSampler() { return this.LogicalParent._Samplers[this._sampler]; }

        public IAnimationSampler<Vector3> GetScaleSampler()
        {
            if (TargetNodePath != PropertyPath.scale) return null;
            return _GetSampler();
        }

        public IAnimationSampler<Quaternion> GetRotationSampler()
        {
            if (TargetNodePath != PropertyPath.rotation) return null;
            return _GetSampler();
        }

        public IAnimationSampler<Vector3> GetTranslationSampler()
        {
            if (TargetNodePath != PropertyPath.translation) return null;
            return _GetSampler();
        }

        public IAnimationSampler<SparseWeight8> GetSparseMorphSampler()
        {
            if (TargetNodePath != PropertyPath.weights) return null;
            return _GetSampler();
        }

        public IAnimationSampler<float[]> GetMorphSampler()
        {
            if (TargetNodePath != PropertyPath.weights) return null;
            return _GetSampler();
        }

        #endregion

        #region Validation

        protected override void OnValidateReferences(ValidationContext validate)
        {
            base.OnValidateReferences(validate);

            validate.IsNullOrIndex("Sampler", _sampler, this.LogicalParent._Samplers);
        }

        #endregion
    }
}
