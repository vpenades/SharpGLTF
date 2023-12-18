using OneOf;
using SharpGLTF.Validation;
using System;
using System.Collections.Generic;
using System.Globalization;

namespace SharpGLTF.Schema2
{
    public static class ExtStructuralMetadata
    {
        // sample see https://github.com/CesiumGS/3d-tiles-samples/blob/main/glTF/EXT_structural_metadata/SimplePropertyTexture/SimplePropertyTexture.gltf
        public static void SetPropertyTexture(
            this ModelRoot modelRoot,
            PropertyTexture propertyTexture,
            OneOf<StructuralMetadataSchema, Uri> schema
            )
        {
            if (propertyTexture == null) { modelRoot.RemoveExtensions<EXTStructuralMetaDataRoot>(); return; }

            var ext = modelRoot.UseExtension<EXTStructuralMetaDataRoot>();
            ext.PropertyTextures.Clear();
            ext.PropertyTextures.Add(propertyTexture);
            ext.AddSchema(schema);
        }

        public static void SetPropertyAttribute(
            this ModelRoot modelRoot,
            PropertyAttribute propertyAttribute,
            OneOf<StructuralMetadataSchema, Uri> schema)
        {
            if (propertyAttribute == null) { modelRoot.RemoveExtensions<EXTStructuralMetaDataRoot>(); return; }

            var ext = modelRoot.UseExtension<EXTStructuralMetaDataRoot>();
            ext.PropertyAttributes.Clear();
            ext.PropertyAttributes.Add(propertyAttribute);
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
            if (propertyTables == null || propertyTables.Count == 0) { modelRoot.RemoveExtensions<EXTStructuralMetaDataRoot>(); return; }

            // todo add check if propertyTable.Class is in schema.Classes
            foreach (var propertyTable in propertyTables)
            {
                Guard.IsTrue(propertyTable.Class != null, nameof(propertyTable.Class), "Class must be defined");
                Guard.IsTrue(propertyTable.Count > 0, nameof(propertyTable.Count), "Count must be greater than 0");
                Guard.IsTrue(propertyTable.Properties.Count > 0, nameof(propertyTable.Properties), "Properties must be defined");

                schema.Switch(
                    StructuralMetadataSchema =>
                        CheckConsistency(StructuralMetadataSchema, propertyTable),
                    Uri =>
                    {
                        // do not check here, because schema is not loaded
                    }
                    );
            }

            var ext = modelRoot.UseExtension<EXTStructuralMetaDataRoot>();
            ext.PropertyTables = propertyTables;
            ext.AddSchema(schema);
        }

        private static void CheckConsistency(StructuralMetadataSchema StructuralMetadataSchema, PropertyTable propertyTable)
        {
            Guard.IsTrue(StructuralMetadataSchema.Classes.ContainsKey(propertyTable.Class), nameof(propertyTable.Class), $"Class {propertyTable.Class} must be defined in schema");
            foreach (var property in propertyTable.Properties)
            {
                Guard.IsTrue(StructuralMetadataSchema.Classes[propertyTable.Class].Properties.ContainsKey(property.Key), nameof(propertyTable.Properties), $"Property {property.Key} must be defined in schema");
            }
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

                if(typeof(T) == typeof(string))
                {
                    var stringValues = values.ConvertAll(x => x.ConvertAll(y => (string)Convert.ChangeType(y, typeof(string),CultureInfo.InvariantCulture)));
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
            var bytes = new List<byte>();
            foreach (var value in values)
            {
                var b = BinaryTable.GetBytes(value);
                bytes.AddRange(b);
            }
            var bufferView = model.UseBufferView(bytes.ToArray());
            int logicalIndex = bufferView.LogicalIndex;
            return logicalIndex;
        }
    }

    public partial class EXTStructuralMetaDataRoot
    {
        private ModelRoot modelRoot;

        internal EXTStructuralMetaDataRoot(ModelRoot modelRoot)
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

        protected override void OnValidateContent(ValidationContext result)
        {
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
        //public int Index
        //{
        //    get { return _index; }
        //    set { _index = value; }
        //}
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
            set { _class = value; }
        }

        public Dictionary<string, PropertyAttributeProperty> Properties
        {
            get { return _properties; }
            set { _properties = value; }
        }

    }

    public partial class PropertyAttributeProperty
    {
        public string Attribute
        {
            get { return _attribute; }
            set { _attribute = value; }
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
            set { _classes = value; }
        }

        public string Id
        {
            get { return _id; }
            set { _id = value; }
        }

        public string Version
        {
            get { return _version; }
            set { _version = value; }
        }

        public string Name
        {
            get { return _name; }
            set { _name = value; }
        }

        public string Description
        {
            get { return _description; }
            set { _description = value; }
        }

        public Dictionary<string, StructuralMetadataEnum> Enums
        {
            get { return _enums; }
            set { _enums = value; }
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
            set { _name = value; }
        }
        public string Description
        {
            get { return _description; }
            set { _description = value; }
        }
        public List<EnumValue> Values
        {
            get { return _values; }
            set { _values = value; }
        }
    }

    public partial class EnumValue
    {
        public string Name
        {
            get { return _name; }
            set { _name = value; }
        }
        public int Value
        {
            get { return _value; }
            set { _value = value; }
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
            set { _properties = value; }
        }

        public string Name
        {
            get { return _name; }
            set { _name = value; }
        }

        public string Description
        {
            get { return _description; }
            set { _description = value; }
        }

    }

    public partial class ClassProperty
    {
        public string Name
        {
            get { return _name; }
            set { _name = value; }
        }

        public string Description
        {
            get { return _description; }
            set { _description = value; }
        }

        public ElementType Type
        {
            get { return _type; }
            set { _type = value; }
        }

        public string EnumType
        {
            get { return _enumType; }
            set { _enumType = value; }
        }

        public DataType? ComponentType
        {
            get { return _componentType; }
            set { _componentType = value; }
        }

        public bool? Required
        {
            get { return _required; }
            set { _required = value; }
        }

        public bool? Normalized
        {
            get { return _normalized; }
            set { _normalized = value; }
        }

        // add array property
        public bool? Array
        {
            get { return _array; }
            set { _array = value; }

        }

        public int? Count
        {
            get { return _count; }
            set { _count = value; }
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
            set { _name = value; }
        }

        public string Class
        {
            get { return _class; }
            set { _class = value; }
        }

        public int Count
        {
            get { return _count; }
            set { _count = value; }
        }

        public Dictionary<string, PropertyTableProperty> Properties
        {
            get { return _properties; }
            set { _properties = value; }
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
            set { _arrayOffsets = value; }
        }

        public int? StringOffsets
        {
            get { return _stringOffsets; }
            set { _stringOffsets = value; }
        }
    }
}

