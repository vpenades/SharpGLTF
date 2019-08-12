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

        public bool IsAnimated => Tracks.Count > 0;

        public IReadOnlyCollection<string> Tracks => _Animations?.Names;

        #endregion

        #region API

        /// <summary>
        /// Evaluates the value of this <see cref="AnimatableProperty{T}"/> at a given <paramref name="offset"/> for a given <paramref name="trackName"/>.
        /// </summary>
        /// <param name="trackName">An animation track name, or null.</param>
        /// <param name="offset">A time offset within the given animation track.</param>
        /// <returns>The evaluated value taken from the animation <paramref name="trackName"/>, or <see cref="Value"/> if a track was not found.</returns>
        public T GetValueAt(string trackName, float offset)
        {
            var idx = _Animations?.IndexOf(trackName) ?? -1;

            return GetValueAt(idx, offset);
        }

        public T GetValueAt(int trackLogicalIndex, float offset)
        {
            if (_Animations == null) return this.Value;

            if (trackLogicalIndex < 0 || trackLogicalIndex >= _Animations.Count) return this.Value;

            return _Animations[trackLogicalIndex].GetPoint(offset);
        }

        public void AddCurve(int logicalIndex, string name, ICurveSampler<T> sampler)
        {
            Guard.NotNull(sampler, nameof(sampler));
            Guard.MustBeGreaterThanOrEqualTo(logicalIndex, 0, nameof(logicalIndex));

            if (_Animations == null) _Animations = new Collections.NamedList<ICurveSampler<T>>();

            _Animations.SetValue(logicalIndex, name, sampler);
        }

        #endregion
    }
}
