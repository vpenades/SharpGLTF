using SharpGLTF.Validation;
using System.Collections.Generic;
using System.Linq;

namespace SharpGLTF.Schema2
{
    public partial class MeshExtInstanceFeatures
    {
        private Node _node;
        internal MeshExtInstanceFeatures(Node node)
        {
            _node = node;
            _featureIds = new List<MeshExtInstanceFeatureID>();
        }

        public List<MeshExtInstanceFeatureID> FeatureIds
        {
            get
            {
                return _featureIds;
            }
            set
            {
                if (value == null) { _featureIds = null; return; }

                _featureIds = value;
            }
        }

        protected override void OnValidateContent(ValidationContext validate)
        {
            var extInstanceFeatures = (MeshExtInstanceFeatures)_node.Extensions.Where(item => item is MeshExtInstanceFeatures).FirstOrDefault();

            validate.NotNull(nameof(FeatureIds), extInstanceFeatures.FeatureIds);
            validate.IsTrue(nameof(FeatureIds), extInstanceFeatures.FeatureIds.Count > 0, "Instance FeatureIds has items");

            base.OnValidateContent(validate);
        }
    }

    public partial class MeshExtInstanceFeatureID
    {
        public MeshExtInstanceFeatureID()
        {
        }

        public MeshExtInstanceFeatureID(int featureCount, int? attribute = null, int? propertyTable = null, string label = null, int? nullFeatureId = null)
        {
            _featureCount = featureCount;
            _attribute = attribute;
            _label = label;
            _propertyTable = propertyTable;
            _nullFeatureId = nullFeatureId;
        }

        /// <summary>
        /// The number of unique features in the attribute
        /// </summary>
        public int FeatureCount { get => _featureCount; }

        /// <summary>
        /// An attribute containing feature IDs. When this is omitted, then the feature IDs are assigned to the GPU instances by their index.
        /// </summary>
        public int? Attribute { get => _attribute; }

        /// <summary>
        /// A label assigned to this feature ID set
        /// </summary>
        public string Label { get => _label; }

        /// <summary>
        /// The index of the property table containing per-feature property values. Only applicable when using the `EXT_structural_metadata` extension.
        /// </summary>
        public int? PropertyTable { get => _propertyTable; }

        /// <summary>
        /// A value that indicates that no feature is associated with this instance
        /// </summary>
        public int? NullFeatureId { get => _nullFeatureId; }
    }

    public static class ExtInstanceFeatures
    {
        /// <summary>
        /// Set the instance feature ids for this node.
        /// </summary>
        /// <param name="node"></param>
        /// <param name="instanceFeatureIds"></param>
        public static void SetFeatureIds(this Node node, List<MeshExtInstanceFeatureID> instanceFeatureIds)
        {
            if (instanceFeatureIds == null) { node.RemoveExtensions<MeshExtInstanceFeatures>(); return; }

            Guard.NotNullOrEmpty(instanceFeatureIds, nameof(instanceFeatureIds));

            foreach (var instanceFeatureId in instanceFeatureIds)
            {
                ValidateInstanceFeatureId(node, instanceFeatureId);
            };

            var ext = node.UseExtension<MeshExtInstanceFeatures>();
            ext.FeatureIds = instanceFeatureIds;
        }

        private static void ValidateInstanceFeatureId(Node node, MeshExtInstanceFeatureID item)
        {
            Guard.MustBeGreaterThanOrEqualTo((int)item.FeatureCount, 1, nameof(item.FeatureCount));

            if (item.NullFeatureId.HasValue)
            {
                Guard.MustBeGreaterThanOrEqualTo((int)item.NullFeatureId, 0, nameof(item.NullFeatureId));
            }
            if (item.Label != null)
            {
                var regex = "^[a-zA-Z_][a-zA-Z0-9_]*$";
                Guard.IsTrue(System.Text.RegularExpressions.Regex.IsMatch(item.Label, regex), nameof(item.Label));
            }
            if (item.Attribute.HasValue)
            {
                Guard.MustBeGreaterThanOrEqualTo((int)item.Attribute, 0, nameof(item.Attribute));
                // todo: check attribute
                // Guard that the custom vertex attribute (_FEATURE_ID_{attribute}) exists when FeatureID has attribute set
                // var expectedVertexAttribute = $"_FEATURE_ID_{item.Attribute}";
                // Guard.NotNull(primitive.GetVertexAccessor(expectedVertexAttribute), expectedVertexAttribute);
            }
            if (item.PropertyTable.HasValue)
            {
                Guard.MustBeGreaterThanOrEqualTo((int)item.PropertyTable, 0, nameof(item.PropertyTable));
            }
        }
    }
}
