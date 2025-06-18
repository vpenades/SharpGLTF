using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace SharpGLTF.Schema2
{
    /// <summary>
    /// Extensions used internally.
    /// </summary>
    static partial class _Schema2Extensions
    {
        #region morph weights

        public static void SetMorphWeights(this IList<Double> list, int maxCount, Transforms.SparseWeight8 weights)
        {
            while (list.Count > maxCount) list.RemoveAt(list.Count - 1);
            while (list.Count < maxCount) list.Add(0);

            if (list.Count > 0)
            {
                for (int i = 0; i < list.Count; ++i)
                {
                    list[i] = 0;
                }

                foreach (var (index, weight) in weights.GetNonZeroWeights())
                {                    
                    list[index] = weight;
                }
            }
        }

        public static void SetMorphWeights(this IList<Double> list, IReadOnlyList<float> weights)
        {
            if (weights == null) { list.Clear(); return; }

            while (list.Count > weights.Count) list.RemoveAt(list.Count - 1);
            while (list.Count < weights.Count) list.Add(0);

            if (list.Count > 0)
            {
                for (int i = 0; i < list.Count; ++i)
                {
                    list[i] = weights[i];
                }
            }
        }

        #endregion
    }
}
