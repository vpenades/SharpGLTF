using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

using SharpGLTF.IO;
using SharpGLTF.Schema2;

namespace SharpGLTF.Reflection
{
    /// <summary>
    /// Extends <see cref="JsonSerializable"/> with reflection APIs
    /// </summary>
    /// <remarks>
    /// Typically, most objects found in glTF schemas are either enums or classes that inherit
    /// from <see cref="ExtraProperties"/> class, but that's not always the case. In these cases
    /// the base class should be <see cref="JsonReflectable"/> to support all features.
    /// </remarks>
    public abstract class JsonReflectable : JsonSerializable, IReflectionObject
    {
        #region reflection

        public const string SCHEMANAME = "Object";

        protected override string GetSchemaName() => SCHEMANAME;

        protected virtual IEnumerable<string> ReflectFieldsNames()
        {
            return Enumerable.Empty<string>();
        }

        protected virtual bool TryReflectField(string name, out FieldInfo value)
        {
            value = default;
            return false;
        }

        IEnumerable<FieldInfo> IReflectionObject.GetFields()
        {
            foreach (var name in ReflectFieldsNames())
            {
                if (TryReflectField(name, out var finfo)) yield return finfo;
            }
        }

        bool IReflectionObject.TryGetField(string name, out FieldInfo value)
        {
            return TryReflectField(name, out value);
        }

        #endregion

        #region serialization

        protected override void SerializeProperties(Utf8JsonWriter writer)
        {
            
        }        
        
        protected override void DeserializeProperty(string jsonPropertyName, ref Utf8JsonReader reader)
        {
            reader.Skip();
        }

        #endregion
    }
}
