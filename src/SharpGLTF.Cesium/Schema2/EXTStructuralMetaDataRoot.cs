using SharpGLTF.Validation;
using System.Collections.Generic;

namespace SharpGLTF.Schema2
{
    public partial class EXTStructuralMetaDataRoot
    {
        private ModelRoot modelRoot;

        internal EXTStructuralMetaDataRoot(ModelRoot modelRoot)
        {
            this.modelRoot = modelRoot;
            _propertyTables = new List<PropertyTable>();
        }

        public List<PropertyTable> PropertyTables
        {
            get { return _propertyTables; }
            set { _propertyTables = value; }
        }

        public StructuralMetadataSchema Schema
        {
            get { return _schema; }
            set { _schema = value; }
        }


        protected override void OnValidateContent(ValidationContext validate)
        {
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

        // required property
        public bool? Required
        {
            get { return _required; }
            set { _required = value; }
        }
    }

    public partial class PropertyTable
    {
        public PropertyTable()
        {
            _properties = new Dictionary<string, PropertyTableProperty>();
        }
        public PropertyTable(string PropertyTableName, int NumberOfFeatures) : this()
        {
            _class = PropertyTableName;
            _count = NumberOfFeatures;
        }

        public string PropertyTableName
        {
            get { return _class; }
            set { _class = value; }
        }

        public int NumberOfFeatures
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

