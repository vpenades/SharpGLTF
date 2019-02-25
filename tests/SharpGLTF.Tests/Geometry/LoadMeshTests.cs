using System;
using System.Collections.Generic;
using System.Text;

using NUnit.Framework;

namespace SharpGLTF.Geometry
{
    [TestFixture]
    public class LoadMeshTests
    {
        #region setup

        [OneTimeSetUp]
        public void Setup()
        {
            TestFiles.CheckoutDataDirectories();
        }

        #endregion

        [Test]
        public void LoadModels()
        {
            foreach (var f in TestFiles.GetSampleFilePaths())
            {
                var root = GltfUtils.LoadModel(f);
                Assert.NotNull(root);

                var meshes = Mesh.Create(root.LogicalMeshes);                
            }
        }
    }
}
