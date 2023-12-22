using OneOf;
using SharpGLTF.Validation;
using System;
using System.Collections.Generic;
using System.Globalization;

namespace SharpGLTF.Schema2
{
    public static class ExtStructuralMetadataRoot
    {
        public static void SetPropertyAttribute(
            this ModelRoot modelRoot,
            PropertyAttribute propertyAttribute,
            OneOf<StructuralMetadataSchema, Uri> schema)
        {
            SetPropertyAttributes(modelRoot, new List<PropertyAttribute>() { propertyAttribute }, schema);
        }

        public static void SetPropertyAttributes(
this ModelRoot modelRoot,
List<PropertyAttribute> propertyAttributes,
OneOf<StructuralMetadataSchema, Uri> schema)
        {
            if (propertyAttributes == null || propertyAttributes.Count == 0) { modelRoot.RemoveExtensions<EXTStructuralMetadataRoot>(); return; }

            var ext = modelRoot.UseExtension<EXTStructuralMetadataRoot>();
            ext.PropertyAttributes = propertyAttributes;
            ext.AddSchema(schema);
        }


        public static void SetPropertyTexture(
    this ModelRoot modelRoot,
    PropertyTexture propertyTexture,
    OneOf<StructuralMetadataSchema, Uri> schema)
        {
            SetPropertyTextures(modelRoot, new List<PropertyTexture>() { propertyTexture }, schema);
        }


        public static void SetPropertyTextures(
    this ModelRoot modelRoot,
    List<PropertyTexture> propertyTextures,
    OneOf<StructuralMetadataSchema, Uri> schema)
        {
            if (propertyTextures == null || propertyTextures.Count == 0) { modelRoot.RemoveExtensions<EXTStructuralMetadataRoot>(); return; }

            var ext = modelRoot.UseExtension<EXTStructuralMetadataRoot>();
            ext.PropertyTextures = propertyTextures;
            ext.AddSchema(schema);
        }

        public static void SetPropertyTable(
            this ModelRoot modelRoot,
            PropertyTable propertyTable,
            OneOf<StructuralMetadataSchema, Uri> schema)
        {
            SetPropertyTables(modelRoot, new List<PropertyTable>() { propertyTable }, schema);
        }

        public static void SetPropertyTables(
            this ModelRoot modelRoot,
            List<PropertyTable> propertyTables,
            OneOf<StructuralMetadataSchema, Uri> schema)
        {
            if (propertyTables == null || propertyTables.Count == 0) { modelRoot.RemoveExtensions<EXTStructuralMetadataRoot>(); return; }

            var ext = modelRoot.UseExtension<EXTStructuralMetadataRoot>();
            ext.PropertyTables = propertyTables;
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
        private ModelRoot modelRoot;

        internal EXTStructuralMetadataRoot(ModelRoot modelRoot)
        {
            this.modelRoot = modelRoot;
            _propertyTables = new List<PropertyTable>();
            _propertyAttributes = new List<PropertyAttribute>();
            _propertyTextures = new List<PropertyTexture>();
        }

        internal void AddSchema(OneOf<StructuralMetadataSchema, Uri> schema)
        {
            schema.Switch(
                StructuralMetadataSchema => _schema = StructuralMetadataSchema,
                Uri => this.SchemaUri = Uri.ToString()
                );
        }

        internal List<PropertyTable> PropertyTables
        {
            get { return _propertyTables; }
            set { _propertyTables = value; }
        }

        internal string SchemaUri
        {
            get { return _schemaUri; }
            set { _schemaUri = value; }
        }

        internal List<PropertyAttribute> PropertyAttributes
        {
            get { return _propertyAttributes; }
            set { _propertyAttributes = value; }
        }

        internal StructuralMetadataSchema Schema
        {
            get { return _schema; }
            set { _schema = value; }
        }

        internal List<PropertyTexture> PropertyTextures
        {
            get { return _propertyTextures; }
            set { _propertyTextures = value; }
        }

        protected override void OnValidateReferences(ValidationContext validate)
        {
            foreach (var propertyTexture in PropertyTextures)
            {
                foreach (var propertyTextureProperty in propertyTexture.Properties)
                {
                    var textureId = propertyTextureProperty.Value._LogicalTextureIndex;
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
            }

            foreach (var propertyTexture in PropertyTextures)
            {
                foreach (var propertyTextureProperty in propertyTexture.Properties)
                {
                    var texCoord = propertyTextureProperty.Value.TextureCoordinate;
                    var channels = propertyTextureProperty.Value.Channels;
                    var index = propertyTextureProperty.Value._LogicalTextureIndex;
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
    }

    public partial class PropertyTexture
    {
        public PropertyTexture()
        {
            _properties = new Dictionary<string, PropertyTextureProperty>();
        }

        public string Class
        {
            get { return _class; }
            set { _class = value; }
        }

        public Dictionary<string, PropertyTextureProperty> Properties
        {
            get { return _properties; }
            set { _properties = value; }
        }
    }

    public partial class PropertyTextureProperty
    {
        public PropertyTextureProperty()
        {
            _channels = new List<int>();
        }

        public List<int> Channels
        {
            get { return _channels; }
            set { _channels = value; }
        }
    }

    public partial class PropertyAttribute
    {
        public PropertyAttribute()
        {
            _properties = new Dictionary<string, PropertyAttributeProperty>();
        }
        public string Class
        {
            get { return _class; }
            set
            {
                _class = value;
            }
        }

        public Dictionary<string, PropertyAttributeProperty> Properties
        {
            get { return _properties; }
            set
            {
                _properties = value;
            }
        }

    }

    public partial class PropertyAttributeProperty
    {
        public string Attribute
        {
            get { return _attribute; }
            set
            {
                _attribute = value;
            }
        }
    }

    public partial class StructuralMetadataSchema
    {
        public StructuralMetadataSchema()
        {
            _classes = new Dictionary<string, StructuralMetadataClass>();
            _enums = new Dictionary<string, StructuralMetadataEnum>();
        }

        public Dictionary<string, StructuralMetadataClass> Classes
        {
            get { return _classes; }
            set
            {
                _classes = value;
            }
        }

        public string Id
        {
            get { return _id; }
            set
            {
                _id = value;
            }
        }

        public string Version
        {
            get { return _version; }
            set
            {
                _version = value;
            }
        }

        public string Name
        {
            get { return _name; }
            set
            {
                _name = value;
            }
        }

        public string Description
        {
            get { return _description; }
            set
            {
                _description = value;
            }
        }

        public Dictionary<string, StructuralMetadataEnum> Enums
        {
            get { return _enums; }
            set
            {
                _enums = value;
            }
        }
    }

    public partial class StructuralMetadataEnum
    {
        public StructuralMetadataEnum()
        {
            _values = new List<EnumValue>();
        }
        public string Name
        {
            get { return _name; }
            set
            {
                _name = value;
            }
        }
        public string Description
        {
            get { return _description; }
            set
            {
                _description = value;
            }
        }
        public List<EnumValue> Values
        {
            get { return _values; }
            set
            {
                _values = value;
            }
        }
    }

    public partial class EnumValue
    {
        public string Name
        {
            get { return _name; }
            set
            {
                _name = value;
            }
        }
        public int Value
        {
            get { return _value; }
            set
            {
                _value = value;
            }
        }
    }

    public partial class StructuralMetadataClass
    {
        public StructuralMetadataClass()
        {
            _properties = new Dictionary<string, ClassProperty>();
        }

        public Dictionary<string, ClassProperty> Properties
        {
            get { return _properties; }
            set
            {
                _properties = value;
            }
        }

        public string Name
        {
            get { return _name; }
            set
            {
                if (value == null) { _name = null; return; }
                _name = value;
            }
        }

        public string Description
        {
            get { return _description; }
            set
            {
                _description = value;
            }
        }

    }

    public partial class ClassProperty
    {
        public string Name
        {
            get { return _name; }
            set
            {
                _name = value;
            }
        }

        public string Description
        {
            get { return _description; }
            set
            {
                _description = value;
            }
        }

        public ElementType Type
        {
            get { return _type; }
            set
            {
                _type = value;
            }
        }

        public string EnumType
        {
            get { return _enumType; }
            set
            {
                _enumType = value;
            }
        }

        public DataType? ComponentType
        {
            get { return _componentType; }
            set
            {
                _componentType = value;
            }
        }

        public bool? Required
        {
            get { return _required; }
            set
            {
                _required = value;
            }
        }

        public bool? Normalized
        {
            get { return _normalized; }
            set
            {
                _normalized = value;
            }
        }

        public bool? Array
        {
            get { return _array; }
            set
            {
                _array = value;
            }

        }

        public int? Count
        {
            get { return _count; }
            set
            {
                _count = value;
            }
        }
    }

    public partial class PropertyTable
    {
        public PropertyTable()
        {
            _properties = new Dictionary<string, PropertyTableProperty>();
        }
        public PropertyTable(string Class, int Count, string Name = "") : this()
        {
            _class = Class;
            _count = Count;
            _name = Name;
        }

        public string Name
        {
            get { return _name; }
            set
            {
                _name = value;
            }
        }

        public string Class
        {
            get { return _class; }
            set
            {
                _class = value;
            }
        }

        public int Count
        {
            get { return _count; }
            set
            {
                _count = value;
            }
        }

        public Dictionary<string, PropertyTableProperty> Properties
        {
            get { return _properties; }
            set
            {
                _properties = value;
            }
        }
    }

    public partial class PropertyTableProperty
    {
        public int Values
        {
            get { return _values; }
            set { _values = value; }
        }

        public int? ArrayOffsets
        {
            get { return _arrayOffsets; }
            set
            {
                _arrayOffsets = value;
            }
        }

        public int? StringOffsets
        {
            get { return _stringOffsets; }
            set
            {
                _stringOffsets = value;
            }
        }
    }
}

