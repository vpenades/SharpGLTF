using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

using NUnit.Framework;

using SharpGLTF.Geometry;
using SharpGLTF.Geometry.VertexTypes;
using SharpGLTF.Materials;
using SharpGLTF.Scenes;
using SharpGLTF.Schema2;
using SharpGLTF.Validation;

namespace SharpGLTF.ThirdParty
{
    using VBColor1 = VertexBuilder<VertexPosition, VertexColor1, VertexEmpty>;

    [ResourcePathFormat("*\\Assets")]
    [AttachmentPathFormat("*\\?", true)]
    internal class SandboxTests
    {
        [Test]
        public void MaterialCreate()
        {
            Memory.MemoryImage.TryParseMime64("data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAABAAAAAQCAIAAACQkWg2AAAACXBIWXMAAA7DAAAOwwHHb6hkAAAAHXpUWHRUaXRsZQAACJlzSU1LLM0pCUmtKCktSgUAKVIFt/VCuZ8AAAAoelRYdEF1dGhvcgAACJkLy0xOzStJVQhIzUtMSS1WcCzKTc1Lzy8BAG89CQyAoFAQAAAANElEQVQoz2O8cuUKAwxoa2vD2VevXsUqzsRAIqC9Bsb///8TdDey+CD0Awsx7h6NB5prAADPsx0VAB8VRQAAAABJRU5ErkJggg==", out var memoryImage);

            var material = new MaterialBuilder()
                .WithAlpha(Materials.AlphaMode.OPAQUE);            

            material.WithMetallicRoughnessShader()
                .WithMetallicRoughness(0, 1)
                .WithChannelImage(KnownChannel.BaseColor, memoryImage)
                .UseChannel(KnownChannel.BaseColor)
                    .Texture
                    .WithSampler(
                        TextureWrapMode.CLAMP_TO_EDGE,
                        TextureWrapMode.MIRRORED_REPEAT,
                        TextureMipMapFilter.NEAREST_MIPMAP_NEAREST,
                        TextureInterpolationFilter.NEAREST
                    );

            material
                .WithSpecularFactor(memoryImage, 0)
                .WithSpecularColor(memoryImage, Vector3.Zero);

            var model = ModelRoot.CreateModel();
            model.CreateMaterial(material);

            TestContext.WriteLine(model.GetJsonPreview());
        }

        [Test]
        public void TestMorphColorTargets2()
        {
            // create material
            var material = new MaterialBuilder("mat1")
                .WithDoubleSide(true)
                .WithMetallicRoughnessShader();
            var material2 = new MaterialBuilder("mat2")
                .WithDoubleSide(true)
                .WithMetallicRoughnessShader();

            // create a mesh with two primitives, one for each material

            var triangle = new MeshBuilder<VertexPosition, VertexColor1>("mesh");

            var prim = triangle.UsePrimitive(material);
            var redColor = new Vector4(1f, 0f, 0f, 1f);
            prim.AddTriangle(new VBColor1(new VertexPosition(-10, 0, 0), redColor),
                new VBColor1(new VertexPosition(10, 0, 0), redColor),
                new VBColor1(new VertexPosition(0, 10, 0), redColor));

            var prim2 = triangle.UsePrimitive(material2);
            prim2.AddTriangle(new VBColor1(new VertexPosition(-10, 0, 0), redColor),
                new VBColor1(new VertexPosition(10, 0, 0), redColor),
                new VBColor1(new VertexPosition(0, 10, 0), redColor));

            // create a morph target that will change the color from red to green only for prim2
            var greenColor = new Vector4(0f, 1f, 0f, 1f);
            foreach (var p in triangle.Primitives)
            {
                for (var i = 0; i < p.Vertices.Count; ++i)
                {
                    var oldVertexPosition = p.Vertices[i];
                    var greenMat = new VertexColor1(greenColor);

                    ((IPrimitiveBuilder)p).SetVertexDelta(0, i, default,
                        ReferenceEquals(p, prim2)
                            ? greenMat.Subtract(oldVertexPosition.Material)
                            : VertexMaterialDelta.Zero);
                }
            }

            // create a scene
            var scene = new Scenes.SceneBuilder();
            scene.AddRigidMesh(triangle, Matrix4x4.Identity);

            // save the model in different formats
            var model = scene.ToGltf2();

            var json = model.GetJsonPreview();
            AttachmentInfo.From("model.json").WriteAllText(json);
            TestContext.WriteLine(json);

            model.DeepClone();


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

            // Assert
            var morphedColor = triangles[1].A.GetMaterial().GetColor(0);
            Assert.AreEqual(0.5f, morphedColor.X);
            Assert.AreEqual(0.5f, morphedColor.Y);
            Assert.AreEqual(0, morphedColor.Z);
            Assert.AreEqual(1, morphedColor.W);

            morphedColor = triangles[0].A.GetMaterial().GetColor(0);
            Assert.AreEqual(redColor, morphedColor);


            

            var fF= AttachmentInfo
                .From("ColorMorphingMultiPrim.gltf")
                .WriteObject(f => model.Save(f));

            AttachmentInfo.From("ColorMorphingMultiPrim.glb")
                .WriteObject(f => model.Save(f));
            
        }

        [Test]
        public void TestSharedMaterials()
        {
            var red = new MaterialBuilder()
                .WithDoubleSide(true)
                .WithMetallicRoughness(0.9f, 0.1f)
                .WithMetallicRoughnessShader()
                .WithChannelParam(KnownChannel.BaseColor, KnownProperty.RGBA, new Vector4(1, 0, 0, 1));

            VBColor1 v1 = new VBColor1(new Vector3(1, 0, 0));
            VBColor1 v2 = new VBColor1(new Vector3(0, 1, 0));
            VBColor1 v3 = new VBColor1(new Vector3(0, 0, 1));

            var mesh1 = VBColor1.CreateCompatibleMesh("some mesh 1");
            mesh1.UsePrimitive(red).AddTriangle(v1, v2, v3);
            mesh1.UsePrimitive(red).AddTriangle(v1, v2, v3);

            var mesh2 = VBColor1.CreateCompatibleMesh("some mesh 2");
            mesh2.UsePrimitive(red).AddTriangle(v1, v2, v3);

            var scene = new SceneBuilder();
            scene.AddRigidMesh(mesh1, Matrix4x4.Identity);
            scene.AddRigidMesh(mesh2, Matrix4x4.CreateTranslation(10,0,0));

            var gltf = scene.ToGltf2();
        }


        [Test]
        public void TestLoadWideStridefile()
        {
            // https://github.com/vpenades/SharpGLTF/issues/158

            var gltf = ModelRoot.Load(ResourceInfo.From("Gotoarchi\\test1.gltf"));
            Assert.NotNull(gltf);
        }
    }
}
