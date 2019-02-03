using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Epsylon.glTF2Toolkit.CodeGen
{
    // this is a mechanism to define serializable types, pretty much like Type and TypeInfo,
    // but the types can be defined as "Virtual Types" because they don't actually exist.
    // .
    // this is useful to emit source code from schemas, since the types to emit don't exist prior emission.
    // .
    // Alternatives:
    // 1. Type and TypeInfo inherit from MemberInfo and IReflect ... but in the end, you must implement a lot of code
    // 2. Roslyn has a mechanism to define types, but it's missing the "emit serialization" part.



    // In here we define a schema intended only for serialization
    

    public abstract class SchemaType
    {
        protected SchemaType(Context ctx) { _Owner = ctx; }

        internal readonly Context _Owner;

        protected Context Owner => _Owner;

        public abstract string PersistentName { get; }

        public sealed class Context
        {
            #region data

            private readonly Dictionary<string, SchemaType> _Types = new Dictionary<string, SchemaType>();

            private TypeInfo _RefType;

            #endregion

            #region properties

            public IEnumerable<EnumType> Enumerations => _Types.Values.OfType<EnumType>();

            public IEnumerable<ClassType> Classes => _Types.Values.OfType<ClassType>();

            #endregion

            #region API

            private SchemaType _UseOrCreate(SchemaType item)
            {
                if (_Types.TryGetValue(item.PersistentName, out SchemaType value)) return value;

                _Types[item.PersistentName] = item;

                return item;
            }

            public ObjectType UseAnyType()
            {
                var item = new ObjectType(this);
                return (ObjectType)_UseOrCreate(item);
            }

            public StringType UseString()
            {
                var item = new StringType(this);
                return (StringType)_UseOrCreate(item);
            }

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

            public ArrayType UseArray(SchemaType elementType)
            {
                var item = new ArrayType(this,elementType);
                return (ArrayType)_UseOrCreate(item);                
            }            

            public DictionaryType UseDictionary(SchemaType key, SchemaType val)
            {
                var item = new DictionaryType(this, key,val);
                return (DictionaryType)_UseOrCreate(item);
            }

            public ClassType UseClass(string name)
            {
                var item = new ClassType(this, name);
                return (ClassType)_UseOrCreate(item);
            }

            public void Remove(SchemaType type) { _Types.Remove(type.PersistentName); }

            public void Remove(string persistentName)
            {
                _Types.Remove(persistentName);
            }

            #endregion
        }        
    }

    [System.Diagnostics.DebuggerDisplay("{PersistentName}")]
    public sealed class StringType : SchemaType
    {
        // https://en.wikipedia.org/wiki/Blittable_types        

        internal StringType(Context ctx) : base(ctx) { }

        public override string PersistentName => typeof(String).Name;        
    }

    [System.Diagnostics.DebuggerDisplay("{PersistentName}")]
    public sealed class ObjectType : SchemaType
    {
        // https://en.wikipedia.org/wiki/Blittable_types        

        internal ObjectType(Context ctx) : base(ctx) { }

        public override string PersistentName => typeof(Object).Name;

        
    }

    [System.Diagnostics.DebuggerDisplay("{PersistentName}")]
    public sealed class BlittableType : SchemaType // blittable
    {
        // https://en.wikipedia.org/wiki/Blittable_types        

        internal BlittableType(Context ctx, TypeInfo t, bool isNullable) : base(ctx)
        {
            if (t == typeof(String).GetTypeInfo()) isNullable = false;

            _Type = t;
            _IsNullable = isNullable;
        }        

        private readonly TypeInfo _Type;
        private readonly Boolean _IsNullable;

        public TypeInfo DataType => _Type;

        public bool IsNullable => _IsNullable;

        public override string PersistentName => _IsNullable ? $"{_Type.Name}?" : _Type.Name;        
    }

    

    [System.Diagnostics.DebuggerDisplay("enum {PersistentName}")]
    public sealed class EnumType : SchemaType
    {
        internal EnumType(Context ctx, string name, bool isNullable) : base(ctx)
        {
            _PersistentName = name;
            _IsNullable = isNullable;
        }

        private readonly String _PersistentName;
        private readonly Boolean _IsNullable;

        private bool _UseIntegers;
        private readonly Dictionary<string, int> _Values = new Dictionary<string, int>();

        public bool IsNullable => _IsNullable;

        public override string PersistentName => _PersistentName;
        
        public bool UseIntegers { get => _UseIntegers; set => _UseIntegers = value; }

        public SchemaType ItemType => UseIntegers ? (SchemaType)Owner.UseBlittable(typeof(int).GetTypeInfo()) : Owner.UseString();

        public IEnumerable<KeyValuePair<string, int>> Values => _Values;

        public void SetValue(string key, int val) { _Values[key] = val; }        
    }

    [System.Diagnostics.DebuggerDisplay("{PersistentName}")]
    public sealed class ArrayType : SchemaType
    {
        internal ArrayType(Context ctx, SchemaType element) : base(ctx)
        {
            _ItemType = element;
        }

        private readonly SchemaType _ItemType;

        public SchemaType ItemType => _ItemType;

        public override string PersistentName => $"{_ItemType.PersistentName}[]";        
    }    

    [System.Diagnostics.DebuggerDisplay("{PersistentName}")]
    public sealed class DictionaryType : SchemaType
    {
        #region lifecycle

        internal DictionaryType(Context ctx, SchemaType key,SchemaType val) : base(ctx)
        {            
            _KeyType = key;
            _ValueType = val;
        }

        #endregion

        #region data
        
        private readonly SchemaType _KeyType;
        private readonly SchemaType _ValueType;

        #endregion

        #region properties

        public SchemaType KeyType => _KeyType;

        public SchemaType ValueType => _ValueType;

        public override string PersistentName => $"<{_KeyType.PersistentName},{_ValueType.PersistentName}>[]";

        #endregion       
    }

    [System.Diagnostics.DebuggerDisplay("{FieldType.PersistentName} {PersistentName}")]
    public sealed class FieldInfo
    {
        #region lifecycle

        internal FieldInfo(ClassType owner, string name)
        {
            _Owner = owner;
            _PersistentName = name;
        }

        #endregion

        #region data

        private readonly ClassType _Owner;
        private readonly String _PersistentName;
        private SchemaType _FieldType;

        private Object _DefaultValue;
        private Object _MinimumValue;
        private Object _MaximumValue;

        private int _MinItems;
        private int _MaxItems;

        #endregion

        #region properties

        public ClassType DeclaringClass => _Owner;

        public string PersistentName => _PersistentName;                

        public SchemaType FieldType { get => _FieldType; set => _FieldType = value; }

        public Object DefaultValue { get => _DefaultValue; set => _DefaultValue = value; }
        public Object MinimumValue { get => _MinimumValue; set => _MinimumValue = value; }
        public Object MaximumValue { get => _MaximumValue; set => _MaximumValue = value; }

        public int MinItems { get => _MinItems; set => _MinItems = value; }
        public int MaxItems { get => _MaxItems; set => _MaxItems = value; }

        #endregion

        #region fluent api

        public FieldInfo SetDataType(SchemaType type) { _FieldType = type; return this; }

        public FieldInfo SetDataType(Type type, bool isNullable)
        {
            _FieldType = _Owner._Owner.UseBlittable(type.GetTypeInfo(), isNullable);
            return this;
        }

        public FieldInfo RemoveDefaultValue() { _DefaultValue = null; return this; }

        public FieldInfo SetDefaultValue(string defval) { _DefaultValue = defval; return this; }

        public FieldInfo SetLimits(Decimal? min, Decimal? max) { _MinimumValue = min; _MaximumValue = max; return this; }

        public FieldInfo SetItemsRange(int min, int max = int.MaxValue) { _MinItems = min; _MaxItems = max; return this; }

        #endregion

        #region comparer helper

        private sealed class _Comparer : IEqualityComparer<FieldInfo>
        {
            public bool Equals(FieldInfo x, FieldInfo y)
            {
                return x._PersistentName == y._PersistentName;
            }

            public int GetHashCode(FieldInfo obj)
            {
                return obj._PersistentName.GetHashCode();
            }
        }

        private static readonly _Comparer _DefaultComparer = new _Comparer();

        public static IEqualityComparer<FieldInfo> Comparer => _DefaultComparer;        

        #endregion
    }

    [System.Diagnostics.DebuggerDisplay("class {PersistentName} : {BaseClass.PersistentName}")]
    public sealed class ClassType : SchemaType
    {
        internal ClassType(Context ctx, string name) : base(ctx)
        {
            _PersistentName = name;            
        }

        private readonly String _PersistentName;        

        private readonly HashSet<FieldInfo> _Fields = new HashSet<FieldInfo>(FieldInfo.Comparer);

        private ClassType _BaseClass;
        
        public override string PersistentName => _PersistentName;

        public ClassType BaseClass { get => _BaseClass; set => _BaseClass = value; }

        public IEnumerable<FieldInfo> Fields => _Fields;

        public FieldInfo UseField(string name)
        {
            var f = new FieldInfo(this, name);

            _Fields.Add(f);

            return _Fields.FirstOrDefault(item => item.PersistentName == name);
        }        
    }

    
    public sealed class ReferenceType : SchemaType
    {
        internal ReferenceType(Context ctx, SchemaType refType) : base(ctx)
        {
            _ReferencedType = refType;
        }

        // In code it has the representation List<Node>();
        // In serialization, it has the representation List<int>();

        private readonly SchemaType _ReferencedType;

        public override string PersistentName => throw new NotImplementedException();        
    }

        

}
