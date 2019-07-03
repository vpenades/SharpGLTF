using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace SharpGLTF.Animations
{
    public class Animatable<T>
        where T : struct
    {
        #region data

        private Dictionary<string, ICurveSampler<T>> _Tracks = new Dictionary<string, ICurveSampler<T>>();

        public T Default { get; set; }

        #endregion

        #region API

        public T GetValueAt(string track, float value)
        {
            return _Tracks.TryGetValue(track, out ICurveSampler<T> sampler) ? sampler.GetSample(value) : this.Default;
        }

        public ILinearCurve<T> UseLinearCurve(string track)
        {
            if (!_Tracks.TryGetValue(track, out ICurveSampler<T> curve))
            {
                _Tracks[track] = CurveFactory.CreateLinearCurve<T>();
            }

            if (curve is ILinearCurve<T> editableCurve) return editableCurve;

            throw new ArgumentException(nameof(T), "Generic argument not supported");
        }

        public ISplineCurve<T> UseSplineCurve(string track)
        {
            if (!_Tracks.TryGetValue(track, out ICurveSampler<T> curve))
            {
                _Tracks[track] = CurveFactory.CreateSplineCurve<T>();
            }

            if (curve is ISplineCurve<T> editableCurve) return editableCurve;

            throw new ArgumentException(nameof(T), "Generic argument not supported");
        }

        #endregion
    }
}
