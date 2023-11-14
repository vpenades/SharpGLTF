using SharpGLTF.Validation;
using System.Collections.Generic;
using System.Linq;

namespace SharpGLTF.Schema2
{
    public partial class MeshExtInstanceFeatures
    {
        private Node  _node;
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
            var extInstanceFeatures = (MeshExtInstanceFeatures)_node.Extensions.FirstOrDefault();

            validate.NotNull(nameof(FeatureIds), extInstanceFeatures.FeatureIds);
            validate.IsTrue(nameof(FeatureIds), extInstanceFeatures.FeatureIds.Count > 0, "Instance FeatureIds has items");

            base.OnValidateContent(validate);
        }
    }

    public partial class MeshExtInstanceFeatureID
    {
        public MeshExtInstanceFeatureID(int featureCount, int? attribute = null, int? propertyTable = null, string label = null, int? nullFeatureId = null)
        {
            _featureCount = featureCount;
            _attribute = attribute;
            _label = label;
            _propertyTable = propertyTable;
            _nullFeatureId = nullFeatureId;
        }
    }

    public static class ExtInstanceFeatures
    {
        public static void SetFeatureIds(this Node node, List<MeshExtInstanceFeatureID> list)
        {
            if (list == null) { node.RemoveExtensions<MeshExtInstanceFeatures>(); return; }

            var ext = node.UseExtension<MeshExtInstanceFeatures>();
            ext.FeatureIds = list;
        }
    }
}
