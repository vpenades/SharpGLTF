using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

using NUnit.Framework;

using SharpGLTF.Memory;

namespace SharpGLTF.Schema2.Authoring
{
    internal class AdvancedCreationTests
    {
        [Test(Description = "Creates a primary mesh, and then another mesh derived from the data of the first mesh")]
        public void CreateDerivedSparseMeshes()
        {
            TestContext.CurrentContext.AttachGltfValidatorLinks();

            // create model
            var model = ModelRoot.CreateModel();

            // create scene
            var scene = model.DefaultScene = model.UseScene("Default");            

            // create material
            var material = model
                .CreateMaterial("Default")
                .WithDefault(new Vector4(0, 1, 0, 1));

            // create primary node
            var rnode = scene.CreateNode("Triangle Node");

            // create primary mesh
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

            // create primary mesh primitive
            var originalPrimitive = rmesh
                .CreatePrimitive().WithMaterial(material)
                .WithVertexAccessor("POSITION", positions, true) // notice the (true) to explicitly set ByteStride, which is required for shared BufferViews
                .WithIndicesAccessor(PrimitiveType.TRIANGLES, indices);
            

            // create derived node
            rnode = scene.CreateNode("derived Triangle Node");
            rnode.LocalTransform = new Transforms.AffineTransform(Quaternion.Identity, new Vector3(30, 0, 0));

            // create a sparse accessor based on the primary mesh position accessor, and a sparse override.

            var positionsOverride = new Dictionary<int, Vector3>();
            positionsOverride[0] = new Vector3(0, 30, 0);

            var sparseAccessor = model.CreateAccessor();
            sparseAccessor.SetDataFrom(originalPrimitive.GetVertexAccessor("POSITION")); // set base positions from original primitive
            sparseAccessor.CreateSparseData(positionsOverride);

            // create derived mesh
            var dmesh = rnode.Mesh = model.CreateMesh("derived Triangle Mesh");

            var derivedPrimitive = dmesh.CreatePrimitive().WithMaterial(material);
            derivedPrimitive.SetVertexAccessor("POSITION", sparseAccessor);
            derivedPrimitive.SetIndexAccessor(originalPrimitive.IndexAccessor);

            // save model

            model.AttachToCurrentTest("result.glb");
            model.AttachToCurrentTest("result.gltf");

            var m1array = sparseAccessor.AsVector3Array();
            Assert.That(m1array[0], Is.EqualTo(positionsOverride[0]));
            Assert.That(m1array[1], Is.EqualTo(positions[1]));
            Assert.That(m1array[2], Is.EqualTo(positions[2]));
        }

        [Test(Description = "Creates a model with a triangle mesh")]
        public void CreateMorphTargetsWithSparseAccessors()
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

            // create morph target 0

            var morphs0Pos = new Dictionary<int, Vector3>(); // <VertexIndex, PositionDelta>
            morphs0Pos[2] = new Vector3(5, -5, 0);
            var morphs0Acc = _SetMorphTarget(primitive, 0, morphs0Pos);

            // create morph target 1

            var morphs1Pos = new Dictionary<int, Vector3>(); // <VertexIndex, PositionDelta>
            morphs1Pos[0] = new Vector3(0, 10, 0);
            var morphs1Acc = _SetMorphTarget(primitive, 1, morphs1Pos);

            // initialize node with a specific weights blend
            
            rnode.SetMorphWeights(Transforms.SparseWeight8.Create((0, 0.95f), (1, 0.75f))); // morph 0 at 0.95, morph 1 at 0.75

            Assert.That(rnode.MorphWeights, Is.EqualTo(new float[] { 0.95f, 0.75f }));

            // save model

            model.AttachToCurrentTest("result.glb");
            model.AttachToCurrentTest("result.gltf");

            // check accessors

            var m0array = morphs0Acc.AsVector3Array();
            Assert.That(m0array[0], Is.EqualTo(Vector3.Zero));
            Assert.That(m0array[1], Is.EqualTo(Vector3.Zero));
            Assert.That(m0array[2], Is.EqualTo(morphs0Pos[2]));

            var m1array = morphs1Acc.AsVector3Array();
            Assert.That(m1array[0], Is.EqualTo(morphs1Pos[0]));
            Assert.That(m1array[1], Is.EqualTo(Vector3.Zero));
            Assert.That(m1array[2], Is.EqualTo(Vector3.Zero));

        }

        private static Accessor _SetMorphTarget(MeshPrimitive primitive, int morphTargetIndex, Dictionary<int, Vector3> morphs1Pos)
        {
            var accessor = primitive.LogicalParent.LogicalParent.CreateAccessor();
            accessor.SetZeros(3, AttributeFormat.Float3);
            accessor.CreateSparseData(morphs1Pos);

            var attributes = new Dictionary<string, Accessor>();
            attributes["POSITION"] = accessor;

            primitive.SetMorphTargetAccessors(morphTargetIndex, attributes);

            return accessor;
        }
    }
}
