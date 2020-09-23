using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using SharpGLTF.Animations;

namespace SharpGLTF.Runtime
{
    /// <summary>
    /// Defines an animatable property with a default value and a collection of animation curve tracks.
    /// </summary>
    /// <typeparam name="T">A type that can be interpolated with <see cref="ICurveSampler{T}"/></typeparam>
    sealed class AnimatableProperty<T>
       where T : struct
    {
        #region lifecycle

        internal AnimatableProperty(T defval)
        {
            Value = defval;
        }

        #endregion

        #region data

        private Collections.NamedList<ICurveSampler<T>> _Animations;

        /// <summary>
        /// Gets the default value of this instance.
        /// When animations are disabled, or there's no animation track available, this will be the returned value.
        /// </summary>
        public T Value { get; private set; }

        #endregion

        #region properties

        public bool IsAnimated => _Animations == null ? false : _Animations.Count > 0;

        public IReadOnlyCollection<string> Tracks => _Animations?.Names;

        #endregion

        #region API

        /// <summary>
        /// Evaluates the value of this <see cref="AnimatableProperty{T}"/> at a given <paramref name="offset"/> for a given <paramref name="trackLogicalIndex"/>.
        /// </summary>
        /// <param name="trackLogicalIndex">The index of the animation track</param>
        /// <param name="offset">The time offset within the curve</param>
        /// <returns>The evaluated value taken from the animation <paramref name="trackLogicalIndex"/>, or <see cref="Value"/> if a track was not found.</returns>
        public T GetValueAt(int trackLogicalIndex, float offset)
        {
            if (_Animations == null) return this.Value;

            if (trackLogicalIndex < 0 || trackLogicalIndex >= _Animations.Count) return this.Value;

            return _Animations[trackLogicalIndex]?.GetPoint(offset) ?? this.Value;
        }

        public void AddCurve(int logicalIndex, string name, ICurveSampler<T> sampler)
        {
            Guard.NotNull(sampler, nameof(sampler));
            Guard.MustBeGreaterThanOrEqualTo(logicalIndex, 0, nameof(logicalIndex));

            if (_Animations == null) _Animations = new Collections.NamedList<ICurveSampler<T>>();

            _Animations.SetName(logicalIndex, name, sampler);
        }

        #endregion
    }
}
