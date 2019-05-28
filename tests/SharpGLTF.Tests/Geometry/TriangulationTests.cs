using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;

using NUnit.Framework;

namespace SharpGLTF.Geometry
{
    using VERTEX = VertexBuilder<VertexTypes.VertexPosition, VertexTypes.VertexEmpty, VertexTypes.VertexEmpty>;

    [Category("Model Authoring")]
    public class TriangulationTests
    {
        private static readonly Vector3[] _BowTiePolygon = new[]
        {
            new Vector3(-10, -10, 0),
            new Vector3(0, -5, 0),
            new Vector3(10, -10, 0),
            new Vector3(10, 10, 0),
            new Vector3(0, 5, 0),
            new Vector3(-10, 10, 0)
        };

        private static readonly Vector3[] _BridgePolygon = new[]
        {            
            new Vector3(-10, 2, 0),
            new Vector3(-10, -10, 0),
            new Vector3(-8, -10, 0),
            new Vector3(-8, -5, 0),
            new Vector3(-4, -1, 0),
            new Vector3(0, 0, 0),
            new Vector3(+4, -1, 0),
            new Vector3(+8, -5, 0),
            new Vector3(+8, -10, 0),
            new Vector3(+10, -10, 0),
            new Vector3(+10, 2, 0),
        };

        private static readonly Vector3[] _CoLinearBaseTriangle = new[]
        {
            new Vector3(0,10,0),
            new Vector3(-10,-10,0),
            new Vector3(-5,-10,0),
            new Vector3(0,-10,0),
            new Vector3(5,-10,0),
            new Vector3(10,-10,0),            
        };

        [Test]
        public void CreateEarClippingPolygons()
        {
            var material = new Materials.MaterialBuilder()
                .WithUnlitShader();

            var mesh = VERTEX.CreateCompatibleMesh();
            mesh.Triangulator = BasicEarClippingTriangulation.Default;

            for(int i=0; i < 10; ++i)
            {
                var xform = Matrix4x4.CreateFromYawPitchRoll(i, i *2, i);
                xform = Matrix4x4.CreateTranslation(0, 0, 20) * xform;

                var vertices = _BridgePolygon
                    .Select(item => new VERTEX(Vector3.Transform(item,xform)) )
                    .ToArray();

                mesh.UsePrimitive(material).AddPolygon(vertices);
            }            

            mesh.AttachToCurrentTest("BasicEarClippingTriangulation.glb");
        }
    }
}
