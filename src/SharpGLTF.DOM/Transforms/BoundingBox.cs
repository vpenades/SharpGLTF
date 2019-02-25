using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;

namespace SharpGLTF
{
    public struct BoundingBox3
    {
        #region constructors

        public static BoundingBox3? UnionOf(IEnumerable<BoundingBox3?> bounds)
        {
            var final = Empty;

            foreach (var b in bounds.Where(item => item.HasValue))
            {
                final = final.IsEmpty ? b.Value : new BoundingBox3(final, b.Value);
            }

            return final.IsEmpty ? (BoundingBox3?)null : final;
        }

        public static BoundingBox3? FromPoints(IEnumerable<Vector3> points)
        {
            var final = Empty;

            foreach (var p in points)
            {
                final = final.IsEmpty ? new BoundingBox3(p) : new BoundingBox3(final, new BoundingBox3(p));
            }

            return final.IsEmpty ? (BoundingBox3?)null : final;
        }

        internal static BoundingBox3? Create(Schema2.IVisualNodeContainer container)
        {
            var nodes = Schema2.Node
                .Flatten(container)
                .Where(item => item.Mesh != null);

            var final = Empty;

            var points = new List<Vector3>();

            foreach (var n in nodes)
            {
                var bounds = n.Mesh.LocalBounds3;

                if (!bounds.HasValue) continue;

                var xform = n.WorldMatrix;

                var xppp = n.Mesh.LocalBounds3.Value.Points.Select(p => Vector3.Transform(p, xform));

                points.AddRange(xppp);
            }

            return BoundingBox3.FromPoints(points);
        }

        internal BoundingBox3(IReadOnlyList<Double> a, IReadOnlyList<Double> b)
        {
            var va = new Vector3((float)a[0], (float)a[1], (float)a[2]);
            var vb = new Vector3((float)b[0], (float)b[1], (float)b[2]);

            Min = Vector3.Min(va, vb);
            Max = Vector3.Max(va, vb);
        }

        public BoundingBox3(BoundingBox3 a, BoundingBox3 b)
        {
            Min = Vector3.Min(a.Min, b.Min);
            Max = Vector3.Max(a.Max, b.Max);
        }

        public BoundingBox3(Vector3 a)
        {
            Min = a;
            Max = a;
        }

        public BoundingBox3(Vector3 a, Vector3 b)
        {
            Min = Vector3.Min(a, b);
            Max = Vector3.Max(a, b);
        }

        #endregion

        #region data

        public Vector3 Min;
        public Vector3 Max;

        #endregion

        #region properties

        public static readonly BoundingBox3 Empty = new BoundingBox3()
        {
            Min = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue),
            Max = new Vector3(float.MinValue, float.MinValue, float.MinValue)
        };

        public Vector3 Center => (Min + Max) * 0.5f;

        public Vector3 Size => IsEmpty ? Vector3.Zero : Max - Min;

        public bool IsEmpty => (Min.X > Max.X) | (Min.Y > Max.Y) | (Min.Z > Max.Z);

        public IEnumerable<Vector3> Points
        {
            get
            {
                yield return new Vector3(Min.X, Min.Y, Min.Z);
                yield return new Vector3(Max.X, Min.Y, Min.Z);
                yield return new Vector3(Min.X, Max.Y, Min.Z);
                yield return new Vector3(Max.X, Max.Y, Min.Z);

                yield return new Vector3(Min.X, Min.Y, Max.Z);
                yield return new Vector3(Max.X, Min.Y, Max.Z);
                yield return new Vector3(Min.X, Max.Y, Max.Z);
                yield return new Vector3(Max.X, Max.Y, Max.Z);
            }
        }

        #endregion

        #region API

        public override string ToString() { return $"{Min} {Max}"; }

        #endregion
    }
}
