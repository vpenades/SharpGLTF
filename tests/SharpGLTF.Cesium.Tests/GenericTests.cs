using NUnit.Framework;
using SharpGLTF.Schema2;
using SharpGLTF.Schema2.Tiles3D;
using SharpGLTF.Validation;
using System;
using System.IO;

namespace SharpGLTF
{

    [Category("Toolkit.Scenes")]
    public class ExtStructuralMetadataTests
    {
        [SetUp]
        public void SetUp()
        {
            Tiles3DExtensions.RegisterExtensions();
        }

        // Test files are from https://github.com/CesiumGS/3d-tiles-validator/tree/main/specs/data/gltfExtensions/

        [Test(Description = "Reads generic 3D Tiles glTF's")]
        // todo: [TestCase("FeatureIdAttributeAndPropertyTableFeatureIdNotInRange.gltf, typeof(ModelException)")]
        [TestCase("FeatureIdAttributePropertyTableInvalidValue.gltf", typeof(ModelException)]
        [TestCase("FeatureIdAttributePropertyTableWithoutPropertyTables.gltf", typeof(ModelException))]
        [TestCase("FeatureIdAttributePropertyTableWithoutStructuralMetadata.gltf", typeof(ModelException))]
        [TestCase("FeatureIdTextureAndPropertyTableFeatureIdNotInRange.gltf", typeof(ModelException))]
        [TestCase("ValidFeatureIdAttributeAndPropertyTable.gltf", null)]
        [TestCase("ValidFeatureIdTextureAndPropertyTable.gltf", null)]
        public void ReadGenericFiles(string file, Type exception = null)
        {
            var fileName = $"./testfixtures/{file}";

            if (exception != null)
            {
                Assert.Throws(exception, delegate { ModelRoot.Load(fileName); });
            }
            else
            {
                var model = ModelRoot.Load(fileName);
                var ctx = new ValidationResult(model, ValidationMode.Strict, true);
                model.ValidateContent(ctx.GetContext());
            }
        }
    }
}
