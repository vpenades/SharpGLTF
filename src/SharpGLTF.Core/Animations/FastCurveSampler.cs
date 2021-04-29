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
    readonly struct FastCurveSampler<T> : ICurveSampler<T>
    {
        /// <summary>
        /// Creates a new, read only <see cref="ICurveSampler{T}"/> that has been optimized for fast sampling.
        /// </summary>
        /// <remarks>
        /// Sampling a raw curve with a large number of keys can be underperformant. This code splits the keys into 1 second
        /// chunks that can be accessed at much faster speed.
        /// </remarks>
        /// <typeparam name="TKey">The value of a key (may include tangents)</typeparam>
        /// <param name="sequence">A sequence of Time-Key entries, ordered by Time.</param>
        /// <param name="chunkFactory">A curve chunk factory function.</param>
        /// <returns>The new, optimized curve sampler.</returns>
        public static ICurveSampler<T> CreateFrom<TKey>(IEnumerable<(float, TKey)> sequence, Func<(float, TKey)[], ICurveSampler<T>> chunkFactory)
        {
            // not enough keys, or not worth optimizing it.
            if (!sequence.Skip(3).Any()) return null;

            var split = sequence
                .SplitByTime()
                .Select(item => chunkFactory.Invoke(item))
                .Cast<ICurveSampler<T>>();

            return new FastCurveSampler<T>(split);
        }

        private FastCurveSampler(IEnumerable<ICurveSampler<T>> samplers)
        {
            _Samplers = samplers.ToArray();
        }

        [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.RootHidden)]
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
