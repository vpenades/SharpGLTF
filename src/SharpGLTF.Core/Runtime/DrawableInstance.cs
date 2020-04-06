using System;
using System.Collections.Generic;
using System.Text;

namespace SharpGLTF.Runtime
{
    [System.Diagnostics.DebuggerDisplay("{Template.Name} {MeshIndex}")]
    public readonly struct DrawableInstance
    {
        internal DrawableInstance(IDrawableTemplate t, Transforms.IGeometryTransform xform)
        {
            Template = t;
            Transform = xform;
        }

        public readonly IDrawableTemplate Template;

        public readonly Transforms.IGeometryTransform Transform;
    }
}
