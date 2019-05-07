using System;
using System.Numerics;

using SharpGLTF.Geometry;
using SharpGLTF.Geometry.VertexTypes;
using SharpGLTF.Materials;
using SharpGLTF.Schema2;

namespace PointCloudGalaxy
{
    using VERTEX = VertexBuilder<VertexPosition, VertexColor1, VertexEmpty>;

    class Program
    {
        static void Main(string[] args)
        {
            var material = new MaterialBuilder("material1").WithUnlitShader();

            var mesh = VERTEX.CreateCompatibleMesh("points");

            // create a point cloud primitive
            var pointCloud = mesh.UsePrimitive(material, 1);

            var galaxy = new Galaxy();

            galaxy.scaleOverPlane = 0.05f;
            galaxy.randomJitter = 0.01f;
            foreach(var startPoint in galaxy.CreateStarts(50000))
            {
                pointCloud.AddPoint((startPoint, Vector4.One));
            }

            galaxy.scaleOverPlane = 0.15f;
            galaxy.randomJitter = 0.02f;
            foreach (var startPoint in galaxy.CreateStarts(50000))
            {
                pointCloud.AddPoint((startPoint, new Vector4(0.4f, 0.8f, 0.7f, 1)));
            }
            
            galaxy.randomJitter = 0.07f;
            foreach (var startPoint in galaxy.CreateStarts(10000))
            {
                pointCloud.AddPoint((startPoint, new Vector4(0.2f, 0.6f, 0.5f, 1)));
            }

            // create a new gltf model
            var model = ModelRoot.CreateModel();

            // add all meshes (just one in this case) to the model
            model.CreateMeshes(mesh);

            // create a scene, a node, and assign the first mesh (the terrain)
            model.UseScene("Default")
                .CreateNode().WithMesh(model.LogicalMeshes[0]);

            // save the model as GLB
            model.SaveGLB("Galaxy.glb");
        }
    }
}
