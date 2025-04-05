using System;
using System.Collections.Generic;
using System.Text;

using FIELDINFO = SharpGLTF.Reflection.FieldInfo;

namespace SharpGLTF.Reflection
{

    /// <summary>
    /// Signals that an object contains a named collection of <see cref="FIELDINFO"/>
    /// </summary>
    /// <remarks>
    /// if an object implementing this interface also implements <see cref="IReflectionArray"/>,
    /// then <see cref="TryGetField(string, out FIELDINFO)"/> should work using the index
    /// converted to a string as in <code>TryGetField("17", out var field); field == IReflectionArray.GetField(17);</code>
    /// </remarks>
    public interface IReflectionObject
    {
        IEnumerable<FIELDINFO> GetFields();
        bool TryGetField(string name, out FIELDINFO value);
    }

    /// <summary>
    /// Signals that an object contains a variable collection of <see cref="FIELDINFO"/>
    /// </summary>    
    public interface IReflectionArray : IReflectionObject
    {
        int Count { get; }
        FIELDINFO GetField(int index);
    }
}
