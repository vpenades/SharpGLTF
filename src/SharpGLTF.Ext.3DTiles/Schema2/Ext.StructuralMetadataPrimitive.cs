using System;
using System.Linq;
using System.Collections.Generic;

using SharpGLTF.Validation;

namespace SharpGLTF.Schema2
{
    using Tiles3D;

    partial class Tiles3DExtensions
    {
        // TODO: PropertyTexture is taken from a Schema, but it is possible the schema is an external file,
        // in which case we could not have a PopertyTexture, just a blind ID

        // Solution1: enforce loading the schema as part of the memory document
        // Solution2: allow the API to be OneOf<int,PropertyTexture>

        public static void AddPropertyTexture(this MeshPrimitive primitive, PropertyTexture texture)
        {
            var ext = primitive.UseExtension<ExtStructuralMetadataMeshPrimitive>();
            ext.AddTexture(texture);
        }

        public static void AddPropertyAttribute(this MeshPrimitive primitive, PropertyAttribute attribute)
        {
            var ext = primitive.UseExtension<ExtStructuralMetadataMeshPrimitive>();
            ext.AddAttribute(attribute);
        }
    }

    namespace Tiles3D
    {
        partial class ExtStructuralMetadataMeshPrimitive
        {
            #region lifecycle
            internal ExtStructuralMetadataMeshPrimitive(MeshPrimitive meshPrimitive)
            {
                this.meshPrimitive = meshPrimitive;
                _propertyTextures = new List<int>();
                _propertyAttributes = new List<int>();
            }

            #endregion

            #region data

            private MeshPrimitive meshPrimitive;

            #endregion

            #region properties

            private ModelRoot _GetModelRoot() => meshPrimitive.LogicalParent.LogicalParent;

            public int PropertyCount => _propertyTextures.Count;

            public int AttributeCount => _propertyAttributes.Count;

            #endregion

            #region API

            public PropertyTexture GetTexture(int index)
            {
                Guard.MustBeBetweenOrEqualTo(index, 0, _propertyTextures.Count - 1, nameof(index));

                var root = _GetModelRoot();
                var metadata = root.GetExtension<EXTStructuralMetadataRoot>();
                Guard.NotNull(metadata, nameof(index));

                return metadata.PropertyTextures[index];
            }

            public void AddTexture(PropertyTexture texture)
            {
                Guard.NotNull(texture, nameof(texture));
                
                var metadata = _GetModelRoot().UseExtension<EXTStructuralMetadataRoot>();
                var properties = metadata.PropertyTextures;
                Guard.IsTrue(properties.Contains(texture), nameof(texture));
                
                _propertyTextures.Add(texture.LogicalIndex);
            }

            public PropertyAttribute GetAttribute(int index)
            {
                Guard.MustBeBetweenOrEqualTo(index, 0, _propertyTextures.Count - 1, nameof(index));

                var root = _GetModelRoot();
                var metadata = root.GetExtension<EXTStructuralMetadataRoot>();
                Guard.NotNull(metadata, nameof(index));

                return metadata.PropertyAttributes[index];
            }

            public void AddAttribute(PropertyAttribute attribute)
            {
                Guard.NotNull(attribute, nameof(attribute));
                
                var metadata = _GetModelRoot().UseExtension<EXTStructuralMetadataRoot>();
                var properties = metadata.PropertyAttributes;
                Guard.IsTrue(properties.Contains(attribute), nameof(attribute));
                
                _propertyAttributes.Add(attribute.LogicalIndex);
            }

            #endregion

            #region validation

            protected override void OnValidateReferences(ValidationContext validate)
            {
                var rootMetadata = _GetModelRoot().GetExtension<EXTStructuralMetadataRoot>();

                foreach (var propertyTexture in _propertyTextures)
                {
                    var propertyTextures = rootMetadata.PropertyTextures;
                    validate.IsNullOrIndex(nameof(propertyTexture), propertyTexture, propertyTextures);
                }

                foreach (var propertyAttribute in _propertyAttributes)
                {
                    var propertyAttributes = rootMetadata.PropertyAttributes;
                    validate.IsNullOrIndex(nameof(propertyAttribute), propertyAttribute, propertyAttributes);
                }

                base.OnValidateReferences(validate);
            }            

            #endregion
        }
    }
}