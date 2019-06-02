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
        private static readonly Vector2[] _BowTiePolygon = new[]
        {
            new Vector2(-10, -10),
            new Vector2(0, -5),
            new Vector2(10, -10),
            new Vector2(10, 10),
            new Vector2(0, 5),
            new Vector2(-10, 10)
        };

        private static readonly Vector2[] _BridgePolygon = new[]
        {            
            new Vector2(-10, 2),
            new Vector2(-10, -10),
            new Vector2(-8, -10),
            new Vector2(-8, -5),
            new Vector2(-4, -1),
            new Vector2(0, 0),
            new Vector2(+4, -1),
            new Vector2(+8, -5),
            new Vector2(+8, -10),
            new Vector2(+10, -10),
            new Vector2(+10, 2),
        };

        private static readonly Vector2[] _CoLinearBaseTriangle = new[]
        {
            new Vector2(0,10),
            new Vector2(-10,-10),
            new Vector2(-5,-10),
            new Vector2(0,-10),
            new Vector2(5,-10),
            new Vector2(10,-10),            
        };

        // polygon with a hole, connected to the outside with a "vertex bridge"
        private static readonly Vector2[] _PoygonWithHole = new[]
        {
            new Vector2(0,4),
            new Vector2(4,0),            

            // inner "hole"            
            new Vector2(2,0),
            new Vector2(0,2),
            new Vector2(-2,0),
            new Vector2(0,-2),
            new Vector2(2,0),

            new Vector2(4,0),
            new Vector2(0,-4),
            new Vector2(-4,0),
        };

        
        private static readonly Vector2[] _SnakePolygon = new[]
        {
            new Vector2(0,1),
            new Vector2(2,3),
            new Vector2(4,1),

            new Vector2(4,0),
            new Vector2(2,2),
            new Vector2(0,0),
            new Vector2(-2,2),
            new Vector2(-4,0),

            new Vector2(-4,1),
            new Vector2(-2,3),
        };

        // this is a pretty messy polygon with lots of degenerated triangles, and holes
        private static readonly Vector2[] _SahaquielPolygon = new[]
        {
            new Vector2(0,0),

            new Vector2(1,0),
            new Vector2(2,1),
            new Vector2(3,0),

            new Vector2(2.5f,0.0f),
            new Vector2(2.0f,0.5f),
            new Vector2(1.5f,0.0f),
            new Vector2(2.0f,-0.5f),
            new Vector2(2.5f,0.0f),

            new Vector2(2,-1),
            new Vector2(1, 0),

            new Vector2(0,0),
            
            new Vector2(-1,0),
            new Vector2(-2,-1),
            new Vector2(-3,0),

            new Vector2(-2.5f,0.0f),
            new Vector2(-2.0f,-0.5f),
            new Vector2(-1.5f,0.0f),
            new Vector2(-2.0f,0.5f),
            new Vector2(-2.5f,0.0f),

            new Vector2(-2,1),
            new Vector2(-1, 0),
        };

        [Test]
        public void CreateEarClippingPolygons()
        {
            var material = new Materials.MaterialBuilder()
                .WithUnlitShader();

            var mesh = VERTEX.CreateCompatibleMesh();
            mesh.Triangulator = BasicEarClippingTriangulation.Default;

            var polygon = _SnakePolygon
                .Select(item => new Vector3(item, 0))
                .ToArray();

            for (int i=0; i < 10; ++i)
            {
                var xform = Matrix4x4.CreateFromYawPitchRoll(i, i *2, i);
                xform = Matrix4x4.CreateTranslation(0, 0, 20) * xform;

                var vertices = polygon
                    .Select(item => new VERTEX(Vector3.Transform(item,xform)) )
                    .ToArray();

                mesh.UsePrimitive(material).AddPolygon(vertices);
            }            

            mesh.AttachToCurrentTest("BasicEarClippingTriangulation.glb");
        }
    }
}
