using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;

using SharpGLTF.Collections;
using SharpGLTF.Validation;

namespace SharpGLTF.Schema2
{
    using Tiles3D;

    partial class Tiles3DExtensions
    {
        internal static void ValidateFeatureIdReferences(this IMeshFeatureIDInfo featureId, ModelRoot root)
        {
            if (featureId.PropertyTableIndex.HasValue)
            {
                var metadataExtension = root.GetExtension<EXTStructuralMetadataRoot>();
                Guard.NotNull(metadataExtension, nameof(metadataExtension), "EXT_Structural_Metadata extension is not found.");
                Guard.NotNull(metadataExtension.PropertyTables[featureId.PropertyTableIndex.Value], nameof(featureId.PropertyTableIndex), $"Property table index {featureId.PropertyTableIndex.Value} does not exist");
            }
        }

        internal static void ValidateFeatureIdContent(this IMeshFeatureIDInfo featureId)
        {
            Guard.MustBeGreaterThanOrEqualTo(featureId.FeatureCount, 1, nameof(featureId.FeatureCount));

            if (featureId.NullFeatureId.HasValue)
            {
                Guard.MustBeGreaterThanOrEqualTo((int)featureId.NullFeatureId, 0, nameof(featureId.NullFeatureId));
            }
            if (featureId.Label != null)
            {
                var regex = "^[a-zA-Z_][a-zA-Z0-9_]*$";
                Guard.IsTrue(System.Text.RegularExpressions.Regex.IsMatch(featureId.Label, regex), nameof(featureId.Label));
            }
            if (featureId.Attribute.HasValue)
            {
                Guard.MustBeGreaterThanOrEqualTo((int)featureId.Attribute, 0, nameof(featureId.Attribute));
            }
            if (featureId.PropertyTableIndex.HasValue)
            {
                Guard.MustBeGreaterThanOrEqualTo((int)featureId.PropertyTableIndex, 0, nameof(featureId.PropertyTableIndex));
            }
        }

        /// <summary>
        /// Adds the FeatureIds to a MeshPrimitive
        /// </summary>        
        public static MeshExtInstanceFeatureID[] AddInstanceFeatureIds(this Node node, params IMeshFeatureIDInfo[] featureIds)
        {
            if (featureIds == null || featureIds.Length == 0) { node.RemoveExtensions<MeshExtInstanceFeatures>(); return Array.Empty<MeshExtInstanceFeatureID>(); }

            var ext = node.UseExtension<MeshExtInstanceFeatures>();

            var result = new MeshExtInstanceFeatureID[featureIds.Length];

            for (int i = 0; i < result.Length; ++i)
            {
                result[i] = ext.CreateFeatureID(featureIds[i]);
            }

            return result;
        }

        /// <summary>
        /// Adds the FeatureIds to a MeshPrimitive
        /// </summary>        
        public static MeshExtMeshFeatureID[] AddMeshFeatureIds(this MeshPrimitive primitive, params IMeshFeatureIDInfo[] fids)
        {
            if (fids == null || fids.Length == 0) { primitive.RemoveExtensions<MeshExtMeshFeatures>(); return Array.Empty<MeshExtMeshFeatureID>(); }

            var ext = primitive.UseExtension<MeshExtMeshFeatures>();

            var result = new MeshExtMeshFeatureID[fids.Length];

            for (int i = 0; i < result.Length; ++i)
            {
                var fid = fids[i];
                result[i] = ext.CreateFeatureID(fid);

                if (fid is FeatureIDBuilder builder && builder.Texture != null)
                {
                    var texture = result[i].UseTexture();
                    texture.Texture = builder.Texture;
                    if (builder.Channels != null)
                    {
                        texture.SetChannels(builder.Channels);
                    }
                }
            }

            return result;
        }
    }


    namespace Tiles3D
    {
        /// <remarks>
        /// This extension is attached to a <see cref="Schema2.Node"/> using <see cref="Tiles3DExtensions.AddInstanceFeatureIds(Node, IMeshFeatureIDInfo[])"/>
        /// </remarks>
        public partial class MeshExtInstanceFeatures
        {
            #region lifecycle

            internal MeshExtInstanceFeatures() { }

            internal MeshExtInstanceFeatures(Node node)
            {
                _node = node;
                _featureIds = new ChildrenList<MeshExtInstanceFeatureID, MeshExtInstanceFeatures>(this);
            }
            
            #endregion

            #region data

            private Node _node;

            #endregion

            #region properties

            public Node LogicalParent => _node;
            public IReadOnlyList<MeshExtInstanceFeatureID> FeatureIds => _featureIds;

            #endregion

            #region API

            public MeshExtInstanceFeatureID CreateFeatureID(IMeshFeatureIDInfo properties)
            {
                var instance = CreateFeatureID();

                instance.FeatureCount = properties.FeatureCount;
                instance.NullFeatureId = properties.NullFeatureId;
                instance.Label = properties.Label;
                instance.Attribute = properties.Attribute;
                instance.PropertyTableIndex = properties.PropertyTableIndex;

                return instance;
            }

            public MeshExtInstanceFeatureID CreateFeatureID()
            {
                var featureId = new MeshExtInstanceFeatureID();
                _featureIds.Add(featureId);
                return featureId;
            }

            #endregion

            #region validation

            protected override void OnValidateReferences(ValidationContext validate)
            {
                var extInstanceFeatures = _node.GetExtension<MeshExtInstanceFeatures>();
                validate.NotNull(nameof(extInstanceFeatures), extInstanceFeatures);
                var extMeshGpInstancing = _node.GetExtension<MeshGpuInstancing>();
                validate.NotNull(nameof(extMeshGpInstancing), extMeshGpInstancing);

                foreach (var instanceFeatureId in FeatureIds)
                {
                    if (instanceFeatureId.Attribute.HasValue)
                    {
                        var expectedVertexAttribute = $"_FEATURE_ID_{instanceFeatureId.Attribute}";
                        var gpuInstancing = _node.GetGpuInstancing();
                        var featureIdAccessors = gpuInstancing.GetAccessor(expectedVertexAttribute);
                        Guard.NotNull(featureIdAccessors, expectedVertexAttribute);
                    }

                    instanceFeatureId.ValidateFeatureIdReferences(_node.LogicalParent);
                }

                base.OnValidateReferences(validate);
            }

            protected override void OnValidateContent(ValidationContext validate)
            {
                var extInstanceFeatures = _node.GetExtension<MeshExtInstanceFeatures>();
                validate.NotNull(nameof(FeatureIds), extInstanceFeatures.FeatureIds);
                validate.IsTrue(nameof(FeatureIds), extInstanceFeatures.FeatureIds.Count > 0, "Instance FeatureIds has items");

                foreach (var instanceFeatureId in FeatureIds)
                {
                    instanceFeatureId.ValidateFeatureIdContent();
                }

                base.OnValidateContent(validate);
            }

            #endregion
        }

        /// <remarks>
        /// This extension is attached to a <see cref="Schema2.MeshPrimitive"/> using <see cref="Tiles3DExtensions.AddMeshFeatureIds(MeshPrimitive, ValueTuple{IMeshFeatureIDInfo, Texture, IReadOnlyList{int}}[])"/>
        /// </remarks>    
        public partial class MeshExtMeshFeatures
        {
            #region lifecycle

            internal MeshExtMeshFeatures(MeshPrimitive meshPrimitive)
            {
                _meshPrimitive = meshPrimitive;
                _featureIds = new ChildrenList<MeshExtMeshFeatureID, MeshExtMeshFeatures>(this);
            }
            
            #endregion

            #region data

            private MeshPrimitive _meshPrimitive;

            #endregion

            #region properties

            public MeshPrimitive LogicalParent => _meshPrimitive;

            public IReadOnlyList<MeshExtMeshFeatureID> FeatureIds => _featureIds;

            #endregion

            #region API

            public MeshExtMeshFeatureID CreateFeatureID(IMeshFeatureIDInfo properties)
            {
                var instance = CreateFeatureID();

                instance.FeatureCount = properties.FeatureCount;
                instance.NullFeatureId = properties.NullFeatureId;
                instance.Label = properties.Label;
                instance.Attribute = properties.Attribute;
                instance.PropertyTableIndex = properties.PropertyTableIndex;

                return instance;
            }

            public MeshExtMeshFeatureID CreateFeatureID()
            {
                var featureId = new MeshExtMeshFeatureID();
                _featureIds.Add(featureId);
                return featureId;
            }

            #endregion

            #region validation

            protected override void OnValidateReferences(ValidationContext validate)
            {
                foreach (var featureId in _featureIds)
                {
                    if (featureId.Attribute.HasValue)
                    {
                        var expectedVertexAttribute = $"_FEATURE_ID_{featureId.Attribute}";
                        var featureIdVertex = _meshPrimitive.GetVertexAccessor(expectedVertexAttribute);
                        Guard.NotNull(featureIdVertex, expectedVertexAttribute, $"The primitive should have custom vertex attribute {expectedVertexAttribute}.");
                    }

                    featureId.ValidateFeatureIdReferences(_meshPrimitive.LogicalParent.LogicalParent);

                    var texture = featureId.GetTexture();

                    if (texture != null)
                    {
                        var expectedTexCoordAttribute = $"TEXCOORD_{texture.TextureCoordinate}";
                        Guard.NotNull(_meshPrimitive.GetVertexAccessor(expectedTexCoordAttribute), expectedTexCoordAttribute, $"The primitive should have texture {expectedTexCoordAttribute}.");

                        var modelRoot = _meshPrimitive.LogicalParent.LogicalParent;
                        validate.IsNullOrIndex(nameof(texture), texture.TextureCoordinate, modelRoot.LogicalTextures);
                    }
                }

                base.OnValidateReferences(validate);
            }

            protected override void OnValidateContent(ValidationContext validate)
            {
                validate.NotNull(nameof(FeatureIds), _featureIds);
                validate.IsTrue(nameof(FeatureIds), _featureIds.Count > 0, "FeatureIds has items");

                foreach (var featureId in _featureIds)
                {
                    featureId.ValidateFeatureIdContent();

                    if (featureId.Attribute != null)
                    {
                        var expectedVertexAttribute = $"_FEATURE_ID_{featureId.Attribute}";
                        var vertex = _meshPrimitive.GetVertexAccessor(expectedVertexAttribute);
                        Guard.IsTrue(!vertex.Normalized, expectedVertexAttribute, $"The custom vertex attribute {expectedVertexAttribute} should not be normalized.");

                        var distinctFeatureIds = vertex.AsScalarArray().Distinct().ToList();

                        if (featureId.NullFeatureId.HasValue)
                        {
                            distinctFeatureIds.Remove(featureId.NullFeatureId.Value);
                        }

                        if (distinctFeatureIds.Min() < 0)
                        {
                            Guard.IsTrue(distinctFeatureIds.Min() >= 0, nameof(distinctFeatureIds), $"FeatureIds must be equal or larger than 0, but contains {distinctFeatureIds.Min()}");
                        }

                        var count = distinctFeatureIds.Count;

                        // FeatureCount must be greater or equal to the number of distinct feature ids
                        Guard.IsTrue(featureId.FeatureCount >= count, $"Mismatch between FeatureCount ({featureId.FeatureCount}) and Feature Attribute ({count})");

                        if (featureId.PropertyTableIndex.HasValue)
                        {
                            var root = _meshPrimitive.LogicalParent.LogicalParent;
                            var metadataExtension = root.GetExtension<EXTStructuralMetadataRoot>();
                            var propertyTable = metadataExtension.PropertyTables[featureId.PropertyTableIndex.Value];
                            var propertyCount = propertyTable.Count;
                            Guard.IsTrue(distinctFeatureIds.Max() <= propertyCount - 1, nameof(propertyCount),
                                $"The feature ID refers to a property table with {propertyCount} rows, so the feature IDs must be in the range [0,{propertyCount-1}], but the feature ID attribute contains values [{distinctFeatureIds.Min()},{distinctFeatureIds.Max()}]");
                        }


                    }
                    var texture = featureId.GetTexture();
                    if (texture != null)
                    {
                        var expectedTexCoordAttribute = $"TEXCOORD_{texture.TextureCoordinate}";
                        var vertex = _meshPrimitive.GetVertexAccessor(expectedTexCoordAttribute);
                        var distinctFeatureIds = vertex.AsVector2Array().Count;

                        Guard.IsTrue(featureId.FeatureCount == distinctFeatureIds, $"Mismatch between FeatureCount ({featureId.FeatureCount}) and Feature Texture ({distinctFeatureIds})");

                        var modelRoot = _meshPrimitive.LogicalParent.LogicalParent;

                        var samplers = modelRoot.LogicalTextureSamplers;
                        foreach (var sampler in samplers)
                        {
                            Guard.IsTrue(sampler.MagFilter == TextureInterpolationFilter.NEAREST, $"Texture magnification filter must be 9728 (NEAREST) but is set to {sampler.MagFilter}");
                            Guard.IsTrue(sampler.MinFilter == TextureMipMapFilter.NEAREST, $"Texture minification filtering must be 9728 (NEAREST) but is set to {sampler.MinFilter}");
                        }

                        // check on channels as workaround
                        // better solution: read the channels of the used texture using image library
                        var logicalTexture = modelRoot.LogicalTextures[texture.TextureCoordinate];
                        // var image = logicalTexture.PrimaryImage;
                        var channels = texture.GetChannels();

                        // chack that the length of channels list is maximum 4
                        Guard.IsTrue(channels.Count <= 4, $"The number of channels must be maximum 4 but is {channels.Count}");

                        // check that value in channels is minimum 0 and maximum 3
                        foreach (var channel in channels)
                        {
                            Guard.IsTrue(channel >= 0 && channel <= 3, $"Channel value must be between 0 and 3 but has channel {channel}");
                        }
                    }
                }

                base.OnValidateContent(validate);
            }

            #endregion
        }

    }
}
