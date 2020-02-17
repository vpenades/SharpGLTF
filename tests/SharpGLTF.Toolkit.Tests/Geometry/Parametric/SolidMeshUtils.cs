using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

using SharpGLTF.Geometry;
using SharpGLTF.Geometry.VertexTypes;

namespace SharpGLTF.Geometry.Parametric
{
    using VERTEX = VertexBuilder<VertexPositionNormal, VertexColor1Texture1, VertexEmpty>;

    abstract class ParametricShape<TMaterial>
    {
        public abstract void AddTo(IMeshBuilder<TMaterial> meshBuilder, Matrix4x4 xform);

        public MeshBuilder<TMaterial, VertexPositionNormal, VertexColor1Texture1, VertexEmpty> ToMesh(Matrix4x4 xform)
        {
            var mesh = new MeshBuilder<TMaterial, VertexPositionNormal, VertexColor1Texture1, VertexEmpty>();

            AddTo(mesh, xform);

            return mesh;
        }
    }

    class Cube<TMaterial> : ParametricShape<TMaterial>
    {
        // TODO:
        // Faces and UV alignment should follow: https://github.com/KhronosGroup/glTF/blob/master/extensions/2.0/Vendor/EXT_lights_image_based/figures/Cube_map.svg

        #region lifecycle

        public Cube(TMaterial material)
        {
            _Front = _Back = _Left = _Right = _Top = _Bottom = material;
        }

        public Cube(TMaterial material, float width, float height, float length)
        {
            _Front = _Back = _Left = _Right = _Top = _Bottom = material;
            _Size = new Vector3(width, height, length);
        }

        #endregion

        #region data

        private Vector3 _Size = Vector3.One;

        private TMaterial _Front;
        private TMaterial _Back;

        private TMaterial _Left;
        private TMaterial _Right;

        private TMaterial _Top;
        private TMaterial _Bottom;

        #endregion

        #region API

        public override void AddTo(IMeshBuilder<TMaterial> meshBuilder, Matrix4x4 xform)
        {
            var x = Vector3.UnitX * _Size.X * 0.5f;
            var y = Vector3.UnitY * _Size.Y * 0.5f;
            var z = Vector3.UnitZ * _Size.Z * 0.5f;

            _AddCubeFace(meshBuilder.UsePrimitive(_Right), x, y, z, xform);
            _AddCubeFace(meshBuilder.UsePrimitive(_Left), -x, z, y, xform);

            _AddCubeFace(meshBuilder.UsePrimitive(_Top), y, z, x, xform);
            _AddCubeFace(meshBuilder.UsePrimitive(_Bottom), -y, x, z, xform);

            _AddCubeFace(meshBuilder.UsePrimitive(_Front), z, x, y, xform);
            _AddCubeFace(meshBuilder.UsePrimitive(_Back), -z, y, x, xform);
        }

        private static void _AddCubeFace(IPrimitiveBuilder primitiveBuilder, Vector3 origin, Vector3 axisX, Vector3 axisY, Matrix4x4 xform)
        {
            var p1 = Vector3.Transform(origin - axisX - axisY, xform);
            var p2 = Vector3.Transform(origin + axisX - axisY, xform);
            var p3 = Vector3.Transform(origin + axisX + axisY, xform);
            var p4 = Vector3.Transform(origin - axisX + axisY, xform);
            var n = Vector3.Normalize(Vector3.TransformNormal(origin, xform));

            primitiveBuilder.AddQuadrangle
                (
                new VERTEX( (p1, n), (Vector4.One, Vector2.Zero)  ),
                new VERTEX( (p2, n), (Vector4.One, Vector2.UnitX) ),
                new VERTEX( (p3, n), (Vector4.One, Vector2.One)   ),
                new VERTEX( (p4, n), (Vector4.One, Vector2.UnitY) )
                );
        }
        
        #endregion
    }

    class IcoSphere<TMaterial> : ParametricShape<TMaterial>
    {
        #region lifecycle

        public IcoSphere(TMaterial material, float radius = 0.5f)
        {
            _Material = material;
            _Radius = radius;
        }
        
        #endregion

        #region data

        private float _Radius = 0.5f;
        private TMaterial _Material;
        private int _Subdivision = 3;

        #endregion

        #region API        

        public override void AddTo(IMeshBuilder<TMaterial> meshBuilder, Matrix4x4 xform)
        {
            // http://blog.andreaskahler.com/2009/06/creating-icosphere-mesh-in-code.html

            var t = 1 + (float)(Math.Sqrt(5.0) / 2);

            var v0 = new Vector3(-1, t, 0) * _Radius;
            var v1 = new Vector3(1, t, 0) * _Radius;
            var v2 = new Vector3(-1, -t, 0) * _Radius;
            var v3 = new Vector3(1, -t, 0) * _Radius;

            var v4 = new Vector3(0, -1, t) * _Radius;
            var v5 = new Vector3(0, 1, t) * _Radius;
            var v6 = new Vector3(0, -1, -t) * _Radius;
            var v7 = new Vector3(0, 1, -t) * _Radius;

            var v8 = new Vector3(t, 0, -1) * _Radius;
            var v9 = new Vector3(t, 0, 1) * _Radius;
            var v10 = new Vector3(-t, 0, -1) * _Radius;
            var v11 = new Vector3(-t, 0, 1) * _Radius;

            var prim = meshBuilder.UsePrimitive(_Material);

            // 5 faces around point 0
            _AddSphereFace(prim, xform, v0, v11, v5, _Subdivision);
            _AddSphereFace(prim, xform, v0, v5, v1, _Subdivision);
            _AddSphereFace(prim, xform, v0, v1, v7, _Subdivision);
            _AddSphereFace(prim, xform, v0, v7, v10, _Subdivision);
            _AddSphereFace(prim, xform, v0, v10, v11, _Subdivision);

            // 5 adjacent faces
            _AddSphereFace(prim, xform, v1, v5, v9, _Subdivision);
            _AddSphereFace(prim, xform, v5, v11, v4, _Subdivision);
            _AddSphereFace(prim, xform, v11, v10, v2, _Subdivision);
            _AddSphereFace(prim, xform, v10, v7, v6, _Subdivision);
            _AddSphereFace(prim, xform, v7, v1, v8, _Subdivision);

            // 5 faces around point 3
            _AddSphereFace(prim, xform, v3, v9, v4, _Subdivision);
            _AddSphereFace(prim, xform, v3, v4, v2, _Subdivision);
            _AddSphereFace(prim, xform, v3, v2, v6, _Subdivision);
            _AddSphereFace(prim, xform, v3, v6, v8, _Subdivision);
            _AddSphereFace(prim, xform, v3, v8, v9, _Subdivision);

            // 5 adjacent faces
            _AddSphereFace(prim, xform, v4, v9, v5, _Subdivision);
            _AddSphereFace(prim, xform, v2, v4, v11, _Subdivision);
            _AddSphereFace(prim, xform, v6, v2, v10, _Subdivision);
            _AddSphereFace(prim, xform, v8, v6, v7, _Subdivision);
            _AddSphereFace(prim, xform, v9, v8, v1, _Subdivision);
        }

        private static void _AddSphereFace(IPrimitiveBuilder primitiveBuilder, Matrix4x4 xform, Vector3 a, Vector3 b, Vector3 c, int iterations = 0)
        {
            if (iterations <= 0)
            {
                var tt = (a + b + c) / 3.0f;

                var aa = _CreateVertex(a, xform);
                var bb = _CreateVertex(b, xform);
                var cc = _CreateVertex(c, xform);
                primitiveBuilder.AddTriangle(aa, bb, cc);
                return;
            }

            --iterations;

            var ab = Vector3.Normalize(a + b) * a.Length();
            var bc = Vector3.Normalize(b + c) * b.Length();
            var ca = Vector3.Normalize(c + a) * c.Length();

            // central triangle
            _AddSphereFace(primitiveBuilder, xform, ab, bc, ca, iterations);

            // vertex triangles
            _AddSphereFace(primitiveBuilder, xform, a, ab, ca, iterations);
            _AddSphereFace(primitiveBuilder, xform, b, bc, ab, iterations);
            _AddSphereFace(primitiveBuilder, xform, c, ca, bc, iterations);
        }

        private static VERTEX _CreateVertex(Vector3 position, Matrix4x4 xform)
        {
            var v = new VERTEX();

            v.Geometry.Position = Vector3.Transform(position, xform);
            v.Geometry.Normal = Vector3.Normalize(Vector3.TransformNormal(position, xform));
            v.Material.Color = Vector4.One;
            v.Material.TexCoord = Vector2.Zero;

            return v;
        }

        #endregion
    }

    static class SolidMeshUtils
    {
        public static void AddCube<TMaterial>(this IMeshBuilder<TMaterial> meshBuilder, TMaterial material, Matrix4x4 xform)
        {
            var cube = new Cube<TMaterial>(material);

            cube.AddTo(meshBuilder, xform);
        }

        public static void AddSphere<TMaterial>(this IMeshBuilder<TMaterial> meshBuilder, TMaterial material, Single radius, Matrix4x4 xform)
        {
            var sphere = new IcoSphere<TMaterial>(material, radius);

            sphere.AddTo(meshBuilder, xform);
        }

        public static MeshBuilder<VertexPosition, VertexTexture1> CreateTerrainMesh(int width, int length, Func<int,int,float> heightFunction, string terrainColorImagePath)
        {
            // we create a new material to use with the terrain mesh
            var material = new Materials.MaterialBuilder("TerrainMaterial")
                .WithChannelImage(Materials.KnownChannel.BaseColor, terrainColorImagePath);

            // we create a MeshBuilder
            var terrainMesh = new MeshBuilder<VertexPosition, VertexTexture1>("terrain");

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
                        .AddQuadrangle
                        (
                            (a, at),
                            (b, bt),
                            (c, ct),
                            (d, dt)
                        );
                }
            }

            terrainMesh.Validate();

            return terrainMesh;
        }
    }
}
