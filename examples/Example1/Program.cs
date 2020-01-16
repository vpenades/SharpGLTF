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
            prim.AddQuadrangle(new VERTEX(-5, 0, 3), new VERTEX(0, -5, 3), new VERTEX(5, 0, 3), new VERTEX(0, 5, 3));

            // create a scene

            var scene = new SharpGLTF.Scenes.SceneBuilder();

            scene.AddRigidMesh(mesh, Matrix4x4.Identity);

            // save the model in different formats

            var model = scene.ToGltf2();
            model.SaveAsWavefront("mesh.obj");
            model.SaveGLB("mesh.glb");
            model.SaveGLTF("mesh.gltf");
        }
    }
}
