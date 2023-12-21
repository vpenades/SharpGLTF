using SharpGLTF.Validation;
using System.Collections.Generic;

namespace SharpGLTF.Schema2
{
    partial class ExtStructuralMetadataMeshPrimitive
    {
        internal ExtStructuralMetadataMeshPrimitive(MeshPrimitive meshPrimitive)
        {
            this.meshPrimitive = meshPrimitive;
            _propertyTextures = new List<int>();
            _propertyAttributes = new List<int>();
        }

        private MeshPrimitive meshPrimitive;

        public List<int> PropertyTextures
        {
            get
            {
                return _propertyTextures;
            }
            set
            {
                if (value == null) { _propertyTextures = null; return; }
                _propertyTextures = value;
            }
        }

        public List<int> PropertyAttributes
        {
            get
            {
                return _propertyAttributes;
            }
            set
            {
                if (value == null) { _propertyAttributes = null; return; }
                _propertyAttributes = value;
            }
        }

        protected override void OnValidateReferences(ValidationContext validate)
        {
            foreach (var propertyTexture in PropertyTextures)
            {
                var propertyTextures = meshPrimitive.LogicalParent.LogicalParent.GetExtension<EXTStructuralMetadataRoot>().PropertyTextures;
                validate.IsNullOrIndex(nameof(propertyTexture), propertyTexture, propertyTextures);
            }

            foreach (var propertyAttribute in PropertyAttributes)
            {
                var propertyAttributes = meshPrimitive.LogicalParent.LogicalParent.GetExtension<EXTStructuralMetadataRoot>().PropertyAttributes;
                validate.IsNullOrIndex(nameof(propertyAttribute), propertyAttribute, propertyAttributes);
            }

            base.OnValidateReferences(validate);
        }

        protected override void OnValidateContent(ValidationContext result)
        {
            base.OnValidateContent(result);
        }
    }

    partial class CesiumExtensions
    {
        public static void SetPropertyTextures(this MeshPrimitive primitive, List<int> propertyTextures)
        {
            if (propertyTextures == null) { primitive.RemoveExtensions<ExtStructuralMetadataMeshPrimitive>(); return; }

            var ext = primitive.UseExtension<ExtStructuralMetadataMeshPrimitive>();
            ext.PropertyTextures = propertyTextures;
        }

        public static void SetPropertyAttributes(this MeshPrimitive primitive, List<int> propertyAttributes)
        {
            if (propertyAttributes == null) { primitive.RemoveExtensions<ExtStructuralMetadataMeshPrimitive>(); return; }

            var ext = primitive.UseExtension<ExtStructuralMetadataMeshPrimitive>();
            ext.PropertyAttributes = propertyAttributes;
        }
    }
}