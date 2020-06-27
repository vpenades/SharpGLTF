using System;
using System.Collections.Generic;
using System.Numerics;
using System.Linq;

using NUnit.Framework;

using SharpGLTF.Materials;
using SharpGLTF.Geometry.Parametric;
using SharpGLTF.Schema2;
using SharpGLTF.Scenes;

namespace SharpGLTF.Geometry
{
    using VEMPTY = Geometry.VertexTypes.VertexEmpty;
    using VPOSNRM = Geometry.VertexTypes.VertexPositionNormal;
    using VPOS = Geometry.VertexTypes.VertexPosition;
    using VSKIN4 = Geometry.VertexTypes.VertexJoints4;

    [TestFixture]
    [Category("Model Authoring")]
    public class MeshBuilderAdvancedTests
    {
        [Test(Description = "Creates a scene using a mesh builder helper class")]
        public void CreateSceneWithMeshBuilder()
        {
            TestContext.CurrentContext.AttachShowDirLink();
            TestContext.CurrentContext.AttachGltfValidatorLinks();

            // create a material
            var material1 = new MaterialBuilder("material1")
                .WithChannelParam(KnownChannel.BaseColor, Vector4.One);
            
            // create model
            var meshBuilder = new MeshBuilder<VPOSNRM>("mesh1");
            meshBuilder.VertexPreprocessor.SetDebugPreprocessors();

            // define 4 vertices
            var v1 = new VPOSNRM(-10, 10, 0, 0, 0, 1);
            var v2 = new VPOSNRM(10, 10, 0, 0, 0, 1);
            var v3 = new VPOSNRM(10, -10, 0, 0, 0, 1);
            var v4 = new VPOSNRM(-10, -10, 0, 0, 0, 1);

            // add a polygon to the primitive that uses material1 as key.
            meshBuilder.UsePrimitive(material1).AddQuadrangle(v1, v2, v3, v4);

            // create a scene
            var scene = new SceneBuilder();

            scene.AddRigidMesh(meshBuilder, Matrix4x4.Identity);

            scene.AttachToCurrentTest("result.glb");
            scene.AttachToCurrentTest("result.gltf");
            scene.AttachToCurrentTest("result.plotly");
        }

        [Test(Description = "Creates a scene with 4 meshes, where the meshes have been initialized so they can share the same vertex and index buffers")]
        public void CreateSceneWithSharedBuffers()
        {
            TestContext.CurrentContext.AttachShowDirLink();
            TestContext.CurrentContext.AttachGltfValidatorLinks();

            // create materials
            var material1 = new MaterialBuilder("material1")
                .WithChannelParam(KnownChannel.BaseColor, new Vector4(1, 1, 0, 1));

            var material2 = new MaterialBuilder("material1")
                .WithChannelParam(KnownChannel.BaseColor, new Vector4(1, 0, 1, 1));            

            // create several meshes
            var meshBuilder1 = new MeshBuilder<VPOSNRM>("mesh1");
                meshBuilder1.VertexPreprocessor.SetDebugPreprocessors();
            var meshBuilder2 = new MeshBuilder<VPOSNRM>("mesh2");
                meshBuilder2.VertexPreprocessor.SetDebugPreprocessors();
            var meshBuilder3 = new MeshBuilder<VPOSNRM>("mesh3");
                meshBuilder3.VertexPreprocessor.SetDebugPreprocessors();
            var meshBuilder4 = new MeshBuilder<VPOSNRM>("mesh4");
                meshBuilder4.VertexPreprocessor.SetDebugPreprocessors();

            meshBuilder1.AddCube(material1, Matrix4x4.Identity);
            meshBuilder2.AddCube(material2, Matrix4x4.Identity);
            meshBuilder3.AddSphere(material1, 0.5f, Matrix4x4.Identity);
            meshBuilder4.AddSphere(material2, 0.5f, Matrix4x4.Identity);

            meshBuilder1.Validate();
            meshBuilder2.Validate();
            meshBuilder3.Validate();
            meshBuilder4.Validate();

            // create scene nodes
            var pivot1 = new NodeBuilder("Cube1").WithLocalTranslation(new Vector3(-5, 0, 0));
            var pivot2 = new NodeBuilder("Cube2").WithLocalTranslation(new Vector3(0, 5, 0));
            var pivot3 = new NodeBuilder("SPhere1").WithLocalTranslation(new Vector3(+5, 0, 0));
            var pivot4 = new NodeBuilder("SPhere2").WithLocalTranslation(new Vector3(0, -5, 0));

            // create the scene:

            var scene = new SceneBuilder();
            scene.AddRigidMesh(meshBuilder1, pivot1);
            scene.AddRigidMesh(meshBuilder2, pivot2);
            scene.AddRigidMesh(meshBuilder3, pivot3);
            scene.AddRigidMesh(meshBuilder4, pivot4);

            // convert to gltf2

            var model = scene.ToGltf2();

            model.MergeBuffers();

            // checks
            Assert.AreEqual(1, model.LogicalBuffers.Count);
            Assert.AreEqual(2, model.LogicalBufferViews.Count);
            Assert.IsTrue(model.LogicalBufferViews[0].IsVertexBuffer);
            Assert.IsTrue(model.LogicalBufferViews[1].IsIndexBuffer);
            Assert.AreEqual(2, model.LogicalMaterials.Count);

            model.AttachToCurrentTest("result.glb");
            model.AttachToCurrentTest("result.gltf");
            scene.AttachToCurrentTest("result.plotly");
        }

        [Test(Description = "Creates a node animated scene.")]
        public void CreateSceneWithAnimatedMeshBuilder()
        {
            TestContext.CurrentContext.AttachShowDirLink();
            TestContext.CurrentContext.AttachGltfValidatorLinks();            

            // create a material
            var material1 = new MaterialBuilder("material1").WithChannelParam(KnownChannel.BaseColor, Vector4.One);

            // create a mesh
            var meshBuilder = new MeshBuilder<VPOSNRM>("mesh1");
            meshBuilder.VertexPreprocessor.SetDebugPreprocessors();

            meshBuilder.AddCube(material1, Matrix4x4.Identity);
            meshBuilder.Validate();

            // create an animated node
            
            var keyframes = new Dictionary<Single, Vector3>()
            {
                [1] = new Vector3(0, 0, 0),
                [2] = new Vector3(50, 0, 0),
                [3] = new Vector3(0, 50, 0),
                [4] = new Vector3(0, 0, 0),
            };

            var pivot = new NodeBuilder("RootNode").WithLocalTranslation("track1", keyframes);

            // create scene

            var scene = new SceneBuilder();
            scene.AddRigidMesh(meshBuilder, pivot);

            scene.AttachToCurrentTest("result.glb");
            scene.AttachToCurrentTest("result.gltf");
        }        

        [Test(Description = "Creates a skinned animated scene.")]
        public void CreateSceneWithSkinnedAnimatedMeshBuilder()
        {
            TestContext.CurrentContext.AttachShowDirLink();
            TestContext.CurrentContext.AttachGltfValidatorLinks();
            
            // create animation sequence with 4 frames
            var keyframes = new Dictionary<Single, Quaternion>
            {
                [1] = Quaternion.Identity,
                [2] = Quaternion.CreateFromYawPitchRoll(0, 1, 0),
                [3] = Quaternion.CreateFromYawPitchRoll(0, 0, 1),
                [4] = Quaternion.Identity,
            };

            // create two materials
            var pink = new MaterialBuilder("material1")
                .WithChannelParam(KnownChannel.BaseColor, new Vector4(1, 0, 1, 1))
                .WithDoubleSide(true);

            var yellow = new MaterialBuilder("material2")
                .WithChannelParam(KnownChannel.BaseColor, new Vector4(1, 1, 0, 1))
                .WithDoubleSide(true);

            // create the mesh
            var meshBuilder = new MeshBuilder<VPOS, VEMPTY, VSKIN4>("mesh1");

            #if DEBUG
            meshBuilder.VertexPreprocessor.SetDebugPreprocessors();
            #else
            meshBuilder.VertexPreprocessor.SetSanitizerPreprocessors();
            #endif

            const int jointIdx0 = 0;
            const int jointIdx1 = 1;
            const int jointIdx2 = 2;

            var v1 = (new VPOS(-10, 0, +10), new VSKIN4(jointIdx0));
            var v2 = (new VPOS(+10, 0, +10), new VSKIN4(jointIdx0));
            var v3 = (new VPOS(+10, 0, -10), new VSKIN4(jointIdx0));
            var v4 = (new VPOS(-10, 0, -10), new VSKIN4(jointIdx0));

            var v5 = (new VPOS(-10, 40, +10), new VSKIN4((jointIdx0, 0.5f), (jointIdx1, 0.5f)));
            var v6 = (new VPOS(+10, 40, +10), new VSKIN4((jointIdx0, 0.5f), (jointIdx1, 0.5f)));
            var v7 = (new VPOS(+10, 40, -10), new VSKIN4((jointIdx0, 0.5f), (jointIdx1, 0.5f)));
            var v8 = (new VPOS(-10, 40, -10), new VSKIN4((jointIdx0, 0.5f), (jointIdx1, 0.5f)));

            var v9  = (new VPOS(-5, 80, +5), new VSKIN4(jointIdx2));
            var v10 = (new VPOS(+5, 80, +5), new VSKIN4(jointIdx2));
            var v11 = (new VPOS(+5, 80, -5), new VSKIN4(jointIdx2));
            var v12 = (new VPOS(-5, 80, -5), new VSKIN4(jointIdx2));

            meshBuilder.UsePrimitive(pink).AddQuadrangle(v1, v2, v6, v5);
            meshBuilder.UsePrimitive(pink).AddQuadrangle(v2, v3, v7, v6);
            meshBuilder.UsePrimitive(pink).AddQuadrangle(v3, v4, v8, v7);
            meshBuilder.UsePrimitive(pink).AddQuadrangle(v4, v1, v5, v8);

            meshBuilder.UsePrimitive(yellow).AddQuadrangle(v5, v6, v10, v9);
            meshBuilder.UsePrimitive(yellow).AddQuadrangle(v6, v7, v11, v10);
            meshBuilder.UsePrimitive(yellow).AddQuadrangle(v7, v8, v12, v11);
            meshBuilder.UsePrimitive(yellow).AddQuadrangle(v8, v5, v9, v12);

            meshBuilder.Validate();

            // create base model
            var model = ModelRoot.CreateModel();
            var scene = model.UseScene("Default");            

            // create the three joints that will affect the mesh
            var skelet = scene.CreateNode("Skeleton");
            var joint0 = skelet.CreateNode("Joint 0").WithLocalTranslation(new Vector3(0, 0, 0));
            var joint1 = joint0.CreateNode("Joint 1").WithLocalTranslation(new Vector3(0, 40, 0)).WithRotationAnimation("Base Track", keyframes);
            var joint2 = joint1.CreateNode("Joint 2").WithLocalTranslation(new Vector3(0, 40, 0));

            // setup skin
            var snode = scene.CreateNode("Skeleton Node");
            snode.Skin = model.CreateSkin();            
            snode.Skin.BindJoints(joint0, joint1, joint2);

            snode.WithMesh( model.CreateMesh(meshBuilder) );

            model.AttachToCurrentTest("result.glb");
            model.AttachToCurrentTest("result.gltf");
        }

        [Test(Description = "Creates a textured terrain mesh.")]
        public void CreateSceneWithTerrain()
        {
            TestContext.CurrentContext.AttachShowDirLink();
            TestContext.CurrentContext.AttachGltfValidatorLinks();

            // texture path
            var imagePath = System.IO.Path.Combine(TestContext.CurrentContext.WorkDirectory, "Assets", "Texture1.jpg");

            // fancy height function; can be easily replaced with a bitmap sampler.
            float heightFunction(int xx, int yy)
            {
                float x = xx;
                float y = yy;

                double h = 0;

                h += Math.Sin(x / 45);
                h += Math.Sin(3 + x / 13) * 0.5f;

                h += Math.Sin(2 + y / 31);
                h += Math.Sin(y / 13) * 0.5f;

                h += Math.Sin((x + y * 2) / 19);

                h *= 5;

                return (float)h;
            }

            var terrain = SolidMeshUtils.CreateTerrainMesh(128,128, heightFunction, imagePath);

            // create a new gltf model
            var model = ModelRoot.CreateModel();
            
            // add all meshes (just one in this case) to the model
            model.CreateMeshes(terrain);

            // create a scene, a node, and assign the first mesh (the terrain)
            model.UseScene("Default")
                .CreateNode().WithMesh(model.LogicalMeshes[0]);

            // save the model as GLB
            model.AttachToCurrentTest("terrain.glb");
            model.AttachToCurrentTest("result.plotly");
        }

        [Test(Description = "Creates a scene with 1 million points cloud.")]        
        public void CreateSceneWithPointCloud()
        {
            TestContext.CurrentContext.AttachShowDirLink();
            TestContext.CurrentContext.AttachGltfValidatorLinks();

            var material = new MaterialBuilder("material1").WithUnlitShader();            

            var mesh = new MeshBuilder<VPOS, Geometry.VertexTypes.VertexColor1>("points");
            mesh.VertexPreprocessor.SetDebugPreprocessors();

            // create a point cloud primitive
            var pointCloud = mesh.UsePrimitive(material, 1);

            var rnd = new Random(178);
            for (int i = 0; i < 1000000; ++i)
            {
                var x = (float)(rnd.NextDouble() * 2 - 1);
                var y = (float)(rnd.NextDouble() * 2 - 1);
                var z = (float)(rnd.NextDouble() * 2 - 1);

                var opacity = Math.Max(Math.Max(Math.Abs(x), Math.Abs(y)), Math.Abs(z));

                opacity = opacity * opacity * opacity * opacity;

                var r = (float)rnd.NextDouble() * opacity;
                var g = (float)rnd.NextDouble() * opacity;
                var b = (float)rnd.NextDouble() * opacity;

                x *= 50;
                y *= 50;
                z *= 50;

                pointCloud.AddPoint((new Vector3(x, y + 60, z), new Vector4(r, g, b, 1)));
            }

            // adds 4 lines as the base of the points
            mesh.UsePrimitive(material, 2).AddLine((new Vector3(-50, 0, -50), Vector4.One), (new Vector3(+50, 0, -50), Vector4.UnitW));
            mesh.UsePrimitive(material, 2).AddLine((new Vector3(+50, 0, -50), Vector4.One), (new Vector3(+50, 0, +50), Vector4.UnitW));
            mesh.UsePrimitive(material, 2).AddLine((new Vector3(+50, 0, +50), Vector4.One), (new Vector3(-50, 0, +50), Vector4.UnitW));
            mesh.UsePrimitive(material, 2).AddLine((new Vector3(-50, 0, +50), Vector4.One), (new Vector3(-50, 0, -50), Vector4.UnitW));

            // create a new gltf model
            var model = ModelRoot.CreateModel();

            // add all meshes (just one in this case) to the model
            model.CreateMeshes(mesh);

            // create a scene, a node, and assign the first mesh (the terrain)
            model.UseScene("Default")
                .CreateNode().WithMesh(model.LogicalMeshes[0]);

            // save the model as GLB
            model.AttachToCurrentTest("PointCloud.glb");
        }

        [Test(Description ="Creates a single mesh with multiple cubes.")]
        public void CreateMeshWithRandomCubes()
        {
            TestContext.CurrentContext.AttachShowDirLink();
            TestContext.CurrentContext.AttachGltfValidatorLinks();

            var rnd = new Random();

            var materials = Enumerable
                .Range(0, 10)
                .Select(idx => MaterialBuilder.CreateDefault()
                .WithChannelParam("BaseColor", new Vector4(rnd.NextVector3(),1)))
                .ToList();

            // create a mesh
            var cubes = new MeshBuilder<VPOSNRM>("cube");
            cubes.VertexPreprocessor.SetDebugPreprocessors();

            for (int i=0; i < 100; ++i)
            {
                var r = rnd.NextVector3() * 5;
                var m = materials[rnd.Next(0, 10)];
                var xform = Matrix4x4.CreateFromYawPitchRoll(r.X,r.Y,r.Z) * Matrix4x4.CreateTranslation(rnd.NextVector3() * 25);
                cubes.AddCube(m, xform);
            }

            cubes.Validate();

            var scene = new SceneBuilder();

            scene.AddRigidMesh(cubes, Matrix4x4.Identity);

            // save the model as GLB
            scene.AttachToCurrentTest("cubes.glb");
            scene.AttachToCurrentTest("cubes.gltf");
            scene.AttachToCurrentTest("cubes.plotly");
        }

        [Test(Description ="Simulates animating mesh visibility by setting scale to (0,0,0)")]
        public void CreateSceneWithAnimatedVisibility()
        {
            TestContext.CurrentContext.AttachShowDirLink();
            TestContext.CurrentContext.AttachGltfValidatorLinks();            

            // create a mesh
            var cube = new MeshBuilder<VPOSNRM>("cube");
            cube.VertexPreprocessor.SetDebugPreprocessors();
            cube.AddCube(MaterialBuilder.CreateDefault(), Matrix4x4.Identity);
            cube.Validate();
            
            // create a node and animate it
            var pivot = new NodeBuilder();

            var flatx = new Vector3(0, 1, 1);
            var flaty = new Vector3(1, 0, 1);
            var flatz = new Vector3(1, 1, 0);

            pivot.UseScale("Track1")
                .WithPoint(0, Vector3.One)
                .WithPoint(1 - 0.0001f, Vector3.One)
                .WithPoint(1, Vector3.Zero)
                .WithPoint(2 - 0.0001f, Vector3.Zero)
                .WithPoint(2, Vector3.One)
                .WithPoint(3, Vector3.One)
                .WithPoint(4, -Vector3.One)
                .WithPoint(5, -Vector3.One)
                .WithPoint(6, Vector3.One)
                .WithPoint(7, flatx)
                .WithPoint(8, flatx)
                .WithPoint(9, flaty)
                .WithPoint(10, flaty)
                .WithPoint(11, flatz)
                .WithPoint(12, flatz)
                .WithPoint(13, Vector3.One);

            // create the scene
            var scene = new SceneBuilder();

            scene.AddRigidMesh(cube, pivot);

            // save the model
            scene.AttachToCurrentTest("animatedvisibility.glb");
            scene.AttachToCurrentTest("animatedvisibility.gltf");
        }
    }
}
