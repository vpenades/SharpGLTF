using System;
using System.Collections.Generic;
using System.Text;

namespace SharpGLTF.Collections
{
    interface IChildOf<TParent>
        where TParent : class
    {
        int LogicalIndex { get; }

        TParent LogicalParent { get; }

        void _SetLogicalParent(TParent parent, int index);
    }
}
