using System;
using System.Collections.Generic;
using System.Numerics;
using System.Linq;

using NUnit.Framework;

namespace SharpGLTF.Schema2.Authoring
{
    using Geometry;
    using Materials;

    using VEMPTY = Geometry.VertexTypes.VertexEmpty;
    using VPOSNRM = Geometry.VertexTypes.VertexPositionNormal;
    using VPOS = Geometry.VertexTypes.VertexPosition;
    using VSKIN4 = Geometry.VertexTypes.VertexJoints8x4;

    [TestFixture]
    [Category("Model Authoring")]
    public class MeshBuilderCreationTests
    {
        [Test(Description = "Creates an interleaved scene using a mesh builder helper class")]
        public void CreateSceneWithInterleavedMeshBuilder()
        {
            TestContext.CurrentContext.AttachShowDirLink();
            TestContext.CurrentContext.AttachGltfValidatorLink();

            // define 4 vertices
            var v1 = new VPOSNRM(-10, 10, 0, -10, 10, 15);
            var v2 = new VPOSNRM(10, 10, 0, 10, 10, 15);
            var v3 = new VPOSNRM(10, -10, 0, 10, -10, 15);
            var v4 = new VPOSNRM(-10, -10, 0, -10, -10, 15);

            // create a material
            var material1 = new MaterialBuilder("material1").WithChannelParam(KnownChannels.BaseColor, Vector4.One);

            // create model
            var meshBuilder = new MeshBuilder<VPOSNRM>("mesh1");
            
            // add a polygon to the primitive that uses material1 as key.
            meshBuilder.UsePrimitive(material1).AddPolygon(v1, v2, v3, v4);

            // create a gltf scene
            var model = ModelRoot.CreateModel();

            model.UseScene("Default")
                .CreateNode("RootNode")
                .WithMesh( model.CreateMesh(meshBuilder) );

            model.AttachToCurrentTest("result.glb");
            model.AttachToCurrentTest("result.gltf");
        }

        [Test(Description = "Creates a scene with 4 meshes, where the meshes have been initialized so they can share the same vertex and index buffers")]
        public void CreateSceneWithSharedBuffers()
        {
            TestContext.CurrentContext.AttachShowDirLink();
            TestContext.CurrentContext.AttachGltfValidatorLink();

            // create materials
            var material1 = new MaterialBuilder("material1").WithChannelParam(KnownChannels.BaseColor, new Vector4(1, 1, 0, 1));
            var material2 = new MaterialBuilder("material1").WithChannelParam(KnownChannels.BaseColor, new Vector4(1, 0, 1, 1));            

            // create several meshes
            var meshBuilder1 = new MeshBuilder<VPOSNRM>("mesh1");
            var meshBuilder2 = new MeshBuilder<VPOSNRM>("mesh2");
            var meshBuilder3 = new MeshBuilder<VPOSNRM>("mesh3");
            var meshBuilder4 = new MeshBuilder<VPOSNRM>("mesh4");

            meshBuilder1.AddCube(material1, Matrix4x4.Identity);
            meshBuilder2.AddCube(material2, Matrix4x4.Identity);
            meshBuilder3.AddSphere(material1, 0.5f, Matrix4x4.Identity);
            meshBuilder4.AddSphere(material2, 0.5f, Matrix4x4.Identity);

            meshBuilder1.Validate();
            meshBuilder2.Validate();
            meshBuilder3.Validate();
            meshBuilder4.Validate();

            // create the gltf model
            var model = ModelRoot.CreateModel();            

            // create gltf meshes, by doing so in a single call, we ensure a single, shared vertex and index buffer will be used.
            var meshes = model.CreateMeshes(meshBuilder1, meshBuilder2, meshBuilder3, meshBuilder4);

            // create scene nodes
            model.UseScene("Default").CreateNode("Cube1").WithMesh(meshes[0]).WithLocalTranslation(new Vector3(-5, 0, 0));
            model.UseScene("Default").CreateNode("Cube2").WithMesh(meshes[1]).WithLocalTranslation(new Vector3(0, 5, 0));
            model.UseScene("Default").CreateNode("Sphere1").WithMesh(meshes[2]).WithLocalTranslation(new Vector3(+5, 0, 0));
            model.UseScene("Default").CreateNode("Sphere2").WithMesh(meshes[3]).WithLocalTranslation(new Vector3(0, -5, 0));

            // manuall merge all the buffers
            model.MergeBuffers();

            // checks
            Assert.AreEqual(1, model.LogicalBuffers.Count);
            Assert.AreEqual(2, model.LogicalBufferViews.Count);
            Assert.AreEqual(BufferMode.ARRAY_BUFFER, model.LogicalBufferViews[0].DeviceBufferTarget);
            Assert.AreEqual(BufferMode.ELEMENT_ARRAY_BUFFER, model.LogicalBufferViews[1].DeviceBufferTarget);
            Assert.AreEqual(2, model.LogicalMaterials.Count);

            model.AttachToCurrentTest("result.glb");
            model.AttachToCurrentTest("result.gltf");
        }

        [Test(Description = "Creates a node animated scene.")]
        public void CreateSceneWithAnimatedMeshBuilder()
        {
            TestContext.CurrentContext.AttachShowDirLink();
            TestContext.CurrentContext.AttachGltfValidatorLink();

            // create animation sequence with 4 frames
            var keyframes = new Dictionary<Single, Vector3>()
            {
                [1] = new Vector3(0, 0, 0),
                [2] = new Vector3(50, 0, 0),
                [3] = new Vector3(0, 50, 0),
                [4] = new Vector3(0, 0, 0),
            };

            // create a material
            var material1 = new MaterialBuilder("material1").WithChannelParam(KnownChannels.BaseColor, Vector4.One);

            // create a mesh
            var meshBuilder = new MeshBuilder<VPOSNRM>("mesh1");
            meshBuilder.AddCube(material1, Matrix4x4.Identity);
            meshBuilder.Validate();

            // create the gltf model
            var model = ModelRoot.CreateModel();

            model.UseScene("Default")
                .CreateNode("RootNode")
                .WithTranslationAnimation("track1", keyframes)
                .WithMesh(model.CreateMesh(meshBuilder));

            model.AttachToCurrentTest("result.glb");
            model.AttachToCurrentTest("result.gltf");
        }        

        [Test(Description = "Creates a skinned animated scene.")]
        public void CreateSceneWithSkinnedAnimatedMeshBuilder()
        {
            TestContext.CurrentContext.AttachShowDirLink();
            TestContext.CurrentContext.AttachGltfValidatorLink();
            
            // create animation sequence with 4 frames
            var keyframes = new Dictionary<Single, Quaternion>
            {
                [1] = Quaternion.Identity,
                [2] = Quaternion.CreateFromYawPitchRoll(0, 1, 0),
                [3] = Quaternion.CreateFromYawPitchRoll(0, 0, 1),
                [4] = Quaternion.Identity,
            };

            // create two materials
            var pink = new MaterialBuilder("material1").WithChannelParam(KnownChannels.BaseColor, new Vector4(1, 0, 1, 1)).WithDoubleSide(true);
            var yellow = new MaterialBuilder("material2").WithChannelParam(KnownChannels.BaseColor, new Vector4(1, 1, 0, 1)).WithDoubleSide(true);

            // create the mesh
            var meshBuilder = new MeshBuilder<VPOS, VEMPTY, VSKIN4>("mesh1");

            var v1 = (new VPOS(-10, 0, +10), new VSKIN4(0));
            var v2 = (new VPOS(+10, 0, +10), new VSKIN4(0));
            var v3 = (new VPOS(+10, 0, -10), new VSKIN4(0));
            var v4 = (new VPOS(-10, 0, -10), new VSKIN4(0));

            var v5 = (new VPOS(-10, 40, +10), new VSKIN4((0,0.5f), (1, 0.5f)));
            var v6 = (new VPOS(+10, 40, +10), new VSKIN4((0, 0.5f), (1, 0.5f)));
            var v7 = (new VPOS(+10, 40, -10), new VSKIN4((0, 0.5f), (1, 0.5f)));
            var v8 = (new VPOS(-10, 40, -10), new VSKIN4((0, 0.5f), (1, 0.5f)));

            var v9  = (new VPOS(-5, 80, +5), new VSKIN4(2));
            var v10 = (new VPOS(+5, 80, +5), new VSKIN4(2));
            var v11 = (new VPOS(+5, 80, -5), new VSKIN4(2));
            var v12 = (new VPOS(-5, 80, -5), new VSKIN4(2));

            meshBuilder.UsePrimitive(pink).AddPolygon(v1, v2, v6, v5);
            meshBuilder.UsePrimitive(pink).AddPolygon(v2, v3, v7, v6);
            meshBuilder.UsePrimitive(pink).AddPolygon(v3, v4, v8, v7);
            meshBuilder.UsePrimitive(pink).AddPolygon(v4, v1, v5, v8);

            meshBuilder.UsePrimitive(yellow).AddPolygon(v5, v6, v10, v9);
            meshBuilder.UsePrimitive(yellow).AddPolygon(v6, v7, v11, v10);
            meshBuilder.UsePrimitive(yellow).AddPolygon(v7, v8, v12, v11);
            meshBuilder.UsePrimitive(yellow).AddPolygon(v8, v5, v9, v12);

            meshBuilder.Validate();

            // create base model
            var model = ModelRoot.CreateModel();
            var scene = model.UseScene("Default");            

            // create the three joints that will affect the mesh
            var skelet = scene.CreateNode("Skeleton");
            var joint1 = skelet.CreateNode("Joint 1").WithLocalTranslation(new Vector3(0, 0, 0));
            var joint2 = joint1.CreateNode("Joint 2").WithLocalTranslation(new Vector3(0, 40, 0)).WithRotationAnimation("Base Track", keyframes);
            var joint3 = joint2.CreateNode("Joint 3").WithLocalTranslation(new Vector3(0, 40, 0));

            // setup skin
            var snode = scene.CreateNode("Skeleton Node");
            snode.Skin = model.CreateSkin();
            snode.Skin.Skeleton = skelet;
            snode.Skin.BindJoints(joint1, joint2, joint3);

            snode.WithMesh( model.CreateMesh(meshBuilder) );

            model.AttachToCurrentTest("result.glb");
            model.AttachToCurrentTest("result.gltf");
        }

        [Test(Description = "Creates a textured terrain mesh.")]
        public void CreateSceneWithTerrain()
        {
            TestContext.CurrentContext.AttachShowDirLink();
            TestContext.CurrentContext.AttachGltfValidatorLink();

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
        }

        [Test]
        public void CreateSceneWithRandomCubes()
        {
            TestContext.CurrentContext.AttachShowDirLink();
            TestContext.CurrentContext.AttachGltfValidatorLink();

            var rnd = new Random();

            var materials = Enumerable
                .Range(0, 10)
                .Select(idx => new MaterialBuilder()
                .WithChannelParam("BaseColor", new Vector4(rnd.NextVector3(),1)))
                .ToList();

            // create a mesh
            var cubes = new MeshBuilder<VPOSNRM>("cube");

            for(int i=0; i < 100; ++i)
            {
                var r = rnd.NextVector3() * 5;
                var m = materials[rnd.Next(0, 10)];
                var xform = Matrix4x4.CreateFromYawPitchRoll(r.X,r.Y,r.Z) * Matrix4x4.CreateTranslation(rnd.NextVector3() * 25);
                cubes.AddCube(m, xform);
            }

            cubes.Validate();

            // create a new gltf model
            var model = ModelRoot.CreateModel();

            // add all meshes (just one in this case) to the model
            model.CreateMeshes(cubes);

            // create a scene, a node, and assign the first mesh (the terrain)
            model.UseScene("Default")
                .CreateNode().WithMesh(model.LogicalMeshes[0]);

            // save the model as GLB
            model.AttachToCurrentTest("cubes.glb");
        }        
    }
}
