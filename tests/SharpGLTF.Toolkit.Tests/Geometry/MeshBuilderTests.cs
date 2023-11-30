using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

using NUnit.Framework;

using SharpGLTF.Geometry.VertexTypes;
using SharpGLTF.Geometry.Parametric;
using SharpGLTF.Schema2;
using SharpGLTF.Scenes;

namespace SharpGLTF.Geometry
{
    using VERTEX1 = VertexBuilder<VertexPosition, VertexColor1Texture1, VertexJoints4>;
    using VERTEX2 = VertexBuilder<VertexPositionNormal, VertexColor1Texture1, VertexJoints4>;

    [Category("Toolkit.Geometry")]
    public class MeshBuilderTests
    {
        [Test]
        public void CreateInvalidTriangles()
        {
            var m = new Materials.MaterialBuilder();            

            var mb = VERTEX2.CreateCompatibleMesh();

            // replaces default preprocessor with a debug preprocessor that throws exceptions at the slightest issue.
            mb.VertexPreprocessor.SetValidationPreprocessors();

            int TriangleCounter() { return mb.Primitives.Sum(item => item.Triangles.Count); }

            var prim = mb.UsePrimitive(m);

            var a = VERTEX2
                .Create(Vector3.Zero,Vector3.UnitX)
                .WithMaterial(Vector4.One,Vector2.Zero)
                .WithSkinning((0,1));

            var b = VERTEX2
                .Create(Vector3.UnitX, Vector3.UnitX)
                .WithMaterial(Vector4.One, Vector2.Zero)
                .WithSkinning((0, 1));

            var c = VERTEX2
                .Create(Vector3.UnitY, Vector3.UnitX)
                .WithMaterial(Vector4.One, Vector2.Zero)
                .WithSkinning((0, 1));

            prim.AddTriangle(a, b, c);
            Assert.That(TriangleCounter(), Is.EqualTo(1));

            var v2nan = new Vector2(float.NaN, float.NaN);
            var v3nan = new Vector3(float.NaN, float.NaN, float.NaN);
            var v4nan = new Vector4(float.NaN, float.NaN, float.NaN, float.NaN);

            Assert.Throws(typeof(ArgumentException), () => prim.AddTriangle(a.WithGeometry(v3nan), b, c));
            Assert.That(TriangleCounter(), Is.EqualTo(1));

            Assert.Throws(typeof(ArgumentException), () => prim.AddTriangle(a.WithGeometry(Vector3.Zero, v3nan), b, c));
            Assert.That(TriangleCounter(), Is.EqualTo(1));

            Assert.Throws(typeof(ArgumentOutOfRangeException), () => prim.AddTriangle(a.WithGeometry(Vector3.Zero, Vector3.Zero), b, c));
            Assert.That(TriangleCounter(), Is.EqualTo(1));            
            
            Assert.Throws(typeof(ArgumentOutOfRangeException), () => prim.AddTriangle(a.WithGeometry(Vector3.Zero, Vector3.UnitX * 0.8f), b, c));
            Assert.That(TriangleCounter(), Is.EqualTo(1));

            Assert.Throws(typeof(ArgumentException), () => prim.AddTriangle(a.WithMaterial(v2nan), b, c));
            Assert.That(TriangleCounter(), Is.EqualTo(1));

            Assert.Throws(typeof(ArgumentException), () => prim.AddTriangle(a.WithMaterial(v4nan), b, c));
            Assert.That(TriangleCounter(), Is.EqualTo(1));

            Assert.Throws(typeof(ArgumentOutOfRangeException), () => prim.AddTriangle(a.WithMaterial(Vector4.One*2), b, c));
            Assert.That(TriangleCounter(), Is.EqualTo(1));

            Assert.Throws(typeof(ArgumentOutOfRangeException), () => prim.AddTriangle(a.WithMaterial(-Vector4.One), b, c));
            Assert.That(TriangleCounter(), Is.EqualTo(1));

            Assert.Throws(typeof(ArgumentOutOfRangeException), () => prim.AddTriangle(a.WithSkinning((0,0)), b, c));
            Assert.That(TriangleCounter(), Is.EqualTo(1));
        }

        [Test]
        public void CreateMeshInSanitizedMode()
        {
            var mesh = VERTEX2.CreateCompatibleMesh();

            mesh.VertexPreprocessor.SetSanitizerPreprocessors();

            var prim = mesh.UsePrimitive(Materials.MaterialBuilder.CreateDefault(), 1);

            var p = new VertexPositionNormal(Vector3.UnitX, new Vector3(float.NaN));
            var m = new VertexColor1Texture1(Vector4.One * 7, new Vector2(float.NaN));
            var s = new VertexJoints4((0, 2), (1, 7), (2, 6), (3, 5));
            
            var v1Idx = prim.AddPoint(new VERTEX2(p, m, s));
            var v1Bis = prim.Vertices[v1Idx];

            NumericsAssert.AreEqual(v1Bis.Geometry.Position, Vector3.UnitX);
            NumericsAssert.AreEqual(v1Bis.Geometry.Normal, Vector3.UnitX);
            NumericsAssert.AreEqual(v1Bis.Material.Color, Vector4.One);
            NumericsAssert.AreEqual(v1Bis.Material.TexCoord, Vector2.Zero);
            NumericsAssert.AreEqual(v1Bis.Skinning.Joints, new Vector4(1, 2, 3, 0));
            NumericsAssert.AreEqual(v1Bis.Skinning.Weights, new Vector4(7, 6, 5, 2) / (7f + 6f + 5f + 2f));
        }
        
        [Test]
        public static void CreateWithDegeneratedTriangle()
        {
            // create materials

            var material1 = new Materials.MaterialBuilder()
                .WithMetallicRoughnessShader()
                .WithChannelParam(Materials.KnownChannel.BaseColor, Materials.KnownProperty.RGBA, Vector4.One * 0.5f);

            var material2 = new Materials.MaterialBuilder()
                .WithMetallicRoughnessShader()
                .WithChannelParam(Materials.KnownChannel.BaseColor, Materials.KnownProperty.RGBA, Vector4.One * 0.7f);

            // create a mesh with degenerated triangles

            var validTriangle =
                (
                new Vector3(4373.192624189425f, 5522.678275192156f, -359.8238015332605f),
                new Vector3(4370.978060142137f, 5522.723320999183f, -359.89184701762827f),
                new Vector3(4364.615741107147f, 5511.510615546256f, -359.08922455413233f)
                );

            var degeneratedTriangle =
                (
                new Vector3(4374.713581837248f, 5519.741978117265f, -360.87014389818034f),
                new Vector3(4373.187151107471f, 5521.493282925338f, -355.70835120644153f),
                new Vector3(4373.187151107471f, 5521.493282925338f, -355.70835120644153f)
                );            

            var mesh = new MeshBuilder<VertexPosition>("mesh");
            mesh.VertexPreprocessor.SetValidationPreprocessors();

            var validIndices = mesh.UsePrimitive(material1)
                .AddTriangle
                    (
                    new VertexPosition(validTriangle.Item1),
                    new VertexPosition(validTriangle.Item2),
                    new VertexPosition(validTriangle.Item3)
                    );
            Assert.That(validIndices.A, Is.GreaterThanOrEqualTo(0));
            Assert.That(validIndices.B, Is.GreaterThanOrEqualTo(0));
            Assert.That(validIndices.C, Is.GreaterThanOrEqualTo(0));

            var degenIndices = mesh.UsePrimitive(material2)
                .AddTriangle
                    (
                    new VertexPosition(degeneratedTriangle.Item1),
                    new VertexPosition(degeneratedTriangle.Item2),
                    new VertexPosition(degeneratedTriangle.Item3)
                    );
            Assert.That(degenIndices.A, Is.LessThan(0));
            Assert.That(degenIndices.B, Is.LessThan(0));
            Assert.That(degenIndices.C, Is.LessThan(0));

            // create scene:

            var scene = new SceneBuilder();

            scene.AddRigidMesh(mesh, Matrix4x4.Identity);

            // check gltf2

            var model = scene.ToGltf2();

            Assert.That(model.LogicalMeshes[0].Primitives, Has.Count.EqualTo(1));
        }

        [Test]
        public static void CreateWithMutableSharedMaterial()
        {
            // create materials

            var material1 = Materials.MaterialBuilder.CreateDefault();
            var material2 = Materials.MaterialBuilder.CreateDefault();
            var material3 = Materials.MaterialBuilder.CreateDefault();            

            Assert.That(Materials.MaterialBuilder.AreEqualByContent(material1, material2), Is.True);
            Assert.That(Materials.MaterialBuilder.AreEqualByContent(material1, material3), Is.True);

            Assert.That(material2 != material1);
            Assert.That(material3 != material1);

            // MeshBuilder should split primitives by material reference,
            // because in general, materials will not be immutable.

            var mesh = new MeshBuilder<VertexPosition>();            
            mesh.UsePrimitive(material1, 1).AddPoint(default);
            mesh.UsePrimitive(material2, 1).AddPoint(default);
            mesh.UsePrimitive(material3, 1).AddPoint(default);

            Assert.That(mesh.Primitives, Has.Count.EqualTo(3));

            // create scene

            var scene = new SceneBuilder();

            scene.AddRigidMesh(mesh, Matrix4x4.Identity);

            // check gltf

            // The build process should identify that, at this point, material1, material2 and material3
            // represent the same material, and coalesce to a single material.

            var gltfModel = scene.ToGltf2();                        
            Assert.That(gltfModel.LogicalMaterials, Has.Count.EqualTo(1));

            // since Materials.MaterialBuilder is not immutable we can change the contents,
            // so now, material1, material2 and material3 no longer represent the same material
            material1.WithMetallicRoughnessShader().WithChannelParam(Materials.KnownChannel.BaseColor, Materials.KnownProperty.RGBA, Vector4.One * 0.2f);
            material2.WithMetallicRoughnessShader().WithChannelParam(Materials.KnownChannel.BaseColor, Materials.KnownProperty.RGBA, Vector4.One * 0.4f);
            material3.WithMetallicRoughnessShader().WithChannelParam(Materials.KnownChannel.BaseColor, Materials.KnownProperty.RGBA, Vector4.One * 0.6f);

            gltfModel = scene.ToGltf2();            
            Assert.That(gltfModel.LogicalMaterials, Has.Count.EqualTo(3));

        }

        [Test]
        public void CreateMeshWithTriangleAndQuad()
        {
            var dmat = Materials.MaterialBuilder.CreateDefault();
            var mesh = VERTEX1.CreateCompatibleMesh();
            var prim = mesh.UsePrimitive(dmat);

            var triIdx = prim.AddTriangle(new VERTEX1(Vector3.Zero), new VERTEX1(Vector3.UnitX * 2) , new VERTEX1(Vector3.UnitY * 2));
            var qadIdx = prim.AddQuadrangle(new VERTEX1(-Vector3.UnitX), new VERTEX1(Vector3.UnitY), new VERTEX1(Vector3.UnitX), new VERTEX1(-Vector3.UnitY));

            Assert.That(prim.Vertices, Has.Count.EqualTo(7));
            Assert.That(prim.Triangles, Has.Count.EqualTo(3));
            Assert.That(prim.Surfaces, Has.Count.EqualTo(2));

            Assert.That(triIdx, Is.EqualTo((0, 1, 2)));
            Assert.That(qadIdx, Is.EqualTo((3, 4, 5, 6)));

            Assert.That(prim.GetIndices(), Is.EqualTo(new[] { 0, 1, 2, 3, 4, 5, 3, 5, 6 }));
        }

        [Test]
        public void CreateMeshWithCustomVertexAttribute()
        {
            var dmat = Materials.MaterialBuilder.CreateDefault();
            var mesh = new MeshBuilder<VertexPosition, VertexColor1Texture1Custom1, VertexEmpty>();
            var prim = mesh.UsePrimitive(dmat);

            prim.AddTriangle
                (
                (Vector3.UnitX, (Vector4.One, Vector2.Zero, 0.1f)),
                (Vector3.UnitY, (Vector4.One, Vector2.Zero, 0.2f)),
                (Vector3.UnitZ, (Vector4.One, Vector2.Zero, 0.3f))
                );

            var dstScene = new Schema2.ModelRoot();

            var dstMesh = dstScene.CreateMesh(mesh);

            var batchId = dstMesh.Primitives[0].GetVertexAccessor(VertexColor1Texture1Custom1.CUSTOMATTRIBUTENAME).AsScalarArray();

            Assert.That(batchId, Is.EqualTo(new float[] { 0.1f, 0.2f, 0.3f }));
        }

        [Test]
        public void GenerateTangents()
        {
            var vertices = new VertexBufferColumns();
            vertices.Positions = new[] { Vector3.Zero, Vector3.UnitX, Vector3.UnitY };
            vertices.Normals = new[] { Vector3.UnitZ, Vector3.UnitZ, Vector3.UnitZ };
            vertices.TexCoords0 = new[] { Vector2.Zero, Vector2.UnitX, Vector2.UnitY };

            var indices = new[] { (0,1,2) } as IEnumerable<(int,int,int)>;

            VertexBufferColumns.CalculateTangents(new[] { (vertices, indices) });
        }

        [Test]
        public void CloneMeshBuilder()
        {
            var material1 = Materials.MaterialBuilder.CreateDefault();
            var material2 = Materials.MaterialBuilder.CreateDefault();

            var mesh = new MeshBuilder<VertexPosition>();
            mesh.AddCube(material1, Matrix4x4.Identity);
            mesh.AddSphere(material2, 5, Matrix4x4.CreateTranslation(0, 10, 0));

            var cloned1 = mesh.Clone( m => m.Clone() );

            var primitivePairs = mesh.Primitives.Zip(cloned1.Primitives, (src, dst) => (src, dst));

            foreach(var (src, dst) in primitivePairs)
            {
                Assert.That(dst.Material, Is.Not.SameAs(src.Material));
                Assert.That(dst.Triangles, Has.Count.EqualTo(src.Triangles.Count));

                Assert.That(dst.Triangles, Is.EqualTo(src.Triangles));
            }

            var material3 = Materials.MaterialBuilder.CreateDefault();
            
            // force all geometries to use a single material,
            // which should result in a mesh with with all the primitives
            // of the source mesh merged into a single primitive.
            var cloned2 = cloned1.Clone(m => material3);

            Assert.That(cloned2.Primitives.Count, Is.EqualTo(1));
            Assert.That(cloned2.Primitives.First().Material, Is.SameAs(material3));
            Assert.That(cloned2.Primitives.Sum(item => item.Triangles.Count), Is.EqualTo(cloned1.Primitives.Sum(item => item.Triangles.Count)));
        }
    }
}
