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
            Guard.MustBeGreaterThanOrEqualTo((int)featureId.FeatureCount, 1, nameof(featureId.FeatureCount));

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
        public static MeshExtMeshFeatureID[] AddMeshFeatureIds(this MeshPrimitive primitive, params IMeshFeatureIDInfo []fids)
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
                    var featureIdBuilder = (FeatureIDBuilder)fid;
                    featureIdBuilder.Texture = builder.Texture;
                    featureIdBuilder.Channels = builder.Channels;
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

            protected override IEnumerable<ExtraProperties> GetLogicalChildren()
            {
                return base.GetLogicalChildren().Concat(_featureIds);
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

            protected override IEnumerable<ExtraProperties> GetLogicalChildren()
            {
                return base.GetLogicalChildren().Concat(_featureIds);
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
                        Guard.NotNull(_meshPrimitive.GetVertexAccessor(expectedVertexAttribute), expectedVertexAttribute);
                    }

                    featureId.ValidateFeatureIdReferences(_meshPrimitive.LogicalParent.LogicalParent);

                    var texture = featureId.GetTexture();

                    if (texture != null)
                    {
                        var expectedTexCoordAttribute = $"TEXCOORD_{texture.TextureCoordinate}";
                        Guard.NotNull(_meshPrimitive.GetVertexAccessor(expectedTexCoordAttribute), expectedTexCoordAttribute);

                        var modelRoot = _meshPrimitive.LogicalParent.LogicalParent;
                        validate.IsNullOrIndex(nameof(texture), texture.TextureCoordinate, modelRoot.LogicalTextures);
                    }
                }

                base.OnValidateReferences(validate);
            }

            protected override void OnValidateContent(ValidationContext validate)
            {
                var extMeshFeatures = _meshPrimitive.Extensions.Where(item => item is MeshExtMeshFeatures).FirstOrDefault();
                validate.NotNull(nameof(extMeshFeatures), extMeshFeatures);
                validate.NotNull(nameof(FeatureIds), _featureIds);
                validate.IsTrue(nameof(FeatureIds), _featureIds.Count > 0, "FeatureIds has items");

                foreach (var featureId in _featureIds)
                {
                    featureId.ValidateFeatureIdContent();
                }

                base.OnValidateContent(validate);
            }

            #endregion
        }

    }    
}
