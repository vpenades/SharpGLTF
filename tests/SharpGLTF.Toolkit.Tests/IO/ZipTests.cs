using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using NUnit.Framework;

using SharpGLTF.Scenes;
using SharpGLTF.Geometry;
using SharpGLTF.Geometry.Parametric;


namespace SharpGLTF.IO
{
    internal class ZipTests
    {
        [Test]
        public void ZipRoundtripTest()
        {
            // create a model
            
            var mesh = new MeshBuilder<Geometry.VertexTypes.VertexPositionNormal, Geometry.VertexTypes.VertexEmpty, Geometry.VertexTypes.VertexEmpty>("SphereMesh");
            mesh.AddSphere(Materials.MaterialBuilder.CreateDefault(), 50, System.Numerics.Matrix4x4.Identity);

            var scene = new SceneBuilder();
            scene.AddRigidMesh(mesh, System.Numerics.Matrix4x4.Identity).WithName("Sphere");

            Schema2.ModelRoot model = scene.ToGltf2();

            Assert.AreEqual("SphereMesh", model.LogicalMeshes[0].Name);
            Assert.AreEqual("Sphere", model.LogicalNodes[0].Name);            

            model = _ZipRoundtrip(model);

            Assert.AreEqual("SphereMesh", model.LogicalMeshes[0].Name);
            Assert.AreEqual("Sphere", model.LogicalNodes[0].Name);
        }


        private static Schema2.ModelRoot _ZipRoundtrip(Schema2.ModelRoot model)
        {
            byte[] raw;

            // write to zip into memory:

            using (var memory = new System.IO.MemoryStream())
            {
                using (var zipWriter = new ZipWriter(memory))
                {
                    zipWriter.AddModel("model.gltf", model);
                }

                raw = memory.ToArray();
            }

            // read the model back:

            using (var memory = new System.IO.MemoryStream(raw, false))
            {
                using (var zipReader = new ZipReader(memory))
                {
                    return zipReader.LoadModel("model.gltf");
                }
            }
        }
    }
}
