using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.Numerics;

using SharpGLTF.Collections;
using SharpGLTF.Transforms;
using SharpGLTF.Validation;
using System.Xml.Linq;
using System.IO;
using System.Reflection;

namespace SharpGLTF.Schema2
{
    [System.Diagnostics.DebuggerDisplay("AnimChannel {TargetPointerPath}")]
    public sealed partial class AnimationChannel : IChildOfList<Animation>
    {
        #region lifecycle
        internal AnimationChannel() { }

        /// <summary>
        /// Sets the target property of this animation channel
        /// </summary>
        /// <param name="pointerPath">The path, as defined by AnimationChannel, as in '/nodes/0/rotation'</param>
        internal AnimationChannel(string pointerPath)
        {
            _SetChannelTarget(new AnimationChannelTarget(pointerPath));            
            _sampler = -1;
        }

        internal AnimationChannel(Node targetNode, PropertyPath targetPath)
        {
            _SetChannelTarget(new AnimationChannelTarget(targetNode, targetPath));            
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
        /// Gets the path to the property being animated by this channel.
        /// </summary>
        /// <remarks>
        /// <para>
        /// The format is defined by <see href="https://github.com/KhronosGroup/glTF/tree/main/extensions/2.0/Khronos/KHR_animation_pointer">KHR_animation_pointer</see>
        /// </para>
        /// <para>
        /// examples:<br/>
        /// "/nodes/0/rotation"<br/>
        /// "/materials/0/pbrMetallicRoughness/baseColorFactor"<br/>
        /// </para>
        /// </remarks>
        public string TargetPointerPath => this._target?.GetPointerPath() ?? null;

        /// <summary>
        /// Gets the <see cref="Node"/> which property is to be bound with this animation.
        /// </summary>
        [Obsolete("Use TargetPointerPath whenever possible")]
        public Node TargetNode
        {
            get
            {
                var idx = this._target?.GetNodeIndex() ?? -1;
                if (idx < 0) return null;
                return this.LogicalParent.LogicalParent.LogicalNodes[idx];
            }
        }

        /// <summary>
        /// Gets which property of the <see cref="Node"/> pointed by <see cref="TargetNode"/> is to be bound with this animation.
        /// </summary>
        /// <remarks>
        /// If the target is anything other than a <see cref="Node"/> transform property, the returned value will be <see cref="PropertyPath.pointer"/>
        /// </remarks>
        public PropertyPath TargetNodePath => this._target?.GetNodePath() ?? PropertyPath.translation;

        #endregion

        #region API

        private void _SetChannelTarget(AnimationChannelTarget target)
        {
            new Collections.ChildSetter<AnimationChannel>(this).SetProperty(ref _target, target);
        }

        public IAnimationSampler<T> GetSamplerOrNull<T>()
        {
            return _GetSampler() as IAnimationSampler<T>;
        }

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
