using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using NUnit.Framework;
using SharpGLTF.Geometry;
using SharpGLTF.Geometry.VertexTypes;
using SharpGLTF.Materials;
using SharpGLTF.Runtime;
using SharpGLTF.Schema2;
using SharpGLTF.Transforms;
using SharpGLTF.Validation;

namespace SharpGLTF.ThirdParty
{
    using VBColor1 = VertexBuilder<VertexPosition,VertexColor1,VertexEmpty>;    
    
    public class PetarTasevTests
    {

        [Test]
        public void SkewedMatrix()
        {
            var skewedMatrix = new Matrix4x4
            {
                M11 = 0.92487437f,  M12 = -0.020702988f, M13 = -0.07374853f, M14 = 0f,
                M21 = 0.049406692f, M22 = 0.75778115f,   M23 = 0.02921307f,  M24 = 0f,
                M31 = -0.060421363f, M32 = 0.045907855f, M33 = 0.8235954f, M34 = 0f,
                M41 = 0.56907547f, M42 = 1.1176939f, M43 = 0.31836793f, M44 = 1f
            };

            var affine = new AffineTransform(skewedMatrix);
            Assert.That(!affine.IsDecomposable);

            var mesh = new MeshBuilder<VertexPosition>("mesh");
            var material = new MaterialBuilder("mat1")
               .WithMetallicRoughnessShader();
            var prim1 = mesh.UsePrimitive(material, 1);
            prim1.AddPoint(new VertexPosition(0, 0, 0));

            var scene = new Scenes.SceneBuilder();
            scene.AddRigidMesh(mesh, skewedMatrix);

            var m = scene.ToGltf2();

            var result = AttachmentInfo.From("skewedMatrix.glb").WriteObject(path => m.Save(path));

            var m2 = ModelRoot.Load(result.FullName);
        }


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
            Assert.That(morphedColor, Is.EqualTo(expMorphColor));
            
            morphedColor = triangles[0].A.GetMaterial().GetColor(0);
            Assert.That(morphedColor, Is.EqualTo(redColor));

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