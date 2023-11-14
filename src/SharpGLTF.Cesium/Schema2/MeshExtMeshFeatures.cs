using SharpGLTF.Validation;
using System.Collections.Generic;
using System.Linq;

namespace SharpGLTF.Schema2
{
    public partial class MeshExtMeshFeatures
    {
        private MeshPrimitive _meshPrimitive;

        internal MeshExtMeshFeatures(MeshPrimitive meshPrimitive)
        {
            _meshPrimitive = meshPrimitive;
            _featureIds = new List<MeshExtMeshFeatureID>();
        }


        public List<MeshExtMeshFeatureID> FeatureIds
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
            var extMeshFeatures = (MeshExtMeshFeatures)_meshPrimitive.Extensions.FirstOrDefault();

            validate.NotNull(nameof(FeatureIds), extMeshFeatures.FeatureIds);
            validate.IsTrue(nameof(FeatureIds), extMeshFeatures.FeatureIds.Count > 0, "FeatureIds has items");

            base.OnValidateContent(validate);
        }
    }

    public partial class MeshExtMeshFeatureID
    {
        public MeshExtMeshFeatureID(int featureCount, int? attribute = null, int? propertyTable = null, string label = null)
        {
            _featureCount = featureCount;
            _attribute = attribute == null ? 0 : attribute;
            _label = label;
            _propertyTable = propertyTable == null ? 0 : propertyTable;
        }
    }

    public static class ExtMeshFeatures
    {
        public static void SetFeatureIds(this MeshPrimitive primitive, List<MeshExtMeshFeatureID> list)
        {
            if (list == null) { primitive.RemoveExtensions<MeshExtMeshFeatures>(); return; }

            var ext = primitive.UseExtension<MeshExtMeshFeatures>();
            ext.FeatureIds = list;
        }
    }

}