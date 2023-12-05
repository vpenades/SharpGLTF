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

        protected override void OnValidateContent(ValidationContext validate)
        {
        }
    }

    public partial class PropertyTable
    {
        public PropertyTable()
        {
            _properties = new Dictionary<string, PropertyTableProperty>();
        }
        public PropertyTable(string PropertyTableName, int NumberOfFeatures): this()
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
        public int Values {
            get { return _values; }
            set { _values = value; }
        }
    }
}