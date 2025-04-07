using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace SharpGLTF.SchemaReflection
{
    /// <summary>
    /// Base class for all schema Types
    /// </summary>
    /// <remarks>
    /// Derived classes:
    /// - <see cref="StringType"/><br/>
    /// - <see cref="ObjectType"/><br/>
    /// - <see cref="BlittableType"/><br/>
    /// - <see cref="EnumType"/><br/>
    /// - <see cref="ArrayType"/><br/>
    /// - <see cref="DictionaryType"/><br/>
    /// - <see cref="ClassType"/><br/>
    /// - <see cref="ReferenceType"/><br/>
    /// </remarks>
    public abstract partial class SchemaType
    {
        #region constructor

        protected SchemaType(Context ctx) { _Owner = ctx; }

        #endregion

        #region data

        /// <summary>
        /// context where this type is stored
        /// </summary>
        private readonly Context _Owner;        

        /// <summary>
        /// identifier used for serialization and deserialization
        /// </summary>
        public abstract string PersistentName { get; }

        /// <summary>
        /// This is the value usually found in the schema's "$id:"
        /// </summary>
        /// <example>
        /// material.occlusionTextureInfo.schema.json
        /// </example>
        public string Identifier { get; set; }

        public String Description { get; set; }

        #endregion

        #region properties

        /// <summary>
        /// Short version of <see cref="Identifier"/>
        /// </summary>
        /// <example>
        /// occlusionTextureInfo
        /// </example>
        public string ShortIdentifier
        {
            get
            {
                var id = this.Identifier.Replace(".schema.json","", StringComparison.OrdinalIgnoreCase);
                var idx = id.LastIndexOf('.');
                return idx < 0 ? id : id.Substring(idx+1);
            }
        }
            

        

        public Context Owner => _Owner;

        #endregion
    }

    /// <summary>
    /// Represents a <see cref="String"/> type.
    /// </summary>
    [System.Diagnostics.DebuggerDisplay("StringType {PersistentName}")]
    public sealed class StringType : SchemaType
    {
        #region constructor

        internal StringType(Context ctx) : base(ctx) { }

        #endregion

        #region properties

        public override string PersistentName => typeof(String).Name;

        #endregion
    }

    /// <summary>
    /// Represents an undefined <see cref="Object"/> type.
    /// </summary>
    /// <remarks>
    /// undefined objects are serialized/deserialized as <see cref="System.Text.Json.Nodes.JsonNode"/>
    /// </remarks>
    [System.Diagnostics.DebuggerDisplay("ObjectType {PersistentName}")]
    public sealed class ObjectType : SchemaType
    {
        #region constructor

        internal ObjectType(Context ctx) : base(ctx) { }

        #endregion

        #region properties

        public override string PersistentName => typeof(System.Text.Json.Nodes.JsonNode).FullName;

        #endregion
    }

    /// <summary>
    /// Represents a "by value" type <see cref="Boolean"/>, <see cref="Int32"/>, <see cref="Single"/>, etc
    /// </summary>
    /// <remarks>
    /// This type can optionally be set as nullable.
    /// </remarks>
    [System.Diagnostics.DebuggerDisplay("BlittableType {PersistentName}")]
    public sealed class BlittableType : SchemaType
    {
        #region constructor        

        internal BlittableType(Context ctx, TypeInfo t, bool isNullable) : base(ctx)
        {
            if (t == typeof(String).GetTypeInfo()) isNullable = false;

            _Type = t;
            _IsNullable = isNullable;
        }

        #endregion

        #region data

        // https://en.wikipedia.org/wiki/Blittable_types

        private readonly TypeInfo _Type;
        private readonly Boolean _IsNullable;

        #endregion

        #region properties

        public TypeInfo DataType => _Type;

        public bool IsNullable => _IsNullable;

        public override string PersistentName => _IsNullable ? $"{_Type.Name}?" : _Type.Name;

        #endregion
    }    

    [System.Diagnostics.DebuggerDisplay("EnumType {PersistentName}")]
    public sealed class EnumType : SchemaType
    {
        #region constructor

        internal EnumType(Context ctx, string name, bool isNullable) : base(ctx)
        {
            _PersistentName = name;
            _IsNullable = isNullable;
        }

        #endregion

        #region data

        private readonly String _PersistentName;
        private readonly Boolean _IsNullable;

        private bool _UseIntegers;

        private readonly Dictionary<string, int> _Values = new Dictionary<string, int>();

        #endregion

        #region properties

        public bool IsNullable => _IsNullable;

        public override string PersistentName => _PersistentName;
        
        public bool UseIntegers { get => _UseIntegers; set => _UseIntegers = value; }

        public SchemaType ItemType => UseIntegers ? (SchemaType)Owner.UseBlittable(typeof(int).GetTypeInfo()) : Owner.UseString();

        public IEnumerable<KeyValuePair<string, int>> Values => _Values;

        #endregion

        #region API

        public void SetValue(string key, int val) { _Values[key] = val; }

        #endregion
    }

    [System.Diagnostics.DebuggerDisplay("ArrayType {PersistentName}")]
    public sealed class ArrayType : SchemaType
    {
        #region constructor

        internal ArrayType(Context ctx, SchemaType element) : base(ctx)
        {
            _ItemType = element;
        }

        #endregion

        #region data

        private readonly SchemaType _ItemType;

        public SchemaType ItemType => _ItemType;

        #endregion

        #region properties

        public override string PersistentName => $"{_ItemType.PersistentName}[]";

        #endregion
    }    

    [System.Diagnostics.DebuggerDisplay("DictionaryType {PersistentName}")]
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

    /// <summary>
    /// Represents a field property within a <see cref="ClassType"/>
    /// </summary>
    [System.Diagnostics.DebuggerDisplay("FieldInfo {_FieldType} {FieldType.PersistentName} {PersistentName}")]
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

        private Object _ExclusiveMinimumValue;
        private Object _InclusiveMinimumValue;

        private Object _DefaultValue;

        private Object _InclusiveMaximumValue;
        private Object _ExclusiveMaximumValue;

        private int _MinItems;
        private int _MaxItems;

        #endregion

        #region properties

        public ClassType DeclaringClass => _Owner;

        public String Description { get; set; }

        public String PersistentName => _PersistentName;

        public SchemaType FieldType { get => _FieldType; set => _FieldType = value; }
        
        public Object ExclusiveMinimumValue { get => _ExclusiveMinimumValue; set => _ExclusiveMinimumValue = value; }
        public Object InclusiveMinimumValue { get => _InclusiveMinimumValue; set => _InclusiveMinimumValue = value; }
        public Object DefaultValue { get => _DefaultValue; set => _DefaultValue = value; }
        public Object InclusiveMaximumValue { get => _InclusiveMaximumValue; set => _InclusiveMaximumValue = value; }
        public Object ExclusiveMaximumValue { get => _ExclusiveMaximumValue; set => _ExclusiveMaximumValue = value; }

        public int MinItems { get => _MinItems; set => _MinItems = value; }
        public int MaxItems { get => _MaxItems; set => _MaxItems = value; }

        #endregion

        #region fluent api

        public FieldInfo SetDataType(SchemaType type) { _FieldType = type; return this; }

        public FieldInfo SetDataType(Type type, bool isNullable)
        {
            if (type == typeof(string)) { _FieldType = DeclaringClass.Owner.UseString(); return this; }

            _FieldType = DeclaringClass.Owner.UseBlittable(type.GetTypeInfo(), isNullable);
            return this;
        }

        public FieldInfo RemoveDefaultValue() { _DefaultValue = null; return this; }

        public FieldInfo SetDefaultValue(string defval) { _DefaultValue = defval; return this; }

        public FieldInfo SetLimits(Decimal? min, Decimal? max) { _InclusiveMinimumValue = min; _InclusiveMaximumValue = max; return this; }

        public FieldInfo SetItemsRange(int min, int max = int.MaxValue) { _MinItems = min; _MaxItems = max; return this; }

        #endregion

        #region comparer helper

        private sealed class _Comparer : IEqualityComparer<FieldInfo> , IComparer<FieldInfo>
        {
            public int Compare(FieldInfo x, FieldInfo y)
            {
                var xx = _Adjust(x._PersistentName);
                var yy = _Adjust(y._PersistentName);

                return string.CompareOrdinal(xx, yy);
            }

            private static string _Adjust(string name)
            {
                if (name.StartsWith("asset")) return "____" + name;
                if (name.StartsWith("extensions")) return "___" + name;
                return name;
            }

            public bool Equals(FieldInfo x, FieldInfo y) { return Compare(x,y) == 0; }

            public int GetHashCode(FieldInfo obj) { return obj._PersistentName.GetHashCode(StringComparison.Ordinal); }
        }

        private static readonly _Comparer _DefaultComparer = new _Comparer();

        public static IEqualityComparer<FieldInfo> EqualityComparer => _DefaultComparer;

        public static IComparer<FieldInfo> Comparer => _DefaultComparer;

        #endregion
    }

    [System.Diagnostics.DebuggerDisplay("ClassType {PersistentName} : {BaseClass.PersistentName}")]
    public sealed class ClassType : SchemaType
    {
        #region constructor

        internal ClassType(Context ctx, string name) : base(ctx)
        {
            _PersistentName = name;            
        }

        #endregion

        #region data

        private readonly String _PersistentName;        

        private readonly SortedSet<FieldInfo> _Fields = new SortedSet<FieldInfo>(FieldInfo.Comparer);

        private ClassType _BaseClass;        

        /// <summary>
        /// True to prevent to codegen emitter to emit this class
        /// </summary>
        public bool IgnoredByEmitter { get; set; }

        #endregion

        #region properties
        public override string PersistentName => _PersistentName;
        public IEnumerable<FieldInfo> Fields => _Fields;
        public ClassType BaseClass { get => _BaseClass; set => _BaseClass = value; }

        #endregion

        #region API

        public FieldInfo GetField(string name)
        {
            return _Fields.First(item => item.PersistentName == name);
        }

        public FieldInfo UseField(string name)
        {
            var f = new FieldInfo(this, name);

            _Fields.Add(f);

            return GetField(name);
        }

        #endregion
    }

    /// <summary>
    /// not used
    /// </summary>
    public sealed class ReferenceType : SchemaType
    {
        #region constructor

        internal ReferenceType(Context ctx, SchemaType refType) : base(ctx)
        {
            _ReferencedType = refType;
        }

        #endregion

        #region data

        // In code it has the representation List<Node>();
        // In serialization, it has the representation List<int>();

        private readonly SchemaType _ReferencedType;

        #pragma warning disable CA1065 // Do not raise exceptions in unexpected locations
        public override string PersistentName => throw new NotImplementedException();
        #pragma warning restore CA1065 // Do not raise exceptions in unexpected locations

        #endregion
    }

        

}
