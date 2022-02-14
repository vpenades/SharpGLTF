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
using VB = SharpGLTF.Geometry.VertexBuilder<SharpGLTF.Geometry.VertexTypes.VertexPosition,
    SharpGLTF.Geometry.VertexTypes.VertexColor1,
    SharpGLTF.Geometry.VertexTypes.VertexEmpty>;

namespace SharpGLTF.ThirdParty
{
    internal class AceCebovTests
    {
        [Test]
        public void TestMorphTargets()
        {
            // create material
            var material = new MaterialBuilder()
                .WithDoubleSide(true)
                .WithMetallicRoughnessShader();

            // create a mesh with two primitives, one for each material

            var triangle = new MeshBuilder<VertexPosition, VertexColor1>("mesh");

            var prim = triangle.UsePrimitive(material);
            var redColor = new Vector4(1f, 0f, 0f, 1f);
            prim.AddTriangle(new VB(new VertexPosition(-10, 0, 0), redColor),
                new VB(new VertexPosition(10, 0, 0), redColor),
                new VB(new VertexPosition(0, 10, 0), redColor));

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

                morphTargetBuilder.SetVertex(vertexPosition, new VB(newVertexPosition,
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
