using System;
using System.Linq;
using System.Collections.Generic;

using SharpGLTF.Validation;

namespace SharpGLTF.Schema2
{
    using Tiles3D;

    partial class Tiles3DExtensions
    {
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
                Guard.NotNull(rootMetadata, nameof(rootMetadata), "EXT_Structural_Metadata extension missing in root");
                var propertyTextures = rootMetadata.PropertyTextures;

                // Scan textures
                foreach (var propertyTexture in _propertyTextures)
                {
                    validate.IsNullOrIndex(nameof(propertyTexture), propertyTexture, propertyTextures);

                    var schemaTexture = propertyTextures[propertyTexture];
                    var className = schemaTexture.ClassName;
                    foreach (var property in schemaTexture.Properties)
                    {
                        var textureCoordinate = property.Value.TextureCoordinate;
                        // Guard the meshprimitive has the texture coordinate attribute
                        var expectedVertexAttribute = "TEXCOORD_" + textureCoordinate;
                        Guard.NotNull(meshPrimitive.GetVertexAccessor(expectedVertexAttribute), nameof(textureCoordinate), $"The primitive should have texture coordinate attribute {textureCoordinate}.");

                        var texture = property.Value.Texture;
                        Guard.NotNull(texture, nameof(texture), $"The primitive should have texture {texture}.");
                    }
                }

                // scan attributes
                foreach (var propertyAttribute in _propertyAttributes)
                {
                    var propertyAttributes = rootMetadata.PropertyAttributes;
                    validate.IsNullOrIndex(nameof(propertyAttribute), propertyAttribute, propertyAttributes);

                    var schema = rootMetadata.Schema;
                    var schemaAttribute = propertyAttributes[propertyAttribute];

                    var className = schemaAttribute.ClassName;
                    Guard.NotNull(schema, nameof(schema), "EXT_Structural_Metadata extension missing schema");
                    schema.Classes.TryGetValue(className, out var classDefinition);
                    Guard.NotNull(classDefinition, nameof(classDefinition), $"EXT_Structural_Metadata extension missing class definition for {className}");

                    foreach (var property in schemaAttribute.Properties)
                    {
                        var expectedVertexAttribute = property.Value.Attribute;
                        Guard.NotNull(meshPrimitive.GetVertexAccessor(expectedVertexAttribute), expectedVertexAttribute, $"The primitive should have custom vertex attribute {expectedVertexAttribute}.");

                        var key = property.Key;

                        classDefinition.Properties.TryGetValue(key, out var propertyDefinition);
                        Guard.NotNull(propertyDefinition, nameof(propertyDefinition), $"EXT_Structural_Metadata extension missing property definition for {key}");

                        Guard.IsTrue(propertyDefinition.Array == false, nameof(propertyDefinition.Array), $"The property '{property.Key}' is an array, which is not supported for property attributes");

                        if (propertyDefinition.Type == ElementType.ENUM)
                        {
                            var enumType = propertyDefinition.EnumType;
                            // Get the enum from the schema
                            var enumDefinition = schema.Enums[enumType];
                            Guard.NotNull(enumDefinition, nameof(enumDefinition), $"EXT_Structural_Metadata extension missing enum definition for {enumType}");
                        }
                    }
                }

                base.OnValidateReferences(validate);
            }

            protected override void OnValidateContent(ValidationContext validate)
            {
                var rootMetadata = _GetModelRoot().GetExtension<EXTStructuralMetadataRoot>();
                var propertyTextures = rootMetadata.PropertyTextures;

                // Scan textures
                foreach (var propertyTexture in _propertyTextures)
                {
                    var schemaTexture = propertyTextures[propertyTexture];
                    var className = schemaTexture.ClassName;
                    foreach (var property in schemaTexture.Properties)
                    {
                        var textureCoordinate = property.Value.TextureCoordinate;
                        var expectedVertexAttribute = "TEXCOORD_" + textureCoordinate;
                        var vertex = meshPrimitive.GetVertexAccessor(expectedVertexAttribute);

                        var texture = property.Value.Texture;

                        var schemaProperty = rootMetadata.Schema.Classes[className].Properties[property.Key];

                        Guard.IsTrue(schemaProperty.Type != ElementType.STRING, nameof(schemaProperty.Type),
                            $"The property '{property.Key}' has the type 'STRING', which is not supported for property textures");


                        if (schemaProperty.Array)
                        {
                            Guard.IsTrue(schemaProperty.Count != null, nameof(schemaProperty.Array),
                                 $"The property '{property.Key}'  is a variable-length array, which is not supported for property textures");
                        }

                        // todo: check used values in texture against min, max (using scale and offset)
                        // var min = schemaProperty.Min;
                        // var max = schemaProperty.Max;
                        // var scale = schemaProperty.Scale;
                        // var offset = schemaProperty.Offset;

                        var channels = property.Value.Channels;
                        var elementCount = ComponentCount.ElementCountForType(schemaProperty.Type);
                        if (schemaProperty.ComponentType != null)
                        {
                            var componentByteSize = ComponentCount.ByteSizeForComponentType(schemaProperty.ComponentType);
                            var elementByteSize = elementCount * componentByteSize;
                            var totalByteSize = channels.Count * elementByteSize;
                            Guard.IsTrue(totalByteSize == channels.Count, nameof(totalByteSize),
                                $"The property '{property.Key}' has the component type {schemaProperty.ComponentType}, with a size of {componentByteSize} bytes, and the type {schemaProperty.Type} with {channels.Count} components, resulting in {totalByteSize} bytes per element, but the number of channels in the property texture property was {channels.Count}");
                        }
                    }
                }

                // scan attributes
                foreach (var propertyAttribute in _propertyAttributes)
                {
                    var propertyAttributes = rootMetadata.PropertyAttributes;
                    var schema = rootMetadata.Schema;
                    var schemaAttribute = propertyAttributes[propertyAttribute];

                    var className = schemaAttribute.ClassName;
                    schema.Classes.TryGetValue(className, out var classDefinition);

                    foreach (var property in schemaAttribute.Properties)
                    {
                        var expectedVertexAttribute = property.Value.Attribute;

                        // todo: check used values in attribute against min, max (using scale and offset)
                        var min = property.Value.Min;
                        var max = property.Value.Max;
                        var scale = property.Value.Scale;
                        var offset = property.Value.Offset;
                        // todo read values from accessor, get min, max
                        // var acc = property.Value.Attribute;
                        // var vertexAccessor = meshPrimitive.GetVertexAccessor(acc);
                        // var a = vertexAccessor.AsScalarArray();

                        var key = property.Key;

                        classDefinition.Properties.TryGetValue(key, out var propertyDefinition);

                        Guard.IsTrue(propertyDefinition.Array == false, nameof(propertyDefinition.Array), $"The property '{property.Key}' is an array, which is not supported for property attributes");

                        Guard.IsTrue(propertyDefinition.Type != ElementType.STRING, nameof(propertyDefinition.Type),
                                                       $"The property '{property.Key}' has the type 'STRING', which is not supported for property attributes");
                        if (propertyDefinition.Type == ElementType.SCALAR)
                        {
                            var allowedComponentTypes = new List<DataType?>()
                            {
                                DataType.INT8,DataType.UINT8,DataType.INT16,DataType.UINT16,DataType.FLOAT32,
                            };

                            Guard.IsTrue(allowedComponentTypes.Contains(propertyDefinition.ComponentType), nameof(propertyDefinition.ComponentType),
                                $"The property '{property.Key}' has the component type {propertyDefinition.ComponentType}, but the type must be one of INT8,UINT8,INT16,UINT16,FLOAT32 for property attribute");
                        }
                        else if (propertyDefinition.Type == ElementType.ENUM)
                        {
                            var enumType = propertyDefinition.EnumType;
                            // Get the enum from the schema
                            var enumDefinition = schema.Enums[enumType];
                            var valueType = enumDefinition.ValueType;
                            var allowedIntegerTypes = new List<IntegerType?>()
                            {
                                IntegerType.INT8,IntegerType.UINT8,IntegerType.INT16,IntegerType.UINT16
                            };
                            Guard.IsTrue(allowedIntegerTypes.Contains(valueType), nameof(valueType),
                                                               $"The enumeration '{property.Key}' has the value type {valueType}, but the type must be one of INT8,UINT8,INT16,UINT16 for property attribute");
                        }
                    }
                }

                base.OnValidateContent(validate);
            }

            #endregion
        }

    }
}