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
        }

        internal List<PropertyTable> PropertyTables
        {
            get { return _propertyTables; }
            set { if (value == null) { _propertyTables = null; return; } _propertyTables = value; }
        }

        internal StructuralMetadataSchema Schema
        {
            get { return _schema; }
            set { if (value == null) { _schema = null; return; } _schema = value; }
        }


        protected override void OnValidateContent(ValidationContext validate)
        {
        }
    }

    partial class StructuralMetadataSchema
    {
        public StructuralMetadataSchema()
        {
            _classes = new Dictionary<string, StructuralMetadataClass>();
        }

        public Dictionary<string, StructuralMetadataClass> Classes { get; set; }
    }

    partial class PropertyTable
    {
        public PropertyTable(string PropertyTableName, int numberOfFeatures)
        {
            _class = PropertyTableName;
            _count = numberOfFeatures;
            _properties = new Dictionary<string, PropertyTableProperty>();
        }
    }

    partial class PropertyTableProperty
    {
    }

    partial class StructuralMetadataClass
    {
        public StructuralMetadataClass()
        {
            _properties = new Dictionary<string, ClassProperty>();
        }
    }

    partial class ClassProperty
    {
    }

    public static class ExtStructuralMetadata
    {
        // Creates EXTStructuralMetaData with Schema and 1 PropertyTable
        public static void InitializeMetadataExtension(this ModelRoot modelRoot, string propertyTableName, int numberOfFeatures)
        {
            if (propertyTableName == null) { modelRoot.RemoveExtensions<EXTStructuralMetaDataRoot>(); return; }

            var ext = modelRoot.UseExtension<EXTStructuralMetaDataRoot>();

            var schema = GetInitialSchema(propertyTableName);
            ext.Schema = schema;
            var propertyTable = new PropertyTable(propertyTableName, numberOfFeatures);
            ext.PropertyTables = new List<PropertyTable>() { propertyTable };
        }

        public static void AddMetadata<T>(this ModelRoot modelRoot, string fieldname, List<T> values)
        {
        }

        private static StructuralMetadataSchema GetInitialSchema(string schemaName)
        {
            var structuralMetadataSchema = new StructuralMetadataSchema();
            var structuralMetadataClass = new StructuralMetadataClass();

            structuralMetadataSchema.Classes = new Dictionary<string, StructuralMetadataClass>
            {
            { schemaName , structuralMetadataClass }
            };

            return structuralMetadataSchema;
        }
    }
}