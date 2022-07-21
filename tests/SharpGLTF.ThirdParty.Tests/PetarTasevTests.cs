using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using NUnit.Framework;
using SharpGLTF.Geometry;
using SharpGLTF.Geometry.VertexTypes;
using SharpGLTF.Materials;
using SharpGLTF.Runtime;
using SharpGLTF.Schema2;
using SharpGLTF.Validation;

namespace SharpGLTF.ThirdParty
{
    using VBColor1 = VertexBuilder<VertexPosition,VertexColor1,VertexEmpty>;
    
    [ResourcePathFormat("*\\Assets")]
    [AttachmentPathFormat("*\\?", true)]
    public class PetarTasevTests
    {
        [Test]
        public void MorphColor_MultiplePrimitives()
        {
            // create material
            var material = new MaterialBuilder("mat1")
                .WithDoubleSide(true)
                .WithMetallicRoughnessShader();
            var material2 = new MaterialBuilder("mat2")
                .WithDoubleSide(true)
                .WithMetallicRoughnessShader();

            // create a mesh with two primitives, one for each material

            var mesh = new MeshBuilder<VertexPosition, VertexColor1>("mesh");

            var prim1 = mesh.UsePrimitive(material);
            var redColor = new Vector4(1f, 0f, 0f, 1f);
            prim1.AddTriangle(new VBColor1(new VertexPosition(-10, 0, 0), redColor),
                new VBColor1(new VertexPosition(10, 0, 0), redColor),
                new VBColor1(new VertexPosition(0, 10, 0), redColor));
            
            var prim2 = mesh.UsePrimitive(material2);
            prim2.AddTriangle(new VBColor1(new VertexPosition(-10, 0, 0), redColor),
                new VBColor1(new VertexPosition(10, 0, 0), redColor),
                new VBColor1(new VertexPosition(0, 10, 0), redColor));

            var tri2 = new MeshBuilder<VertexPosition, VertexColor1>("mesh2");
            prim1 = tri2.UsePrimitive(material);
            prim1.AddTriangle(new VBColor1(new VertexPosition(-10, 0, 0), redColor),
                new VBColor1(new VertexPosition(10, 0, 0), redColor),
                new VBColor1(new VertexPosition(0, 10, 0), redColor));

            // create a morph target that will change the color from red to green only for prim2
            var greenColor = new Vector4(0f, 1f, 0f, 1f);

            foreach (var p in mesh.Primitives)
            {
                if (p is not IPrimitiveBuilder pb) continue;

                for (var i = 0; i < p.Vertices.Count; ++i)
                {
                    var oldVertexPosition = p.Vertices[i];
                    var greenMat = new VertexColor1(greenColor);                    

                    pb.SetVertexDelta(0, i, default,
                        ReferenceEquals(p, prim2)
                            ? greenMat.Subtract(oldVertexPosition.Material)
                            : VertexMaterialDelta.Zero);
                }
            }

            // create a scene
            var scene = new Scenes.SceneBuilder();
            scene.AddRigidMesh(mesh, Matrix4x4.Identity);
            scene.AddRigidMesh(tri2, Matrix4x4.Identity);

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
            var triangles = model.DefaultScene
                .EvaluateTriangles(null, model.LogicalAnimations[0], 0.5f)
                .ToArray();
            var expMorphColor = Vector4.Lerp(redColor, greenColor, 0.5f);

            // Assert
            var morphedColor = triangles[1].A.GetMaterial().GetColor(0);
            Assert.AreEqual(expMorphColor, morphedColor);
            
            morphedColor = triangles[0].A.GetMaterial().GetColor(0);
            Assert.AreEqual(redColor, morphedColor);

            // save the model in different formats
            AttachmentInfo
                .From("ColorMorphingMultiPrim.glb")
                .WriteObject(f => model.Save(f));

            AttachmentInfo
                .From("ColorMorphingMultiPrim.gltf")
                .WriteObject(f => model.Save(f));

            // save evaluated frames

            var aaa = model.LogicalMeshes[0].Decode();

            for (int i=0; i < 5; ++i)
            {
                AttachmentInfo
                .From($"ColorMorphingMultiPrim_{i}.obj")
                .WriteObject(f => model.SaveAsWavefront(f, model.LogicalAnimations[0], (float)i / 5));
            }
        }
    }
}