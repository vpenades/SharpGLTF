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
        #region lifecycle

        /// <summary>
        /// Creates a new, read only <see cref="ICurveSampler{T}"/> that has been optimized for fast sampling
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
            if (sequence == null) throw new ArgumentNullException(nameof(sequence));
            if (chunkFactory == null) throw new ArgumentNullException(nameof(chunkFactory));

            #pragma warning disable CA1851            
            if (!sequence.Skip(3).Any()) // not enough keys, or not worth optimizing it, use a standard sampler
            {
                return chunkFactory.Invoke(sequence.ToArray());
            }
            #pragma warning restore CA1851

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

        #endregion

        #region data

        [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.RootHidden)]
        private readonly ICurveSampler<T>[] _Samplers;

        #endregion

        #region API

        public T GetPoint(float offset)
        {
            if (offset < 0) offset = 0;

            var index = (int)offset;

            if (index >= _Samplers.Length) index = _Samplers.Length - 1;

            return _Samplers[index].GetPoint(offset);
        }

        #endregion
    }
}
