using NUnit.Framework;
using SharpGLTF.Schema2;

namespace SharpGLTF.ThirdParty
{
    public class AccessorBufferViewMissing
    {
        [Test]
        public void ModelLoadFailsWhenAccessorBufferViewMissing()
        {
            ExtensionsFactory.RegisterExtension<MeshPrimitive, SpzGaussianSplatsCompression>("KHR_spz_gaussian_splats_compression", p => new SpzGaussianSplatsCompression(p));

            // Currently, the model loading fails with SharpGLTF.Validation.SchemaException: 'Accessor[0] _bufferView: must be defined.Model'
            // This is because the accessor does not have a bufferView defined,
            // the bufferView is defined in the Splat extension.
            var modelRoot = ModelRoot.Load("./TestFixtures/tower.glb");
        }

    }
}
