using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

using NUnit.Framework;

using SharpGLTF.Schema2;

namespace SharpGLTF.ThirdParty
{
    [ResourcePathFormat("*\\Assets")]
    [AttachmentPathFormat("*\\?", true)]
    internal class MeltyPlayerTests
    {
        /// <summary>
        /// Regression: The only way to write models that exceed 2gb is to disable MergeBuffers.
        /// </summary>
        /// <remarks>
        /// <see href="https://github.com/vpenades/SharpGLTF/issues/165">#165</see>
        /// </remarks>
        [Test]        
        public void CreateSceneWithMultipleBuffers()
        {
            TestContext.CurrentContext.AttachGltfValidatorLinks();

            // create model
            var model = ModelRoot.CreateModel();

            // create scene
            var scene = model.DefaultScene = model.UseScene("Default");

            // create material
            var material = model.CreateMaterial("Default")
                .WithDefault(new Vector4(0, 1, 0, 1))
                .WithDoubleSide(true);

            void addNodeMesh(string name, Matrix4x4 xform)
            {
                // create node
                var rnode = scene.CreateNode(name);

                // create mesh
                var rmesh = rnode.Mesh = model.CreateMesh($"{name} Triangle Mesh");

                // create the vertex positions
                var positions = new[]
                {
                    Vector3.Transform(new Vector3(0, 10, 0), xform),
                    Vector3.Transform(new Vector3(-10, -10, 0), xform),
                    Vector3.Transform(new Vector3(10, -10, 0), xform),
                };

                // create an index buffer and fill it            
                var indices = new[] { 0, 1, 2 };

                // create mesh primitive
                var primitive = rmesh.CreatePrimitive()
                    .WithVertexAccessor("POSITION", positions)
                    .WithIndicesAccessor(PrimitiveType.TRIANGLES, indices)
                    .WithMaterial(material);
            }

            addNodeMesh("Node1", Matrix4x4.Identity);
            addNodeMesh("Node2", Matrix4x4.CreateTranslation(20, 0, 0));

            var ws = new WriteSettings();
            ws.MergeBuffers = false;
            ws.JsonIndented = true;

            var resultPath0 = AttachmentInfo.From("result0.gltf").WriteObject(f => model.Save(f, ws));

            var satellites = ModelRoot.GetSatellitePaths(resultPath0.FullName);

            Assert.AreEqual(4, satellites.Length);
        }
    }
}
