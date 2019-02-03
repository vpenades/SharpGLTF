using System;
using System.Collections.Generic;
using System.Text;

namespace glTF2Sharp.Collections
{
    interface IChildOf<TParent>
        where TParent : class
    {
        TParent LogicalParent { get; }

        void _SetLogicalParent(TParent parent);
    }
}
