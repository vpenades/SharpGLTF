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
                .WithVertexAccessor("POSITION", positions)                
                .WithIndicesAccessor(PrimitiveType.TRIANGLES, indices)
                .WithMaterial(material);

            // create morph target
            
            var morphs0Pos = new Dictionary<int, Vector3>();
            morphs0Pos[2] = new Vector3(11, -11, 0);

            var morphs0Acc = model.CreateAccessor();
            morphs0Acc.SetZeros(3, DimensionType.VEC3, EncodingType.FLOAT, false);
            morphs0Acc.CreateSparseData(morphs0Pos);

            var morphs0 = new Dictionary<string, Accessor>();
            morphs0["POSITION"] = morphs0Acc;

            primitive.SetMorphTargetAccessors(0, morphs0);

            rnode.SetMorphWeights(Transforms.SparseWeight8.Create(  (0, 0.75f) ));

            model.AttachToCurrentTest("result.glb");
            model.AttachToCurrentTest("result.gltf");
        }
    }
}
