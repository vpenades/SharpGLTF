using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SharpGLTF.Animations
{
    /// <summary>
    /// Wraps a collection of samplers split over time to speed up key retrieval.
    /// </summary>
    /// <typeparam name="T">The value sampled at any offset</typeparam>
    readonly struct FastSampler<T> : ICurveSampler<T>
    {
        public FastSampler(IEnumerable<ICurveSampler<T>> samplers)
        {
            _Samplers = samplers.ToArray();
        }

        private readonly ICurveSampler<T>[] _Samplers;

        public T GetPoint(float offset)
        {
            if (offset < 0) offset = 0;

            var index = (int)offset;

            if (index >= _Samplers.Length) index = _Samplers.Length - 1;

            return _Samplers[index].GetPoint(offset);
        }
    }
}
