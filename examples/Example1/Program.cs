using System;
using System.Numerics;

using SharpGLTF.Geometry;
using SharpGLTF.Geometry.VertexTypes;
using SharpGLTF.Materials;
using SharpGLTF.Schema2;

namespace Example1
{
    using VERTEX = SharpGLTF.Geometry.VertexTypes.VertexPosition;

    class Program
    {
        static void Main(string[] args)
        {
            // create two materials

            var material1 = new MaterialBuilder()
                .WithDoubleSide(true)
                .WithMetallicRoughnessShader()
                .WithChannelParam("BaseColor", new Vector4(1,0,0,1) );

            var material2 = new MaterialBuilder()
                .WithDoubleSide(true)
                .WithMetallicRoughnessShader()
                .WithChannelParam("BaseColor", new Vector4(1, 0, 1, 1));

            // create a mesh with two primitives, one for each material

            var mesh = new MeshBuilder<VERTEX>("mesh");

            var prim = mesh.UsePrimitive(material1);
            prim.AddTriangle(new VERTEX(-10, 0, 0), new VERTEX(10, 0, 0), new VERTEX(0, 10, 0));
            prim.AddTriangle(new VERTEX(10, 0, 0), new VERTEX(-10, 0, 0), new VERTEX(0, -10, 0));

            prim = mesh.UsePrimitive(material2);
            prim.AddConvexPolygon(new VERTEX(-5, 0, 3), new VERTEX(0, -5, 3), new VERTEX(5, 0, 3), new VERTEX(0, 5, 3));

            // create a scene

            var scene = new SharpGLTF.Scenes.SceneBuilder();

            scene.AddMesh(mesh, Matrix4x4.Identity);

            // save the model in different formats

            var model = scene.ToSchema2();
            model.SaveAsWavefront("mesh.obj");
            model.SaveGLB("mesh.glb");
            model.SaveGLTF("mesh.gltf");
        }
    }

    static class ToolkitUtils
    {
        public static void AddConvexPolygon<TMaterial, TvG, TvM, TvS>(this PrimitiveBuilder<TMaterial, TvG, TvM, TvS> primitive, params VertexBuilder<TvG, TvM, TvS>[] vertices)
        where TvG : struct, IVertexGeometry
        where TvM : struct, IVertexMaterial
        where TvS : struct, IVertexSkinning
        {
            for (int i = 2; i < vertices.Length; ++i)
            {
                var a = vertices[0];
                var b = vertices[i - 1];
                var c = vertices[i];

                primitive.AddTriangle(a, b, c);
            }
        }
    }
}
