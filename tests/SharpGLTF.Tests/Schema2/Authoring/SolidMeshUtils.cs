using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace SharpGLTF.Schema2.Authoring
{
    using Geometry;    
    
    using VPOS = Geometry.VertexTypes.VertexPosition;
    using VTEX1 = Geometry.VertexTypes.VertexTexture1;

    using VPOSNRM = Geometry.VertexTypes.VertexPositionNormal;

    static class SolidMeshUtils
    {
        public static void AddCube<TMaterial>(this MeshBuilder<TMaterial, VPOSNRM> meshBuilder, TMaterial material, Matrix4x4 xform)
        {
            meshBuilder._AddCubeFace(material, Vector3.UnitX, Vector3.UnitY, Vector3.UnitZ, xform);
            meshBuilder._AddCubeFace(material, -Vector3.UnitX, Vector3.UnitZ, Vector3.UnitY, xform);

            meshBuilder._AddCubeFace(material, Vector3.UnitY, Vector3.UnitZ, Vector3.UnitX, xform);
            meshBuilder._AddCubeFace(material, -Vector3.UnitY, Vector3.UnitX, Vector3.UnitZ, xform);

            meshBuilder._AddCubeFace(material, Vector3.UnitZ, Vector3.UnitX, Vector3.UnitY, xform);
            meshBuilder._AddCubeFace(material, -Vector3.UnitZ, Vector3.UnitY, Vector3.UnitX, xform);
        }

        private static void _AddCubeFace<TMaterial>(this MeshBuilder<TMaterial, VPOSNRM> meshBuilder, TMaterial material, Vector3 origin, Vector3 axisX, Vector3 axisY, Matrix4x4 xform)
        {
            var p1 = Vector3.Transform(origin - axisX - axisY, xform);
            var p2 = Vector3.Transform(origin + axisX - axisY, xform);
            var p3 = Vector3.Transform(origin + axisX + axisY, xform);
            var p4 = Vector3.Transform(origin - axisX + axisY, xform);
            var n = Vector3.Normalize(Vector3.TransformNormal(origin, xform));

            meshBuilder.UsePrimitive(material)
                .AddPolygon
                (
                new VPOSNRM(p1, n),
                new VPOSNRM(p2, n),
                new VPOSNRM(p3, n),
                new VPOSNRM(p4, n)
                );
        }

        public static void AddSphere<TMaterial>(this MeshBuilder<TMaterial, VPOSNRM> meshBuilder, TMaterial material, Single radius, Matrix4x4 xform)
        {
            // http://blog.andreaskahler.com/2009/06/creating-icosphere-mesh-in-code.html

            var t = 1 + (float)(Math.Sqrt(5.0) / 2);

            var v0 = new Vector3(-1, t, 0) * radius;
            var v1 = new Vector3(1, t, 0) * radius;
            var v2 = new Vector3(-1, -t, 0) * radius;
            var v3 = new Vector3(1, -t, 0) * radius;

            var v4 = new Vector3(0, -1, t) * radius;
            var v5 = new Vector3(0, 1, t) * radius;
            var v6 = new Vector3(0, -1, -t) * radius;
            var v7 = new Vector3(0, 1, -t) * radius;

            var v8 = new Vector3(t, 0, -1) * radius;
            var v9 = new Vector3(t, 0, 1) * radius;
            var v10 = new Vector3(-t, 0, -1) * radius;
            var v11 = new Vector3(-t, 0, 1) * radius;

            // 5 faces around point 0
            meshBuilder._AddSphereTriangle(material, xform, v0, v11, v5, 3);
            meshBuilder._AddSphereTriangle(material, xform, v0, v5, v1, 3);
            meshBuilder._AddSphereTriangle(material, xform, v0, v1, v7, 3);
            meshBuilder._AddSphereTriangle(material, xform, v0, v7, v10, 3);
            meshBuilder._AddSphereTriangle(material, xform, v0, v10, v11, 3);

            // 5 adjacent faces
            meshBuilder._AddSphereTriangle(material, xform, v1, v5, v9, 3);
            meshBuilder._AddSphereTriangle(material, xform, v5, v11, v4, 3);
            meshBuilder._AddSphereTriangle(material, xform, v11, v10, v2, 3);
            meshBuilder._AddSphereTriangle(material, xform, v10, v7, v6, 3);
            meshBuilder._AddSphereTriangle(material, xform, v7, v1, v8, 3);

            // 5 faces around point 3
            meshBuilder._AddSphereTriangle(material, xform, v3, v9, v4, 3);
            meshBuilder._AddSphereTriangle(material, xform, v3, v4, v2, 3);
            meshBuilder._AddSphereTriangle(material, xform, v3, v2, v6, 3);
            meshBuilder._AddSphereTriangle(material, xform, v3, v6, v8, 3);
            meshBuilder._AddSphereTriangle(material, xform, v3, v8, v9, 3);

            // 5 adjacent faces
            meshBuilder._AddSphereTriangle(material, xform, v4, v9, v5, 3);
            meshBuilder._AddSphereTriangle(material, xform, v2, v4, v11, 3);
            meshBuilder._AddSphereTriangle(material, xform, v6, v2, v10, 3);
            meshBuilder._AddSphereTriangle(material, xform, v8, v6, v7, 3);
            meshBuilder._AddSphereTriangle(material, xform, v9, v8, v1, 3);
        }

        private static void _AddSphereTriangle<TMaterial>(this MeshBuilder<TMaterial, VPOSNRM> meshBuilder, TMaterial material, Matrix4x4 xform, Vector3 a, Vector3 b, Vector3 c, int iterations = 0)
        {
            if (iterations <=0)
            {
                var aa = new VPOSNRM(Vector3.Transform(a, xform), Vector3.Normalize(Vector3.TransformNormal(a, xform)));
                var bb = new VPOSNRM(Vector3.Transform(b, xform), Vector3.Normalize(Vector3.TransformNormal(b, xform)));
                var cc = new VPOSNRM(Vector3.Transform(c, xform), Vector3.Normalize(Vector3.TransformNormal(c, xform)));

                meshBuilder.UsePrimitive(material).AddTriangle(aa, bb, cc);
                return;
            }

            --iterations;

            var ab = Vector3.Normalize(a + b) * a.Length();
            var bc = Vector3.Normalize(b + c) * b.Length();
            var ca = Vector3.Normalize(c + a) * c.Length();

            // central triangle
            _AddSphereTriangle(meshBuilder, material, xform, ab, bc, ca, iterations);

            // vertex triangles
            _AddSphereTriangle(meshBuilder, material, xform, a, ab, ca, iterations);
            _AddSphereTriangle(meshBuilder, material, xform, b, bc, ab, iterations);
            _AddSphereTriangle(meshBuilder, material, xform, c, ca, bc, iterations);
        }

        public static MeshBuilder<Materials.MaterialBuilder, VPOS, VTEX1> CreateTerrainMesh(int width, int length, Func<int,int,float> heightFunction, string terrainColorImagePath)
        {
            // we create a new material to use with the terrain mesh
            var material = new Materials.MaterialBuilder("TerrainMaterial")
                .WithChannelTexture("BaseColor", 0, terrainColorImagePath);

            // we create a MeshBuilder
            var terrainMesh = new MeshBuilder<Materials.MaterialBuilder, VPOS, VTEX1>("terrain");

            var texScale = new Vector2(width, length);

            // fill the MeshBuilder with quads using the heightFunction.
            for (int y = 1; y < length; ++y)
            {
                for (int x = 1; x < width; ++x)
                {
                    // quad vertex positions

                    var a = new Vector3(x - 1, heightFunction(x - 1, y + 0), y + 0);
                    var b = new Vector3(x + 0, heightFunction(x + 0, y + 0), y + 0);
                    var c = new Vector3(x + 0, heightFunction(x + 0, y - 1), y - 1);
                    var d = new Vector3(x - 1, heightFunction(x - 1, y - 1), y - 1);

                    // quad UV coordinates

                    var at = new Vector2(a.X, a.Z) / texScale;
                    var bt = new Vector2(b.X, b.Z) / texScale;
                    var ct = new Vector2(c.X, c.Z) / texScale;
                    var dt = new Vector2(d.X, d.Z) / texScale;

                    terrainMesh
                        .UsePrimitive(material)
                        .AddPolygon
                        (
                            (a, at),
                            (b, bt),
                            (c, ct),
                            (d, dt)
                        );
                }
            }

            return terrainMesh;
        }
    }
}
