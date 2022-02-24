using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;

using NUnit.Framework;

using SharpGLTF.Geometry;
using SharpGLTF.Geometry.VertexTypes;
using SharpGLTF.Materials;
using SharpGLTF.Memory;

namespace SharpGLTF.ThirdParty
{
    using VBColor1 = VertexBuilder<VertexPosition,VertexColor1,VertexEmpty>;
    using VBColor1Texture1 = VertexBuilder<VertexPosition,VertexColor1Texture1,VertexEmpty>;
    using VBTexture1 = VertexBuilder<VertexPosition,VertexTexture1,VertexEmpty>;

    internal class AceCebovTests
    {
        private static string AssetsPath = Path.Combine(TestContext.CurrentContext.TestDirectory, "Assets");

        [Test]
        public void TestMorphColorTargets()
        {
            // create material
            var material = new MaterialBuilder()
                .WithDoubleSide(true)
                .WithMetallicRoughnessShader();

            // create a mesh with two primitives, one for each material

            var triangle = new MeshBuilder<VertexPosition, VertexColor1>("mesh");

            var prim = triangle.UsePrimitive(material);
            var redColor = new Vector4(1f, 0f, 0f, 1f);
            prim.AddTriangle(new VBColor1(new VertexPosition(-10, 0, 0), redColor),
                new VBColor1(new VertexPosition(10, 0, 0), redColor),
                new VBColor1(new VertexPosition(0, 10, 0), redColor));

            // create a scene
            var scene = new Scenes.SceneBuilder();

            scene.AddRigidMesh(triangle, Matrix4x4.Identity);

            var greenColor = new Vector4(0f, 1f, 0f, 1f);

            // create a morph target that will move the triangle in X axis by 1 unit
            // and change the color from red to green
            var morphTargetBuilder = triangle.UseMorphTarget(0);
            foreach (var vertexPosition in morphTargetBuilder.Vertices)
            {
                var newVertexPosition = vertexPosition;

                // new vertex position is moved in X direction by 1 unit
                newVertexPosition.Position.X += 1;

                morphTargetBuilder.SetVertex(vertexPosition, new VBColor1(newVertexPosition,
                    // morph to green color
                    greenColor));
            }

            Assert.AreEqual(3, morphTargetBuilder.Vertices.Count);

            // save the model in different formats
            var model = scene.ToGltf2();

            var animation = model.CreateAnimation();

            // create a morph channel
            animation.CreateMorphChannel(model.LogicalNodes[0],
                new Dictionary<float, float[]>
                {
                    { 0f, new[] { 0f } },
                    { 1f, new[] { 1f } }
                }, 1);

            // evaluate triangles at animation 0.5, and get the color of the first pixel of the first triangle

            var triangles = Schema2.Toolkit
                .EvaluateTriangles(model.DefaultScene, null, model.LogicalAnimations[0], 0.5f)
                .ToArray();

            var morphedColor = triangles[0].A.GetMaterial().GetColor(0);
            Assert.AreEqual(0.5f, morphedColor.X);
            Assert.AreEqual(0.5f, morphedColor.Y);
            Assert.AreEqual(0, morphedColor.Z);
            Assert.AreEqual(1, morphedColor.W);

            // save the model in different formats
            // model.AttachToCurrentTest("ColorAndTextureMorphing.glb");
            // model.AttachToCurrentTest("ColorAndTextureMorphing.gltf");

            // bypassing AttachToCurrentTest until glTFValidator is fixed.

            var outPath = TestContext.CurrentContext.GetAttachmentPath("ColorMorphing.glb", true);
            model.Save(outPath);
            TestContext.AddTestAttachment(outPath);

            outPath = TestContext.CurrentContext.GetAttachmentPath("ColorMorphing.gltf", true);
            model.Save(outPath);
            TestContext.AddTestAttachment(outPath);
        }

        [Test]
        public void TestMorphTextureTargets()
        {
            // create material
            var material = new MaterialBuilder()
                .WithDoubleSide(true)
                .WithMetallicRoughnessShader()
                .WithChannelImage(KnownChannel.BaseColor, new MemoryImage(Path.Combine(AssetsPath, "texture.png")));

            // create a mesh with two primitives, one for each material

            var triangle = new MeshBuilder<VertexPosition, VertexTexture1>("mesh");

            var prim = triangle.UsePrimitive(material);
            prim.AddTriangle(new VBTexture1(new VertexPosition(-10, 0, 0), Vector2.Zero),
                new VBTexture1(new VertexPosition(10, 0, 0), Vector2.UnitX),
                new VBTexture1(new VertexPosition(0, 10, 0), Vector2.UnitY));

            // create a scene
            var scene = new Scenes.SceneBuilder();

            scene.AddRigidMesh(triangle, Matrix4x4.Identity);

            // create a morph target that will move the triangle in X axis by 1 unit
            // and change the color from red to green
            var morphTargetBuilder = triangle.UseMorphTarget(0);
            foreach (var vertexPosition in morphTargetBuilder.Vertices)
            {
                var newVertexPosition = vertexPosition;

                // new vertex position is moved in X direction by 1 unit
                newVertexPosition.Position.X += 1;

                morphTargetBuilder.SetVertex(vertexPosition, new VBTexture1(newVertexPosition,
                    // morph all to (1, 1)
                    Vector2.One));
            }

            Assert.AreEqual(3, morphTargetBuilder.Vertices.Count);

            // save the model in different formats
            var model = scene.ToGltf2();

            var animation = model.CreateAnimation();

            // create a morph channel
            animation.CreateMorphChannel(model.LogicalNodes[0],
                new Dictionary<float, float[]>
                {
                    { 0f, new[] { 0f } },
                    { 1f, new[] { 1f } }
                }, 1);

            // evaluate triangles at animation 0.5, and get the color of the first pixel of the first triangle

            var triangles = Schema2.Toolkit
                .EvaluateTriangles(model.DefaultScene, null, model.LogicalAnimations[0], 0.5f)
                .ToArray();

            var morphedTextCoord = triangles[0].A.GetMaterial().GetTexCoord(0);
            Assert.AreEqual(0.5f, morphedTextCoord.X);
            Assert.AreEqual(0.5f, morphedTextCoord.Y);

            // save the model in different formats
            // model.AttachToCurrentTest("ColorAndTextureMorphing.glb");
            // model.AttachToCurrentTest("ColorAndTextureMorphing.gltf");

            // bypassing AttachToCurrentTest until glTFValidator is fixed.

            var outPath = TestContext.CurrentContext.GetAttachmentPath("TextureMorphing.glb", true);
            model.Save(outPath);
            TestContext.AddTestAttachment(outPath);

            outPath = TestContext.CurrentContext.GetAttachmentPath("TextureMorphing.gltf", true);
            model.Save(outPath);
            TestContext.AddTestAttachment(outPath);
        }

        [Test]
        public void TestMorphColorAndTextureTargets()
        {
            // create material
            var material = new MaterialBuilder()
                .WithDoubleSide(true)
                .WithMetallicRoughnessShader()
                .WithChannelImage(KnownChannel.BaseColor, new MemoryImage(Path.Combine(AssetsPath, "texture.png")));

            // create a mesh with two primitives, one for each material

            var triangle = new MeshBuilder<VertexPosition, VertexColor1Texture1>("mesh");

            var prim = triangle.UsePrimitive(material);
            var whiteColor = Vector4.One;
            prim.AddTriangle(new VBColor1Texture1(new VertexPosition(-10, 0, 0), new VertexColor1Texture1(whiteColor, Vector2.Zero)),
                new VBColor1Texture1(new VertexPosition(10, 0, 0), new VertexColor1Texture1(whiteColor, Vector2.UnitX)),
                new VBColor1Texture1(new VertexPosition(0, 10, 0), new VertexColor1Texture1(whiteColor, Vector2.UnitY)));

            // create a scene
            var scene = new Scenes.SceneBuilder();

            scene.AddRigidMesh(triangle, Matrix4x4.Identity);

            var morphToTransparentGrey = new Vector4(0.5f);
            // create a morph target that will move the triangle in X axis by 1 unit
            // and change the color from red to green
            var morphTargetBuilder = triangle.UseMorphTarget(0);
            foreach (var vertexPosition in morphTargetBuilder.Vertices)
            {
                var newVertexPosition = vertexPosition;

                // new vertex position is moved in X direction by 1 unit
                newVertexPosition.Position.X += 1;

                morphTargetBuilder.SetVertex(vertexPosition, new VBColor1Texture1(newVertexPosition,
                    // morph to transparent grey
                    // morph all to UV: (1, 1)
                    new VertexColor1Texture1(morphToTransparentGrey, Vector2.One)));
            }

            Assert.AreEqual(3, morphTargetBuilder.Vertices.Count);

            // save the model in different formats
            var model = scene.ToGltf2();

            var animation = model.CreateAnimation();

            // create a morph channel
            animation.CreateMorphChannel(model.LogicalNodes[0],
                new Dictionary<float, float[]>
                {
                    { 0f, new[] { 0f } },
                    { 1f, new[] { 1f } }
                }, 1);

            // evaluate triangles at animation 0.5, and get the color of the first pixel of the first triangle

            var triangles = Schema2.Toolkit
                .EvaluateTriangles(model.DefaultScene, null, model.LogicalAnimations[0], 0.5f)
                .ToArray();

            var morphedTextCoord = triangles[0].A.GetMaterial().GetTexCoord(0);
            Assert.AreEqual(0.5f, morphedTextCoord.X);
            Assert.AreEqual(0.5f, morphedTextCoord.Y);

            // save the model in different formats
            // model.AttachToCurrentTest("ColorAndTextureMorphing.glb");
            // model.AttachToCurrentTest("ColorAndTextureMorphing.gltf");

            // bypassing AttachToCurrentTest until glTFValidator is fixed.

            var outPath = TestContext.CurrentContext.GetAttachmentPath("ColorAndTextureMorphing.glb", true);
            model.Save(outPath);
            TestContext.AddTestAttachment(outPath);

            outPath = TestContext.CurrentContext.GetAttachmentPath("ColorAndTextureMorphing.gltf", true);
            model.Save(outPath);
            TestContext.AddTestAttachment(outPath);
        }
    }
}
