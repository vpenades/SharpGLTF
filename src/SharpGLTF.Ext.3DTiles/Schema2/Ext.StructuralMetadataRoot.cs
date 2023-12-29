using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;

using OneOf;

namespace SharpGLTF.Schema2.Tiles3D
{
    using Collections;
    using Memory;
    using Validation;

    using METADATAORURI = OneOf<StructuralMetadataSchema, Uri>;

    partial class Tiles3DExtensions
    {
        public static void SetPropertyAttribute(
            this ModelRoot modelRoot,
            PropertyAttribute propertyAttribute,
            METADATAORURI schema)
        {
            SetPropertyAttributes(modelRoot, new List<PropertyAttribute>() { propertyAttribute }, schema);
        }

        public static void SetPropertyAttributes(
this ModelRoot modelRoot,
List<PropertyAttribute> propertyAttributes,
METADATAORURI schema)
        {
            if (propertyAttributes == null || propertyAttributes.Count == 0) { modelRoot.RemoveExtensions<EXTStructuralMetadataRoot>(); return; }

            var ext = modelRoot.UseExtension<EXTStructuralMetadataRoot>();
            // ext.PropertyAttributes = propertyAttributes;
            throw new NotImplementedException();
            ext.AddSchema(schema);
        }


        public static void SetPropertyTexture(
    this ModelRoot modelRoot,
    PropertyTexture propertyTexture,
    METADATAORURI schema)
        {
            SetPropertyTextures(modelRoot, new List<PropertyTexture>() { propertyTexture }, schema);
        }


        public static void SetPropertyTextures(
    this ModelRoot modelRoot,
    List<PropertyTexture> propertyTextures,
    METADATAORURI schema)
        {
            if (propertyTextures == null || propertyTextures.Count == 0) { modelRoot.RemoveExtensions<EXTStructuralMetadataRoot>(); return; }

            var ext = modelRoot.UseExtension<EXTStructuralMetadataRoot>();
            // ext.PropertyTextures = propertyTextures;
            throw new NotImplementedException();
            ext.AddSchema(schema);
        }

        public static void SetPropertyTable(
            this ModelRoot modelRoot,
            PropertyTable propertyTable,
            METADATAORURI schema)
        {
            SetPropertyTables(modelRoot, new List<PropertyTable>() { propertyTable }, schema);
        }

        public static void SetPropertyTables(
            this ModelRoot modelRoot,
            List<PropertyTable> propertyTables,
            METADATAORURI schema)
        {
            if (propertyTables == null || propertyTables.Count == 0) { modelRoot.RemoveExtensions<EXTStructuralMetadataRoot>(); return; }

            var ext = modelRoot.UseExtension<EXTStructuralMetadataRoot>();
            // ext.PropertyTables = propertyTables;
            throw new NotImplementedException();
            ext.AddSchema(schema);
        }

        public static PropertyTableProperty GetArrayPropertyTableProperty<T>(this ModelRoot model, List<List<T>> values, bool CreateArrayOffsets = true)
        {
            var propertyTableProperty = new PropertyTableProperty();
            int logicalIndex = GetBufferView(model, values);
            propertyTableProperty.Values = logicalIndex;

            if (CreateArrayOffsets)
            {
                var arrayOffsets = BinaryTable.GetArrayOffsets(values);
                int logicalIndexOffsets = GetBufferView(model, arrayOffsets);
                propertyTableProperty.ArrayOffsets = logicalIndexOffsets;

                if (typeof(T) == typeof(string))
                {
                    var stringValues = values.ConvertAll(x => x.ConvertAll(y => (string)Convert.ChangeType(y, typeof(string), CultureInfo.InvariantCulture)));
                    var stringOffsets = BinaryTable.GetStringOffsets(stringValues);
                    int offsets = GetBufferView(model, stringOffsets);
                    propertyTableProperty.StringOffsets = offsets;
                }
            }
            return propertyTableProperty;
        }

        public static PropertyTableProperty GetPropertyTableProperty<T>(this ModelRoot model, List<T> values)
        {
            var propertyTableProperty = new PropertyTableProperty();
            int logicalIndex = GetBufferView(model, values);
            propertyTableProperty.Values = logicalIndex;

            if (typeof(T) == typeof(string))
            {
                var stringvalues = values.ConvertAll(x => (string)Convert.ChangeType(x, typeof(string), CultureInfo.InvariantCulture));
                var stringOffsets = BinaryTable.GetStringOffsets(stringvalues);
                int offsets = GetBufferView(model, stringOffsets);
                propertyTableProperty.StringOffsets = offsets;
            }

            return propertyTableProperty;
        }

        private static int GetBufferView<T>(this ModelRoot model, List<T> values)
        {
            var bytes = BinaryTable.GetBytes(values);
            var bufferView = model.UseBufferView(bytes);
            int logicalIndex = bufferView.LogicalIndex;
            return logicalIndex;
        }

        private static int GetBufferView<T>(this ModelRoot model, List<List<T>> values)
        {
            List<byte> bytes = BinaryTable.GetBytesForArray(values);
            var bufferView = model.UseBufferView(bytes.ToArray());
            int logicalIndex = bufferView.LogicalIndex;
            return logicalIndex;
        }
    }

    public partial class EXTStructuralMetadataRoot
    {
        #region lifecycle

        internal EXTStructuralMetadataRoot(ModelRoot modelRoot)
        {
            this.modelRoot = modelRoot;
            _propertyTables = new ChildrenList<PropertyTable, EXTStructuralMetadataRoot>(this);
            _propertyAttributes = new ChildrenList<PropertyAttribute, EXTStructuralMetadataRoot>(this);
            _propertyTextures = new ChildrenList<PropertyTexture, EXTStructuralMetadataRoot>(this);
        }

        protected override IEnumerable<ExtraProperties> GetLogicalChildren()
        {
            var items = base.GetLogicalChildren()
                .Concat(_propertyTables)
                .Concat(_propertyAttributes)
                .Concat(_propertyTextures);

            if (Schema != null) items = items.Append(Schema);

            return items;
        }

        #endregion

        #region data

        private ModelRoot modelRoot;

        #endregion

        #region properties

        internal string SchemaUri
        {
            get => _schemaUri;
            set { _schemaUri = value; }
        }

        internal StructuralMetadataSchema Schema
        {
            get => _schema;
            set { GetChildSetter(this).SetListProperty(ref _schema, value); }
        }

        internal IReadOnlyList<PropertyTable> PropertyTables => _propertyTables;
        internal IReadOnlyList<PropertyAttribute> PropertyAttributes => _propertyAttributes;
        internal IReadOnlyList<PropertyTexture> PropertyTextures => _propertyTextures;

        #endregion

        #region API

        internal void AddSchema(METADATAORURI schema)
        {
            schema.Switch(
                StructuralMetadataSchema => _schema = StructuralMetadataSchema,
                Uri => this.SchemaUri = Uri.ToString()
                );
        }

        #endregion

        #region validation

        protected override void OnValidateReferences(ValidationContext validate)
        {
            foreach (var propertyTexture in PropertyTextures)
            {
                foreach (var propertyTextureProperty in propertyTexture.Properties)
                {
                    var textureId = propertyTextureProperty.Value.LogicalTextureIndex;
                    validate.IsNullOrIndex(nameof(propertyTexture), textureId, modelRoot.LogicalTextures);
                }
            }

            foreach (var propertyTable in PropertyTables)
            {
                Guard.NotNull(Schema.Classes[propertyTable.Class], nameof(propertyTable.Class), $"Schema must have class {propertyTable.Class}");

                foreach (var property in propertyTable.Properties)
                {
                    Guard.NotNull(Schema.Classes[propertyTable.Class].Properties[property.Key], nameof(property.Key), $"Schema must have property {property.Key}");

                    var values = property.Value.Values;
                    validate.IsNullOrIndex(nameof(propertyTable), values, modelRoot.LogicalBufferViews);

                    if (property.Value.ArrayOffsets.HasValue)
                    {
                        var arrayOffsets = property.Value.ArrayOffsets.Value;
                        validate.IsNullOrIndex(nameof(propertyTable), arrayOffsets, modelRoot.LogicalBufferViews);
                    }

                    if (property.Value.StringOffsets.HasValue)
                    {
                        var stringOffsets = property.Value.StringOffsets.Value;
                        validate.IsNullOrIndex(nameof(propertyTable), stringOffsets, modelRoot.LogicalBufferViews);
                    }
                }
            }

            if (Schema != null)
            {
                foreach (var @class in Schema.Classes)
                {
                    foreach (var property in @class.Value.Properties)
                    {
                        if (property.Value.Type == ElementType.ENUM)
                        {
                            Guard.IsTrue(Schema.Enums.ContainsKey(property.Value.EnumType), nameof(property.Value.EnumType), $"Enum {property.Value.EnumType} must be defined in schema");
                        }
                    }
                }
            }

            base.OnValidateReferences(validate);
        }

        protected override void OnValidateContent(ValidationContext result)
        {
            // check schema id is defined and valid
            if (Schema != null && !String.IsNullOrEmpty(Schema.Id))
            {
                var regex = "^[a-zA-Z_][a-zA-Z0-9_]*$";
                Guard.IsTrue(System.Text.RegularExpressions.Regex.IsMatch(Schema.Id, regex), nameof(Schema.Id));


                foreach(var _class in Schema.Classes)
                {
                    Guard.IsTrue(System.Text.RegularExpressions.Regex.IsMatch(_class.Key, regex), nameof(_class.Key));

                    foreach(var property in _class.Value.Properties)
                    {
                        if (property.Value.Count.HasValue)
                        {
                            Guard.MustBeGreaterThanOrEqualTo(property.Value.Count.Value, 2, nameof(property.Value.Count));
                        }
                    }
                }
            }

            foreach (var propertyTexture in PropertyTextures)
            {
                foreach (var propertyTextureProperty in propertyTexture.Properties)
                {
                    var texCoord = propertyTextureProperty.Value.TextureCoordinate;
                    var channels = propertyTextureProperty.Value.Channels;
                    var index = propertyTextureProperty.Value.LogicalTextureIndex;
                    Guard.MustBeGreaterThanOrEqualTo(texCoord, 0, nameof(texCoord));
                    Guard.IsTrue(channels.Count > 0, nameof(channels), "Channels must be defined");
                    Guard.IsTrue(index >= 0, nameof(index), "Index must be defined");
                }
            }

            foreach (var propertyTable in PropertyTables)
            {
                Guard.IsTrue(propertyTable.Class != null, nameof(propertyTable.Class), "Class must be defined");
                Guard.IsTrue(propertyTable.Count > 0, nameof(propertyTable.Count), "Count must be greater than 0");
                Guard.IsTrue(propertyTable.Properties.Count > 0, nameof(propertyTable.Properties), "Properties must be defined");
            }

            // Check one of schema or schemaUri is defined, but not both
            Guard.IsFalse(Schema != null && SchemaUri != null, "Schema/SchemaUri", "Schema and SchemaUri cannot both be defined");
            Guard.IsFalse(Schema == null && SchemaUri == null, "Schema/SchemaUri", "One of Schema and SchemaUri must be defined");

            base.OnValidateContent(result);
        }

        #endregion
    }

    public partial class PropertyTexture : IChildOfList<EXTStructuralMetadataRoot>
    {
        #region lifecycle
        public PropertyTexture()
        {
            _properties = new ChildrenDictionary<PropertyTextureProperty, PropertyTexture>(this);
        }

        protected override IEnumerable<ExtraProperties> GetLogicalChildren()
        {
            return base.GetLogicalChildren()
                .Concat(_properties.Values);
        }

        #endregion

        #region child properties

        public int LogicalIndex { get; private set; } = -1;
        
        public EXTStructuralMetadataRoot LogicalParent { get; private set; }

        void IChildOfList<EXTStructuralMetadataRoot>.SetLogicalParent(EXTStructuralMetadataRoot parent, int index)
        {
            LogicalParent = parent;
            LogicalIndex = index;
        }

        #endregion

        #region properties

        public string ClassName
        {
            get => _class;
            set => _class = value;
        }

        public IReadOnlyDictionary<string, PropertyTextureProperty> Properties => _properties;

        #endregion
    }

    public partial class PropertyTextureProperty : IChildOfDictionary<PropertyTexture>
    {
        #region lifecycle
        public PropertyTextureProperty()
        {
            _channels = new List<int>();
        }

        #endregion

        #region child properties

        public string LogicalKey { get; private set; }

        public PropertyTexture LogicalParent { get; private set; }

        void IChildOfDictionary<PropertyTexture>.SetLogicalParent(PropertyTexture parent, string key)
        {
            LogicalParent = parent;
            LogicalKey = key;
        }

        #endregion

        #region data

        public List<int> Channels => _channels;

        #endregion
    }

    public partial class PropertyAttribute : IChildOfList<EXTStructuralMetadataRoot>
    {
        #region lifecycle
        public PropertyAttribute()
        {
            _properties = new ChildrenDictionary<PropertyAttributeProperty, PropertyAttribute>(this);
        }

        protected override IEnumerable<ExtraProperties> GetLogicalChildren()
        {
            return base.GetLogicalChildren()
                .Concat(_properties.Values);
        }

        #endregion

        #region child properties

        /// <summary>
        /// Gets the zero-based index of this <see cref="MeshExtInstanceFeatureID"/> at <see cref="MeshExtInstanceFeatures.FeatureIds"/>.
        /// </summary>
        public int LogicalIndex { get; private set; } = -1;

        /// <summary>
        /// Gets the <see cref="MeshExtInstanceFeatures"/> instance that owns this <see cref="MeshExtInstanceFeatureID"/> instance.
        /// </summary>
        public EXTStructuralMetadataRoot LogicalParent { get; private set; }

        void IChildOfList<EXTStructuralMetadataRoot>.SetLogicalParent(EXTStructuralMetadataRoot parent, int index)
        {
            LogicalParent = parent;
            LogicalIndex = index;
        }

        #endregion

        #region properties
        public string Class
        {
            get => _class;
            set => _class = value;
        }

        public IReadOnlyDictionary<string, PropertyAttributeProperty> Properties => _properties;

        #endregion

    }

    public partial class PropertyAttributeProperty : IChildOfDictionary<PropertyAttribute>
    {
        #region child properties

        public string LogicalKey { get; private set; }

        public PropertyAttribute LogicalParent { get; private set; }

        void IChildOfDictionary<PropertyAttribute>.SetLogicalParent(PropertyAttribute parent, string key)
        {
            LogicalParent = parent;
            LogicalKey = key;
        }

        #endregion

        #region properties

        public string Attribute
        {
            get => _attribute;
            set => _attribute = value;
        }

        #endregion
    }

    public partial class StructuralMetadataSchema : IChildOfList<EXTStructuralMetadataRoot>
    {
        #region lifecycle
        public StructuralMetadataSchema()
        {
            _classes = new ChildrenDictionary<StructuralMetadataClass, StructuralMetadataSchema>(this);
            _enums = new ChildrenDictionary<StructuralMetadataEnum, StructuralMetadataSchema>(this);
        }

        protected override IEnumerable<ExtraProperties> GetLogicalChildren()
        {
            return base.GetLogicalChildren()
                .Concat(_classes.Values)
                .Concat(_enums.Values);
        }

        #endregion

        #region child properties

        public int LogicalIndex { get; private set; } = -1;

        public EXTStructuralMetadataRoot LogicalParent { get; private set; }

        void IChildOfList<EXTStructuralMetadataRoot>.SetLogicalParent(EXTStructuralMetadataRoot parent, int index)
        {
            LogicalParent = parent;
            LogicalIndex = index;
        }

        #endregion

        #region properties        

        public IReadOnlyDictionary<string, StructuralMetadataClass> Classes => _classes;
        public IReadOnlyDictionary<string, StructuralMetadataEnum> Enums => _enums;

        public string Id
        {
            get => _id;
            set => _id = value;
        }

        public string Version
        {
            get => _version;
            set => _version = value;
        }

        public string Name
        {
            get => _name;
            set => _name = value;
        }

        public string Description
        {
            get => _description;
            set => _description = value;
        }        

        #endregion
    }

    public partial class StructuralMetadataEnum : IChildOfDictionary<StructuralMetadataSchema>
    {
        #region lifecycle
        public StructuralMetadataEnum()
        {
            _values = new List<EnumValue>();
        }

        #endregion

        #region child properties

        public string LogicalKey { get; private set; }

        public StructuralMetadataSchema LogicalParent { get; private set; }

        void IChildOfDictionary<StructuralMetadataSchema>.SetLogicalParent(StructuralMetadataSchema parent, string key)
        {
            LogicalParent = parent;
            LogicalKey = key;
        }

        #endregion

        #region properties

        public string Name
        {
            get => _name;
            set => _name = value;
        }
        public string Description
        {
            get => _description;
            set => _description = value;
        }
        public List<EnumValue> Values
        {
            get => _values;
            set => _values = value;
        }

        #endregion
    }

    public partial class EnumValue
    {
        public string Description
        {
            get => _description;
            set => _description = value;
        }
        public string Name
        {
            get => _name;
            set => _name = value;
        }
        public int Value
        {
            get => _value;
            set => _value = value;
        }
    }

    public partial class StructuralMetadataClass : IChildOfDictionary<StructuralMetadataSchema>
    {
        #region lifecycle

        public StructuralMetadataClass()
        {
            _properties = new ChildrenDictionary<ClassProperty, StructuralMetadataClass>(this);
        }

        protected override IEnumerable<ExtraProperties> GetLogicalChildren()
        {
            return base.GetLogicalChildren()
                .Concat(_properties.Values);
        }

        #endregion

        #region child properties

        public string LogicalKey { get; private set; }

        public StructuralMetadataSchema LogicalParent { get; private set; }

        void IChildOfDictionary<StructuralMetadataSchema>.SetLogicalParent(StructuralMetadataSchema parent, string key)
        {
            LogicalParent = parent;
            LogicalKey = key;
        }

        #endregion

        #region properties

        public IReadOnlyDictionary<string, ClassProperty> Properties => _properties;

        public string Name
        {
            get => _name;
            set => _name = value;
        }

        public string Description
        {
            get => _description;
            set => _description = value;
        }

        #endregion

    }

    public partial class ClassProperty : IChildOfDictionary<StructuralMetadataClass>
    {
        #region child properties

        public string LogicalKey { get; private set; }

        public StructuralMetadataClass LogicalParent { get; private set; }

        void IChildOfDictionary<StructuralMetadataClass>.SetLogicalParent(StructuralMetadataClass parent, string key)
        {
            LogicalParent = parent;
            LogicalKey = key;
        }

        #endregion

        #region properties
        public string Name
        {
            get => _name;
            set => _name = value;
        }

        public string Description
        {
            get => _description;
            set => _description = value;
        }

        public ElementType Type
        {
            get => _type;
            set => _type = value;
        }

        public string EnumType
        {
            get => _enumType;
            set => _enumType = value;
        }

        public DataType? ComponentType
        {
            get => _componentType;
            set => _componentType = value;
        }

        public bool? Required
        {
            get => _required;
            set => _required = value;
        }

        public bool? Normalized
        {
            get => _normalized;
            set => _normalized = value;
        }

        public bool? Array
        {
            get => _array;
            set => _array = value;
        }

        public int? Count
        {
            get => _count;
            set => _count = value;
        }

        #endregion
    }

    /// <remarks>
    /// Represents a Propery table of <see cref="EXTStructuralMetadataRoot"/>
    /// </remarks> 
    public partial class PropertyTable : IChildOfList<EXTStructuralMetadataRoot>
    {
        #region lifecycle
        public PropertyTable()
        {
            _properties = new ChildrenDictionary<PropertyTableProperty, PropertyTable>(this);
        }
        public PropertyTable(string Class, int Count, string Name = "") : this()
        {
            _class = Class;
            _count = Count;
            _name = Name;
        }

        protected override IEnumerable<ExtraProperties> GetLogicalChildren()
        {
            return base.GetLogicalChildren()
                .Concat(_properties.Values);
        }

        #endregion

        #region child properties

        public int LogicalIndex { get; private set; } = -1;        
        public EXTStructuralMetadataRoot LogicalParent { get; private set; }

        void IChildOfList<EXTStructuralMetadataRoot>.SetLogicalParent(EXTStructuralMetadataRoot parent, int index)
        {
            LogicalParent = parent;
            LogicalIndex = index;
        }

        #endregion

        #region properties

        public IReadOnlyDictionary<string, PropertyTableProperty> Properties => _properties;

        public string Name
        {
            get => _name;
            set => _name = value;
        }

        public string Class
        {
            get => _class;
            set => _class = value;
        }

        public int Count
        {
            get => _count;
            set => _count = value;
        }        

        #endregion
    }

    /// <remarks>
    /// Represents a Property of <see cref="PropertyTable"/>
    /// </remarks>    
    public partial class PropertyTableProperty : IChildOfDictionary<PropertyTable>
    {
        #region child properties

        public string LogicalKey { get; private set; }

        public PropertyTable LogicalParent { get; private set; }

        void IChildOfDictionary<PropertyTable>.SetLogicalParent(PropertyTable parent, string key)
        {
            LogicalParent = parent;
            LogicalKey = key;
        }

        #endregion

        #region properties
        /// <summary>
        /// this is an index to a BufferView
        /// </summary>
        public int Values
        {
            get => _values;
            set => _values = value;
        }

        public int? ArrayOffsets
        {
            get => _arrayOffsets;
            set => _arrayOffsets = value;
        }

        public int? StringOffsets
        {
            get => _stringOffsets;
            set => _stringOffsets = value;
        }

        #endregion
    }
}

