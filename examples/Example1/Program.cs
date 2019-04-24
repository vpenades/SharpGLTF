using System;
using System.Numerics;

using SharpGLTF.Geometry;
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
            prim.AddPolygon(new VERTEX(-5, 0, 3), new VERTEX(0, -5, 3), new VERTEX(5, 0, 3), new VERTEX(0, 5, 3));
            
            // create a new gltf model
            var model = ModelRoot.CreateModel();

            // add all meshes (just one in this case) to the model
            model.CreateMeshes(mesh);

            // create a scene, a node, and assign the first mesh
            model.UseScene("Default")
                .CreateNode()
                .WithMesh(model.LogicalMeshes[0]);

            // save the model in different formats
            model.SaveAsWavefront("mesh.obj");
            model.SaveGLB("mesh.glb");
            model.SaveGLTF("mesh.gltf");
        }
    }
}
