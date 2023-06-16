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

        private List<ICurveSampler<T>> _Curves;

        /// <summary>
        /// Gets the default value of this instance.
        /// When animations are disabled, or there's no animation track available, this will be the returned value.
        /// </summary>
        public T Value { get; private set; }

        #endregion

        #region properties

        public bool IsAnimated => _Curves == null ? false : _Curves.Count > 0;

        #endregion

        #region API

        /// <summary>
        /// Evaluates the value of this <see cref="AnimatableProperty{T}"/> at a given <paramref name="offset"/> for a given <paramref name="trackLogicalIndex"/>.
        /// </summary>
        /// <param name="trackLogicalIndex">The index of the animation track.</param>
        /// <param name="offset">The time offset within the curve</param>
        /// <returns>The evaluated value taken from the animation <paramref name="trackLogicalIndex"/>, or <see cref="Value"/> if a track was not found.</returns>
        public T GetValueAt(int trackLogicalIndex, float offset)
        {
            if (_Curves == null) return this.Value;

            if (trackLogicalIndex < 0 || trackLogicalIndex >= _Curves.Count) return this.Value;

            return _Curves[trackLogicalIndex]?.GetPoint(offset) ?? this.Value;
        }

        /// <summary>
        /// Sets the animation curves for this property.
        /// </summary>
        /// <param name="trackLogicalIndex">The index of the animation track.</param>
        /// <param name="curveSampler">A curve sampler, or null if the curve is to be removed.</param>
        public void SetCurve(int trackLogicalIndex, ICurveSampler<T> curveSampler)
        {
            Guard.MustBeGreaterThanOrEqualTo(trackLogicalIndex, 0, nameof(trackLogicalIndex));

            if (curveSampler == null)
            {
                if (_Curves != null && trackLogicalIndex < _Curves.Count)
                {
                    _Curves[trackLogicalIndex] = null;
                    if (_Curves.All(item => item == null)) _Curves = null;
                }

                return;
            }

            if (_Curves == null) _Curves = new List<ICurveSampler<T>>();
            while (_Curves.Count <= trackLogicalIndex) _Curves.Add(null);

            _Curves[trackLogicalIndex] = curveSampler;
        }

        #endregion
    }
}
