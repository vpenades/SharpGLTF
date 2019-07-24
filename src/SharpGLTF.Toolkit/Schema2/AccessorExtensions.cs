using System;
using System.Collections.Generic;
using System.Text;
using System.Numerics;

namespace SharpGLTF.Schema2
{
    public static partial class Schema2Toolkit
    {
        public static Accessor CreateVertexAccessor(this ModelRoot root, Memory.MemoryAccessor memAccessor)
        {
            Guard.NotNull(root, nameof(root));
            Guard.NotNull(memAccessor, nameof(memAccessor));

            var accessor = root.CreateAccessor(memAccessor.Attribute.Name);

            accessor.SetVertexData(memAccessor);

            return accessor;
        }
    }
}
