using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using NUnit.Framework;

namespace glTF2Sharp.Schema2
{
    [TestFixture]
    public class AccessorSparseTests
    {
        #region setup

        [OneTimeSetUp]
        public void Setup()
        {
            TestFiles.CheckoutDataDirectories();
        }

        #endregion

        [Test]
        public void LoadSparseModels()
        {
            var path = TestFiles.GetSampleFilePaths().FirstOrDefault(item => item.Contains("SimpleSparseAccessor.gltf"));
            
            var model = ModelRoot.Load(path);
            Assert.NotNull(model);

            var primitive = model.LogicalMeshes[0].Primitives[0];

            var accessor = primitive.GetVertexAccessor("POSITION");

            var basePositions = accessor._GetMemoryAccessor().AsVector3Array();

            var positions = accessor.AsVector3Array();            
        }
    }
}
