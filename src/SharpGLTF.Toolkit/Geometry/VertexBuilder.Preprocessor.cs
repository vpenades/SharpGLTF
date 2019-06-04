using System;
using System.Collections.Generic;
using System.Text;

namespace SharpGLTF.Geometry
{
    public partial struct VertexBuilder<TvG, TvM, TvS>
    {
        public class Preprocessor
        {
            private readonly List<Func<TvG, TvG?>> _GeometryPreprocessor = new List<Func<TvG, TvG?>>();
            private readonly List<Func<TvM, TvM?>> _MaterialPreprocessor = new List<Func<TvM, TvM?>>();
            private readonly List<Func<TvS, TvS?>> _SkinningPreprocessor = new List<Func<TvS, TvS?>>();

            public void Append(Func<TvG, TvG?> func)
            {
                _GeometryPreprocessor.Add(func);
            }

            public void Append(Func<TvM, TvM?> func)
            {
                _MaterialPreprocessor.Add(func);
            }

            public void Append(Func<TvS, TvS?> func)
            {
                _SkinningPreprocessor.Add(func);
            }

            public bool PreprocessVertex(ref VertexBuilder<TvG, TvM, TvS> vertex)
            {
                foreach (var f in _GeometryPreprocessor)
                {
                    var g = f(vertex.Geometry);
                    if (!g.HasValue) return false;
                    vertex.Geometry = g.Value;
                }

                foreach (var f in _MaterialPreprocessor)
                {
                    var m = f(vertex.Material);
                    if (!m.HasValue) return false;
                    vertex.Material = m.Value;
                }

                foreach (var f in _SkinningPreprocessor)
                {
                    var s = f(vertex.Skinning);
                    if (!s.HasValue) return false;
                    vertex.Skinning = s.Value;
                }

                return true;
            }
        }

        /// <summary>
        /// Gets a preprocessor that does a best effort to produce valid vertices
        /// </summary>
        public static Preprocessor SanitizerPreprocessor
        {
            get
            {
                var p = new Preprocessor();
                p.Append(VertexTypes.VertexPreprocessors.SanitizeVertexGeometry);
                p.Append(VertexTypes.VertexPreprocessors.SanitizeVertexMaterial);
                p.Append(VertexTypes.VertexPreprocessors.SanitizeVertexGeometry);

                return p;
            }
        }

        /// <summary>
        /// Gets a preprocessor that strictly validates a vertex
        /// </summary>
        public static Preprocessor ValidationPreprocessor
        {
            get
            {
                var p = new Preprocessor();
                p.Append(VertexTypes.VertexPreprocessors.ValidateVertexGeometry);
                p.Append(VertexTypes.VertexPreprocessors.ValidateVertexMaterial);
                p.Append(VertexTypes.VertexPreprocessors.ValidateVertexSkinning);

                return p;
            }
        }

    }
}
