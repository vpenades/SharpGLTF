using System;
using System.Linq;
using System.Numerics;

using NUnit.Framework;

using SharpGLTF.Geometry.VertexTypes;

namespace SharpGLTF.Geometry
{
    using VERTEX1 = VertexBuilder<VertexPosition, VertexColor1Texture1, VertexJoints8x4>;
    using VERTEX2 = VertexBuilder<VertexPositionNormal, VertexColor1Texture1, VertexJoints8x4>;

    [Category("Toolkit.Geometry")]
    public class MeshBuilderTests
    {
        [Description("Although a triangle with three corners at zero should be accounted as a degenerated triangle, if it has different skinning per vertex, it should not be discarded.")]
        [Test]
        public void CreatePseudoDegeneratedTriangle()
        {
            var m = new Materials.MaterialBuilder();

            var mb = VERTEX1.CreateCompatibleMesh();

            var a = new VERTEX1(Vector3.Zero, (0, 1));
            var b = new VERTEX1(Vector3.Zero, (1, 1));
            var c = new VERTEX1(Vector3.Zero, (2, 1));

            mb.UsePrimitive(m).AddTriangle(a, b, c);

            var triCount = mb.Primitives.Sum(item => item.Triangles.Count());

            Assert.AreEqual(1, triCount);
        }

        [Test]
        public void CreateInvalidTriangles()
        {
            var m = new Materials.MaterialBuilder();            

            var mb = VERTEX2.CreateCompatibleMesh();

            // replaces default preprocessor with a debug preprocessor that throws exceptions at the slightest issue.
            mb.VertexPreprocessor.SetDebugPreprocessors();

            int TriangleCounter() { return mb.Primitives.Sum(item => item.Triangles.Count()); }

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
            Assert.AreEqual(1, TriangleCounter());

            var v2nan = new Vector2(float.NaN, float.NaN);
            var v3nan = new Vector3(float.NaN, float.NaN, float.NaN);
            var v4nan = new Vector4(float.NaN, float.NaN, float.NaN, float.NaN);

            Assert.Throws(typeof(ArgumentException), () => prim.AddTriangle(a.WithGeometry(v3nan), b, c));
            Assert.AreEqual(1, TriangleCounter());

            Assert.Throws(typeof(ArgumentException), () => prim.AddTriangle(a.WithGeometry(Vector3.Zero, v3nan), b, c));
            Assert.AreEqual(1, TriangleCounter());

            Assert.Throws(typeof(ArgumentOutOfRangeException), () => prim.AddTriangle(a.WithGeometry(Vector3.Zero, Vector3.Zero), b, c));
            Assert.AreEqual(1, TriangleCounter());            
            
            Assert.Throws(typeof(ArgumentOutOfRangeException), () => prim.AddTriangle(a.WithGeometry(Vector3.Zero, Vector3.UnitX * 0.8f), b, c));
            Assert.AreEqual(1, TriangleCounter());

            Assert.Throws(typeof(ArgumentException), () => prim.AddTriangle(a.WithMaterial(v2nan), b, c));
            Assert.AreEqual(1, TriangleCounter());

            Assert.Throws(typeof(ArgumentException), () => prim.AddTriangle(a.WithMaterial(v4nan), b, c));
            Assert.AreEqual(1, TriangleCounter());

            Assert.Throws(typeof(ArgumentOutOfRangeException), () => prim.AddTriangle(a.WithMaterial(Vector4.One*2), b, c));
            Assert.AreEqual(1, TriangleCounter());

            Assert.Throws(typeof(ArgumentOutOfRangeException), () => prim.AddTriangle(a.WithMaterial(-Vector4.One), b, c));
            Assert.AreEqual(1, TriangleCounter());

            Assert.Throws(typeof(ArgumentOutOfRangeException), () => prim.AddTriangle(a.WithSkinning((0,0)), b, c));
            Assert.AreEqual(1, TriangleCounter());
        }

        [Test]
        public void CreateMeshInSanitizedMode()
        {
            var mesh = VERTEX2.CreateCompatibleMesh();

            mesh.VertexPreprocessor.SetSanitizerPreprocessors();

            var prim = mesh.UsePrimitive(Materials.MaterialBuilder.CreateDefault(), 1);

            var p = new VertexPositionNormal(Vector3.UnitX, new Vector3(float.NaN));
            var m = new VertexColor1Texture1(Vector4.One * 7, new Vector2(float.NaN));
            var s = new VertexJoints8x4((0, 2), (1, 7), (2, 6), (3, 5));

            var v1 = new VERTEX2(p, m, s);
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
        public void CreatePartiallyEmptyMesh()
        {
            var p0 = new Vector3(4403.12831325084f, 5497.3228336684406f, -451.62756590108586f);
            var p1 = new Vector3(4403.1283132596873f, 5497.3228336591274f, -451.62756593199413f);
            var p2 = new Vector3(4392.54991199635f, 5483.549242743291f, -450.72132376581396f);

            Assert.AreEqual(p0, p1);



            /*
            var triangle = new Triangle(p0, p1, p2);
            var normal = triangle.GetNormal();
            var material1 = new MaterialBuilder().WithDoubleSide(true).WithMetallicRoughnessShader().WithChannelParam("BaseColor", new Vector4(1, 1, 1, 1));
            var mesh = new MeshBuilder<VertexPositionNormal>("mesh");
            var prim = mesh.UsePrimitive(material1);
            prim.AddTriangle(
            new VertexPositionNormal((float)triangle.GetP0().X, (float)triangle.GetP0().Y, (float)triangle.GetP0().Z, normal.X, normal.Y, normal.Z),
            new VertexPositionNormal((float)triangle.GetP1().X, (float)triangle.GetP1().Y, (float)triangle.GetP1().Z, normal.X, normal.Y, normal.Z),
            new VertexPositionNormal((float)triangle.GetP2().X, (float)triangle.GetP2().Y, (float)triangle.GetP2().Z, normal.X, normal.Y, normal.Z));

            var model = ModelRoot.CreateModel();
            try
            {
                model.CreateMeshes(mesh);
            }
            catch (ArgumentOutOfRangeException e)
            {
                Console.WriteLine(e);
            }*/
        }

    }
}
