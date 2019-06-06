using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;

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

    }
}
