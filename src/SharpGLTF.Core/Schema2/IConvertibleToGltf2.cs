using System;
using System.Collections.Generic;
using System.Text;

namespace SharpGLTF.Schema2
{
    /// <summary>
    /// Defines a method that converts the implementing reference to a <see cref="ModelRoot"/>
    /// </summary>
    public interface IConvertibleToGltf2
    {
        ModelRoot ToGltf2();
    }
}
