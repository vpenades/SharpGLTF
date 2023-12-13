using SharpGLTF.Validation;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SharpGLTF.Schema2
{
    public static class ExtStructuralMetadata
    {
        // sample see https://github.com/CesiumGS/3d-tiles-samples/blob/main/glTF/EXT_structural_metadata/SimplePropertyTexture/SimplePropertyTexture.gltf
        public static void SetPropertyTexture(
            this ModelRoot modelRoot,
            PropertyTexture propertyTexture,
            StructuralMetadataSchema schema = null,
            Uri schemaUri = null
            )
        {
            if (propertyTexture == null) { modelRoot.RemoveExtensions<EXTStructuralMetaDataRoot>(); return; }

            var ext = modelRoot.UseExtension<EXTStructuralMetaDataRoot>();
            ext.PropertyTextures.Clear();
            ext.PropertyTextures.Add(propertyTexture);
            ext.AddSchema(schema, schemaUri);
        }

        public static void SetPropertyAttribute(
            this ModelRoot modelRoot,
            PropertyAttribute propertyAttribute,
            StructuralMetadataSchema schema = null,
            Uri schemaUri = null)
        {
            if (propertyAttribute == null) { modelRoot.RemoveExtensions<EXTStructuralMetaDataRoot>(); return; }

            var ext = modelRoot.UseExtension<EXTStructuralMetaDataRoot>();
            ext.PropertyAttributes.Clear();
            ext.PropertyAttributes.Add(propertyAttribute);
            ext.AddSchema(schema, schemaUri);
        }

        public static void SetPropertyTable(
            this ModelRoot modelRoot,
            Dictionary<string, List<int>> attributes,
            string name = "PropertyTable",
            StructuralMetadataSchema schema = null,
            Uri schemaUri = null
            )
        {
            if (attributes == null) { modelRoot.RemoveExtensions<EXTStructuralMetaDataRoot>(); return; }

            var ext = modelRoot.UseExtension<EXTStructuralMetaDataRoot>();
            ext.PropertyTables.Clear();
            ext.PropertyTables.Add(GetPropertyTable(modelRoot, attributes, name, schema));
            ext.AddSchema(schema, schemaUri);
        }


        private static PropertyTable GetPropertyTable(
            ModelRoot modelRoot,
            Dictionary<string, List<int>> attributes,
            string name = "PropertyTable",
            StructuralMetadataSchema schema = null
            // todo: use? Uri schemaUri = null
            )
        {
            var propertyTable = new PropertyTable(name, attributes.FirstOrDefault().Value.Count);

            var firstClass = schema.Classes.FirstOrDefault().Value;

            foreach (var property in firstClass.Properties)
            {
                var id = property.Key;
                var type = property.Value.Type;

                // Todo check type, for example string
                var attribute = attributes[id];
                var list = attribute.ConvertAll(x => (int)x);

                byte[] bytes = BinaryTable.GetBytes(list);
                var bufferView = modelRoot.UseBufferView(bytes);
                int logicalIndex = bufferView.LogicalIndex;
                var propertyTableProperty = new PropertyTableProperty();
                propertyTableProperty.Values = logicalIndex;
                propertyTable.Properties[id] = propertyTableProperty;
            }

            return propertyTable;
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

        internal void AddSchema(StructuralMetadataSchema schema, Uri schemaUri)
        {
            if (schema != null)
            {
                Schema = schema;
            }

            if (schemaUri != null)
            {
                SchemaUri = schemaUri.ToString();
            }
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
            // Schema or SchemaUri must be defined
            Guard.IsTrue(Schema != null || SchemaUri != null, "Schema/SchemaUri", "Schema or SchemaUri must be defined");

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
    }

    public partial class PropertyTable
    {
        public PropertyTable()
        {
            _properties = new Dictionary<string, PropertyTableProperty>();
        }
        public PropertyTable(string Class, int Count) : this()
        {
            _class = Class;
            _count = Count;
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
    }
}

