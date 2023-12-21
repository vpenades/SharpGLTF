using SharpGLTF.Validation;
using System.Collections.Generic;
using System.Linq;

namespace SharpGLTF.Schema2
{
    partial class MeshExtMeshFeatures
    {
        private MeshPrimitive _meshPrimitive;

        internal MeshExtMeshFeatures(MeshPrimitive meshPrimitive)
        {
            _meshPrimitive = meshPrimitive;
            _featureIds = new List<MeshExtMeshFeatureID>();
        }

        public List<MeshExtMeshFeatureID> FeatureIds
        {
            get => _featureIds;
            set
            {
                if (value == null) { _featureIds = null; return; }
                _featureIds = value;
            }
        }

        protected override void OnValidateReferences(ValidationContext validate)
        {
            foreach (var featureId in _featureIds)
            {
                if (featureId.Attribute.HasValue)
                {
                    var expectedVertexAttribute = $"_FEATURE_ID_{featureId.Attribute}";
                    Guard.NotNull(_meshPrimitive.GetVertexAccessor(expectedVertexAttribute), expectedVertexAttribute);
                }
                if (featureId.PropertyTable.HasValue)
                {
                    var metadataExtension = _meshPrimitive.LogicalParent.LogicalParent.GetExtension<EXTStructuralMetadataRoot>();
                    Guard.NotNull(metadataExtension, nameof(metadataExtension), "EXT_Structural_Metadata extension is not found.");
                    Guard.NotNull(metadataExtension.PropertyTables[featureId.PropertyTable.Value], nameof(featureId.PropertyTable), $"Property table index {featureId.PropertyTable.Value} does not exist");
                }
                if (featureId.Texture != null)
                {
                    var expectedTexCoordAttribute = $"TEXCOORD_{featureId.Texture.TextureCoordinate}";
                    Guard.NotNull(_meshPrimitive.GetVertexAccessor(expectedTexCoordAttribute), expectedTexCoordAttribute);

                    var modelRoot = _meshPrimitive.LogicalParent.LogicalParent;
                    validate.IsNullOrIndex(nameof(featureId.Texture), featureId.Texture.TextureCoordinate, modelRoot.LogicalTextures);
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
                if (featureId.PropertyTable.HasValue)
                {
                    Guard.MustBeGreaterThanOrEqualTo((int)featureId.PropertyTable, 0, nameof(featureId.PropertyTable));
                }
                if (featureId.Texture != null)
                {
                    Guard.MustBeGreaterThanOrEqualTo(featureId.Texture.TextureCoordinate, 0, nameof(featureId.Texture.TextureCoordinate));
                }
                base.OnValidateContent(validate);
            }
        }
    }

    public partial class MeshExtMeshFeatureIDTexture
    {
        public MeshExtMeshFeatureIDTexture()
        {
            _channels = new List<int>();
        }

        public MeshExtMeshFeatureIDTexture(List<int> channels, int? index = null, int? texCoord = null)
        {
            Guard.NotNullOrEmpty(channels, nameof(channels));
            Guard.MustBeGreaterThanOrEqualTo((int)index, 0, nameof(index));
            Guard.MustBeGreaterThanOrEqualTo((int)texCoord, 0, nameof(index));

            _channels = channels;
            if (index.HasValue) _LogicalTextureIndex = (int)index;
            if (texCoord.HasValue) TextureCoordinate = (int)texCoord;
        }

        public int Index { get => _LogicalTextureIndex; }
    }

    public partial class MeshExtMeshFeatureID
    {
        public MeshExtMeshFeatureID()
        {
        }
        public MeshExtMeshFeatureID(int featureCount, int? attribute = null, int? propertyTable = null, string label = null, int? nullFeatureId = null, MeshExtMeshFeatureIDTexture texture = null)
        {
            _featureCount = featureCount;
            _attribute = attribute;
            _label = label;
            _propertyTable = propertyTable;
            _nullFeatureId = nullFeatureId;
            _texture = texture;
        }

        /// <summary>
        /// The number of unique features in the attribute or texture.
        /// </summary>
        public int FeatureCount { get => _featureCount; }

        /// <summary>
        /// A value that indicates that no feature is associated with this vertex or texel.
        /// </summary>
        public int? NullFeatureId { get => _nullFeatureId; }

        /// <summary>
        /// An attribute containing feature IDs. When `attribute` and `texture` are omitted the 
        /// feature IDs are assigned to vertices by their index.
        /// </summary>
        public int? Attribute { get => _attribute; }

        /// <summary>
        /// A label assigned to this feature ID set. Labels must be alphanumeric identifiers 
        /// matching the regular expression `^[a-zA-Z_][a-zA-Z0-9_]*$`.
        /// </summary>
        public string Label { get => _label; }

        /// <summary>
        /// A texture containing feature IDs.
        /// </summary>
        public MeshExtMeshFeatureIDTexture Texture { get => _texture; }

        /// <summary>
        /// The index of the property table containing per-feature property values. Only applicable when using the `EXT_structural_metadata` extension.
        /// </summary>
        public int? PropertyTable { get => _propertyTable; }
    }

    public static class ExtMeshFeatures
    {
        public static void SetFeatureId(this MeshPrimitive primitive, MeshExtMeshFeatureID featureId)
        {
            primitive.SetFeatureIds(new List<MeshExtMeshFeatureID>() { featureId });
        }

        /// <summary>
        /// Set the FeatureIds for a MeshPrimitive
        /// </summary>
        /// <param name="primitive"></param>
        /// <param name="featureIds"></param>
        public static void SetFeatureIds(this MeshPrimitive primitive, List<MeshExtMeshFeatureID> featureIds)
        {
            if (featureIds == null || featureIds.Count == 0) { primitive.RemoveExtensions<MeshExtMeshFeatures>(); return; }

            var ext = primitive.UseExtension<MeshExtMeshFeatures>();
            ext.FeatureIds = featureIds;
        }

    }
}
