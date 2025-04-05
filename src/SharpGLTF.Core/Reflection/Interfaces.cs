using System;
using System.Collections.Generic;
using System.Text;

using FIELDINFO = SharpGLTF.Reflection.FieldInfo;

namespace SharpGLTF.Reflection
{
    public interface IReflectionObject
    {
        IEnumerable<FIELDINFO> GetFields();
        bool TryGetField(string name, out FIELDINFO value);
    }

    public interface IReflectionArray : IReflectionObject
    {
        int Count { get; }
        FIELDINFO GetField(int index);
    }
}
