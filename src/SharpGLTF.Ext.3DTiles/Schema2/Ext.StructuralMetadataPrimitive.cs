using System.Collections.Generic;

using SharpGLTF.Validation;

namespace SharpGLTF.Schema2.Tiles3D
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
            get => _propertyTextures;
            set => _propertyTextures = value;
        }

        public List<int> PropertyAttributes
        {
            get => _propertyAttributes;
            set => _propertyAttributes = value;
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

    partial class Tiles3DExtensions
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