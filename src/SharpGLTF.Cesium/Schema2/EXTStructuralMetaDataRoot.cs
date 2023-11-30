using SharpGLTF.Validation;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SharpGLTF.Schema2
{
    public partial class EXTStructuralMetaDataRoot
    {
        internal EXTStructuralMetaDataRoot(ModelRoot modelRoot)
        {
            this.modelRoot = modelRoot;
        }

        private ModelRoot modelRoot;


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

    }

    partial class PropertyTable
    {
        public PropertyTable()
        {
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
        public static void AddMetadata<T>(this ModelRoot modelRoot, string fieldname, List<T> values)
        {
        }
    }
}