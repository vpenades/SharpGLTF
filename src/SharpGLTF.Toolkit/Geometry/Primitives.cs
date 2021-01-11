using System;
using System.Collections.Generic;
using System.Text;

namespace SharpGLTF.Geometry
{
    public readonly partial struct PointPrimitive<TVertex, Tmaterial>
    {
        public readonly TVertex A;
        public readonly Tmaterial Material;
    }

    public readonly partial struct LinePrimitive<TVertex, Tmaterial>
    {
        public readonly TVertex A;
        public readonly TVertex B;
        public readonly Tmaterial Material;
    }

    public readonly partial struct TrianglePrimitive<TVertex, Tmaterial>
    {
        public readonly TVertex A;
        public readonly TVertex B;
        public readonly TVertex C;
        public readonly Tmaterial Material;
    }
}
