using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace SharpGLTF.SchemaReflection
{
    public partial class SchemaType
    {
        /// <summary>
        /// Collection class that contains all the types found in a json schema.
        /// </summary>
        public sealed class Context
        {
            #region data

            private readonly Dictionary<string, SchemaType> _Types = new Dictionary<string, SchemaType>();            

            #endregion

            #region properties

            /// <summary>
            /// returns all the <see cref="enum"/> types.
            /// </summary>
            public IEnumerable<EnumType> Enumerations => _Types.Values.OfType<EnumType>();

            /// <summary>
            /// returns all the <see cref="class"/> types.
            /// </summary>
            public IEnumerable<ClassType> Classes => _Types.Values.OfType<ClassType>();

            #endregion

            #region API

            /// <summary>
            /// Creates a new type or uses an existing one if it already exists.
            /// </summary>
            /// <param name="item">a newly created type</param>
            /// <returns>A stored type if it already exist, or the newly created type</returns>
            private SchemaType _UseOrCreate(SchemaType item)
            {
                if (_Types.TryGetValue(item.PersistentName, out SchemaType value)) return value;

                _Types[item.PersistentName] = item;

                return item;
            }

            public ObjectType UseAnyType() { return (ObjectType)_UseOrCreate( new ObjectType(this) ); }

            public StringType UseString() { return (StringType)_UseOrCreate( new StringType(this) ); }

            public EnumType FindEnum(string persistentName) { return Enumerations.FirstOrDefault(item => item.PersistentName == persistentName); }

            public ArrayType UseArray(SchemaType elementType) { return (ArrayType)_UseOrCreate( new ArrayType(this, elementType) ); }

            public ClassType FindClass(string persistentName) { return Classes.FirstOrDefault(item => item.PersistentName == persistentName); }

            public ClassType UseClass(string name) { return (ClassType)_UseOrCreate(new ClassType(this, name)); }

            public BlittableType UseBlittable(TypeInfo t, bool isNullable = false)
            {
                if (t == null || !t.IsValueType) throw new ArgumentException(nameof(t));

                var item = new BlittableType(this, t, isNullable);
                return (BlittableType)_UseOrCreate(item);
            }

            public EnumType UseEnum(string name, bool isNullable = false)
            {
                var item = new EnumType(this, name, isNullable);
                return (EnumType)_UseOrCreate(item);
            }

            public EnumType GetEnum(string name)
            {
                return _Types.TryGetValue(name, out SchemaType etype) ? etype as EnumType : null;
            }            

            public DictionaryType UseDictionary(SchemaType key, SchemaType val)
            {                
                return (DictionaryType)_UseOrCreate( new DictionaryType(this, key, val) );
            }            

            public void Remove(SchemaType type) { _Types.Remove(type.PersistentName); }

            public void Remove(string persistentName) { _Types.Remove(persistentName); }

            public void IgnoredByCodeEmitter(string persistentName)
            {
                var ct = Classes.FirstOrDefault(item => item.PersistentName == persistentName);

                if (ct != null) ct.IgnoredByEmitter = true;            
            }

            public void IgnoredByCodeEmittierMainSchema()
            {
                IgnoredByCodeEmitter("glTF Property");                
                IgnoredByCodeEmitter("glTF Child of Root Property");
                IgnoredByCodeEmitter("Texture Info");
                IgnoredByCodeEmitter("Material Normal Texture Info");
            }



            #endregion
        }
    }
}
