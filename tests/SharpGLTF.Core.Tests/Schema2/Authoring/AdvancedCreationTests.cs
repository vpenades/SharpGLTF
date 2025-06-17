using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

using NUnit.Framework;

namespace SharpGLTF.Schema2.Authoring
{
    internal class AdvancedCreationTests
    {
        [Test(Description = "Creates a model with a triangle mesh")]
        public void CreateSceneWithSparseAccessors()
        {
            TestContext.CurrentContext.AttachGltfValidatorLinks();

            // create model
            var model = ModelRoot.CreateModel();

            // create scene
            var scene = model.DefaultScene = model.UseScene("Default");

            // create node
            var rnode = scene.CreateNode("Triangle Node");

            // create material
            var material = model
                .CreateMaterial("Default")
                .WithDefault(new Vector4(0, 1, 0, 1));

            // create mesh
            var rmesh = rnode.Mesh = model.CreateMesh("Triangle Mesh");

            // create the vertex positions
            var positions = new[]
            {
                new Vector3(0, 10, 0),
                new Vector3(-10, -10, 0),
                new Vector3(10, -10, 0),
            };

            // create an index buffer and fill it            
            var indices = new[] { 0, 1, 2 };

            // create mesh primitive
            var primitive = rmesh
                .CreatePrimitive()
                .WithVertexAccessor("POSITION", positions, true) // (true) notice here that we request creating a BufferView with explicit ByteStride
                .WithIndicesAccessor(PrimitiveType.TRIANGLES, indices)
                .WithMaterial(material);

            // create morph target 0
            
            var morphs0Pos = new Dictionary<int, Vector3>(); // <VertexIndex, Position>
            morphs0Pos[2] = new Vector3(15, -15, 0);

            var morphs0Acc = model.CreateAccessor();
            morphs0Acc.SetDataFrom(primitive.GetVertexAccessor("POSITION")); // set base data from position attribute accessor
            morphs0Acc.CreateSparseData(morphs0Pos);

            var morphs0 = new Dictionary<string, Accessor>();
            morphs0["POSITION"] = morphs0Acc;

            primitive.SetMorphTargetAccessors(0, morphs0);

            // create morph target 1

            var morphs1Pos = new Dictionary<int, Vector3>(); // <VertexIndex, Position>
            morphs1Pos[0] = new Vector3(0, 15, 0);

            var morphs1Acc = model.CreateAccessor();
            morphs1Acc.SetDataFrom(primitive.GetVertexAccessor("POSITION")); // set base data from position attribute accessor
            morphs1Acc.CreateSparseData(morphs1Pos);

            var morphs1 = new Dictionary<string, Accessor>();
            morphs1["POSITION"] = morphs1Acc;

            primitive.SetMorphTargetAccessors(1, morphs1);

            // initialize node with a specific weights blend

            rnode.SetMorphWeights(Transforms.SparseWeight8.Create(  (0, 0.95f), (1, 0.75f) )); // morph 0 at 0.95, morph 1 at 0.75

            // save model

            model.AttachToCurrentTest("result.glb");
            model.AttachToCurrentTest("result.gltf");

            // check accessors

            var m0array = morphs0Acc.AsVector3Array();
            Assert.That(m0array[0], Is.EqualTo(positions[0]));
            Assert.That(m0array[1], Is.EqualTo(positions[1]));
            Assert.That(m0array[2], Is.EqualTo(morphs0Pos[2]));

            var m1array = morphs1Acc.AsVector3Array();
            Assert.That(m1array[0], Is.EqualTo(morphs1Pos[0]));
            Assert.That(m1array[1], Is.EqualTo(positions[1]));
            Assert.That(m1array[2], Is.EqualTo(positions[2]));

        }
    }
}
