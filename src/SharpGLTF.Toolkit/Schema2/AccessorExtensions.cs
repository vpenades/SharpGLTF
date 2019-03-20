using System;
using System.Collections.Generic;
using System.Text;
using System.Numerics;

namespace SharpGLTF.Schema2
{
    public static partial class Toolkit
    {
        public static Accessor CreateVertexAccessor(this ModelRoot root, Geometry.MemoryAccessor memAccessor)
        {
            var accessor = root.CreateAccessor(memAccessor.Attribute.Name);

            accessor.SetVertexData(memAccessor);

            return accessor;
        }
    }
}
