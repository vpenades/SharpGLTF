using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Text;

namespace SharpGLTF.Diagnostics
{
    internal abstract class _CurveBuilderDebugProxy<T>
        where T : struct
    {
        #region lifecycle

        public _CurveBuilderDebugProxy(Animations.CurveBuilder<T> curve)
        {
            _Curve = curve;
            _CreateItems(curve);
        }

        private void _CreateItems(Animations.CurveBuilder<T> curve)
        {
            Animations._CurveNode<T>? prev = null;

            foreach (var kvp in curve._DebugKeys)
            {
                if (prev.HasValue)
                {
                    var d = prev.Value.Degree;

                    switch (d)
                    {
                        case 0:

                            break;

                        case 1:
                            _Items.Add(new _OutTangent { Tangent = GetTangent(prev.Value.Point, kvp.Value.Point) });
                            break;

                        case 3:
                            _Items.Add(new _OutTangent { Tangent = prev.Value.OutgoingTangent });
                            _Items.Add(new _InTangent { Tangent = kvp.Value.IncomingTangent });
                            break;

                        default:
                            _Items.Add("ERROR: {d}");
                            break;
                    }
                }

                _Items.Add(new _Point { Key = kvp.Key, Point = kvp.Value.Point });

                prev = kvp.Value;
            }
        }

        #endregion

        #region data

        [System.Diagnostics.DebuggerDisplay("{Key} => {Point}")]
        private struct _Point
        {
            public float Key;
            public T Point;
        }

        [System.Diagnostics.DebuggerDisplay("               🡖 {Tangent}")]
        private struct _OutTangent { public T Tangent; }

        [System.Diagnostics.DebuggerDisplay("               🡗 {Tangent}")]
        private struct _InTangent { public T Tangent; }

        private readonly Animations.CurveBuilder<T> _Curve;
        private readonly List<Object> _Items = new List<object>();

        #endregion

        [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.RootHidden)]
        public Object[] Items => _Items.ToArray();

        protected abstract T GetTangent(T a, T b);
    }

    sealed class _CurveBuilderDebugProxyVector3 : _CurveBuilderDebugProxy<Vector3>
    {
        public _CurveBuilderDebugProxyVector3(Animations.CurveBuilder<Vector3> curve)
            : base(curve) { }

        protected override Vector3 GetTangent(Vector3 a, Vector3 b)
        {
            return b - a;
        }
    }

    sealed class _CurveBuilderDebugProxyQuaternion : _CurveBuilderDebugProxy<Quaternion>
    {
        public _CurveBuilderDebugProxyQuaternion(Animations.CurveBuilder<Quaternion> curve)
            : base(curve) { }

        protected override Quaternion GetTangent(Quaternion a, Quaternion b)
        {
            return Animations.CurveSampler.CreateTangent(a, b);
        }
    }

    sealed class _CurveBuilderDebugProxySparse : _CurveBuilderDebugProxy<Transforms.SparseWeight8>
    {
        public _CurveBuilderDebugProxySparse(Animations.CurveBuilder<Transforms.SparseWeight8> curve)
            : base(curve) { }

        protected override Transforms.SparseWeight8 GetTangent(Transforms.SparseWeight8 a, Transforms.SparseWeight8 b)
        {
            return Transforms.SparseWeight8.Subtract(b, a);
        }
    }
}