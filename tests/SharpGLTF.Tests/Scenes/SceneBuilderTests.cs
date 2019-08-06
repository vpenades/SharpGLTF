using System;
using System.Linq;
using System.Numerics;

using NUnit.Framework;

using SharpGLTF.Geometry;
using SharpGLTF.Geometry.VertexTypes;
using SharpGLTF.Geometry.Parametric;
using SharpGLTF.Materials;

namespace SharpGLTF.Scenes
{
    using VPOSNRM = VertexBuilder<VertexPositionNormal, VertexEmpty, VertexEmpty>;

    using SKINNEDVERTEX = VertexBuilder<VertexPosition, VertexEmpty, VertexJoints8x4>;


    [Category("Toolkit.Scenes")]
    public class SceneBuilderTests
    {
        [Test]
        public void CreateCubeScene()
        {
            TestContext.CurrentContext.AttachShowDirLink();
            TestContext.CurrentContext.AttachGltfValidatorLinks();

            var mesh = new Cube<MaterialBuilder>(new MaterialBuilder())
                .ToMesh(Matrix4x4.Identity);

            var scene = new SceneBuilder();

            scene.AddMesh(mesh, Matrix4x4.Identity);

            scene.AttachToCurrentTest("cube.glb");
        }

        [Test]
        public void CreateAnimatedCubeScene()
        {
            TestContext.CurrentContext.AttachShowDirLink();
            TestContext.CurrentContext.AttachGltfValidatorLinks();

            var mesh = new Cube<MaterialBuilder>(MaterialBuilder.CreateDefault())
                .ToMesh(Matrix4x4.Identity);

            var pivot = new NodeBuilder();

            pivot.UseTranslation("track1")
                .WithPoint(0, Vector3.Zero)
                .WithPoint(1, Vector3.One);

            pivot.UseRotation("track1")
                .WithPoint(0, Quaternion.Identity)
                .WithPoint(1, Quaternion.CreateFromAxisAngle(Vector3.UnitY, 1.5f));

            pivot.UseScale("track1")
                .WithPoint(0, Vector3.One)
                .WithPoint(1, new Vector3(0.5f));            

            var scene = new SceneBuilder();            

            scene.AddMesh(mesh, pivot);

            scene.AttachToCurrentTest("animated.glb");
            scene.AttachToCurrentTest("animated.gltf");
        }

        [Test]
        public void CreateSceneWithRandomShapes()
        {
            TestContext.CurrentContext.AttachShowDirLink();
            TestContext.CurrentContext.AttachGltfValidatorLinks();

            var rnd = new Random();

            // create materials
            var materials = Enumerable
                .Range(0, 10)
                .Select(idx => new Materials.MaterialBuilder()
                .WithChannelParam("BaseColor", new Vector4(rnd.NextVector3(), 1)))
                .ToList();
            
            // create scene            

            var scene = new SceneBuilder();

            for (int i = 0; i < 100; ++i)
            {
                // create mesh
                var mat = materials[rnd.Next(0, 10)];
                var mesh = VPOSNRM.CreateCompatibleMesh("shape");

                #if DEBUG
                mesh.VertexPreprocessor.SetDebugPreprocessors();
                #else
                mesh.VertexPreprocessor.SetSanitizerPreprocessors();
                #endif

                if ((i & 1) == 0) mesh.AddCube(mat, Matrix4x4.Identity);
                else mesh.AddSphere(mat, 0.5f, Matrix4x4.Identity);

                mesh.Validate();

                // create random transform
                var r = rnd.NextVector3() * 5;
                var xform = Matrix4x4.CreateFromYawPitchRoll(r.X, r.Y, r.Z) * Matrix4x4.CreateTranslation(rnd.NextVector3() * 25);

                scene.AddMesh(mesh, xform);                
            }

            // save the model as GLB

            scene.AttachToCurrentTest("shapes.glb");
        }

        [Test]
        public void CreateSkinnedScene()
        {
            TestContext.CurrentContext.AttachShowDirLink();
            TestContext.CurrentContext.AttachGltfValidatorLinks();
            
            // create two materials

            var pink = new MaterialBuilder("material1")
                .WithChannelParam(KnownChannels.BaseColor, new Vector4(1, 0, 1, 1))
                .WithDoubleSide(true);

            var yellow = new MaterialBuilder("material2")
                .WithChannelParam(KnownChannels.BaseColor, new Vector4(1, 1, 0, 1))
                .WithDoubleSide(true);

            // create the mesh            

            const int jointIdx0 = 0; // index of joint node 0
            const int jointIdx1 = 1; // index of joint node 1
            const int jointIdx2 = 2; // index of joint node 2

            var v1 = new SKINNEDVERTEX(new Vector3(-10, 0, +10), (jointIdx0, 1));
            var v2 = new SKINNEDVERTEX(new Vector3(+10, 0, +10), (jointIdx0, 1));
            var v3 = new SKINNEDVERTEX(new Vector3(+10, 0, -10), (jointIdx0, 1));
            var v4 = new SKINNEDVERTEX(new Vector3(-10, 0, -10), (jointIdx0, 1));

            var v5 = new SKINNEDVERTEX(new Vector3(-10, 40, +10), (jointIdx0, 0.5f), (jointIdx1, 0.5f));
            var v6 = new SKINNEDVERTEX(new Vector3(+10, 40, +10), (jointIdx0, 0.5f), (jointIdx1, 0.5f));
            var v7 = new SKINNEDVERTEX(new Vector3(+10, 40, -10), (jointIdx0, 0.5f), (jointIdx1, 0.5f));
            var v8 = new SKINNEDVERTEX(new Vector3(-10, 40, -10), (jointIdx0, 0.5f), (jointIdx1, 0.5f));

            var v9  = new SKINNEDVERTEX(new Vector3(-5, 80, +5), (jointIdx2, 1));
            var v10 = new SKINNEDVERTEX(new Vector3(+5, 80, +5), (jointIdx2, 1));
            var v11 = new SKINNEDVERTEX(new Vector3(+5, 80, -5), (jointIdx2, 1));
            var v12 = new SKINNEDVERTEX(new Vector3(-5, 80, -5), (jointIdx2, 1));

            var mesh = SKINNEDVERTEX.CreateCompatibleMesh("mesh1");

            #if DEBUG
            mesh.VertexPreprocessor.SetDebugPreprocessors();
            #else
            mesh.VertexPreprocessor.SetSanitizerPreprocessors();
            #endif

            mesh.UsePrimitive(pink).AddConvexPolygon(v1, v2, v6, v5);
            mesh.UsePrimitive(pink).AddConvexPolygon(v2, v3, v7, v6);
            mesh.UsePrimitive(pink).AddConvexPolygon(v3, v4, v8, v7);
            mesh.UsePrimitive(pink).AddConvexPolygon(v4, v1, v5, v8);

            mesh.UsePrimitive(yellow).AddConvexPolygon(v5, v6, v10, v9);
            mesh.UsePrimitive(yellow).AddConvexPolygon(v6, v7, v11, v10);
            mesh.UsePrimitive(yellow).AddConvexPolygon(v7, v8, v12, v11);
            mesh.UsePrimitive(yellow).AddConvexPolygon(v8, v5, v9, v12);

            mesh.Validate();
            
            // create the skeleton armature for the skinned mesh.

            var armature = new NodeBuilder("Skeleton");
            var joint0 = armature.CreateNode("Joint 0").WithLocalTranslation(new Vector3(0, 0, 0)); // jointIdx0
            var joint1 = joint0.CreateNode("Joint 1").WithLocalTranslation(new Vector3(0, 40, 0));  // jointIdx1
            var joint2 = joint1.CreateNode("Joint 2").WithLocalTranslation(new Vector3(0, 40, 0));  // jointIdx2

            joint1.UseRotation("Base Track")
                .WithPoint(1, Quaternion.Identity)
                .WithPoint(2, Quaternion.CreateFromYawPitchRoll(0, 1, 0))
                .WithPoint(3, Quaternion.CreateFromYawPitchRoll(0, 0, 1))
                .WithPoint(4, Quaternion.Identity);

            // create scene

            var scene = new SceneBuilder();

            scene.AddSkinnedMesh
                (
                mesh,
                Matrix4x4.Identity,
                joint0, // joint used for skinning joint index 0
                joint1, // joint used for skinning joint index 1
                joint2  // joint used for skinning joint index 2
                );

            scene.AttachToCurrentTest("skinned.glb");
            scene.AttachToCurrentTest("skinned.gltf");
        }

        [Test]
        public void CreateDoubleSkinnedScene()
        {
            TestContext.CurrentContext.AttachShowDirLink();
            TestContext.CurrentContext.AttachGltfValidatorLinks();

            // create two materials

            var pink = new MaterialBuilder("material1")
                .WithChannelParam(KnownChannels.BaseColor, new Vector4(1, 0, 1, 1))
                .WithDoubleSide(true);

            var yellow = new MaterialBuilder("material2")
                .WithChannelParam(KnownChannels.BaseColor, new Vector4(1, 1, 0, 1))
                .WithDoubleSide(true);

            // create the mesh            

            const int jointIdx0 = 0; // index of joint node 0
            const int jointIdx1 = 1; // index of joint node 1
            const int jointIdx2 = 2; // index of joint node 2

            var v1 = new SKINNEDVERTEX(new Vector3(-10, 0, +10), (jointIdx0, 1));
            var v2 = new SKINNEDVERTEX(new Vector3(+10, 0, +10), (jointIdx0, 1));
            var v3 = new SKINNEDVERTEX(new Vector3(+10, 0, -10), (jointIdx0, 1));
            var v4 = new SKINNEDVERTEX(new Vector3(-10, 0, -10), (jointIdx0, 1));

            var v5 = new SKINNEDVERTEX(new Vector3(-10, 40, +10), (jointIdx0, 0.5f), (jointIdx1, 0.5f));
            var v6 = new SKINNEDVERTEX(new Vector3(+10, 40, +10), (jointIdx0, 0.5f), (jointIdx1, 0.5f));
            var v7 = new SKINNEDVERTEX(new Vector3(+10, 40, -10), (jointIdx0, 0.5f), (jointIdx1, 0.5f));
            var v8 = new SKINNEDVERTEX(new Vector3(-10, 40, -10), (jointIdx0, 0.5f), (jointIdx1, 0.5f));

            var v9 = new SKINNEDVERTEX(new Vector3(-5, 80, +5), (jointIdx2, 1));
            var v10 = new SKINNEDVERTEX(new Vector3(+5, 80, +5), (jointIdx2, 1));
            var v11 = new SKINNEDVERTEX(new Vector3(+5, 80, -5), (jointIdx2, 1));
            var v12 = new SKINNEDVERTEX(new Vector3(-5, 80, -5), (jointIdx2, 1));

            var mesh = SKINNEDVERTEX.CreateCompatibleMesh("mesh1");

            #if DEBUG
            mesh.VertexPreprocessor.SetDebugPreprocessors();
            #else
            mesh.VertexPreprocessor.SetSanitizerPreprocessors();
            #endif

            mesh.UsePrimitive(pink).AddConvexPolygon(v1, v2, v6, v5);
            mesh.UsePrimitive(pink).AddConvexPolygon(v2, v3, v7, v6);
            mesh.UsePrimitive(pink).AddConvexPolygon(v3, v4, v8, v7);
            mesh.UsePrimitive(pink).AddConvexPolygon(v4, v1, v5, v8);

            mesh.UsePrimitive(yellow).AddConvexPolygon(v5, v6, v10, v9);
            mesh.UsePrimitive(yellow).AddConvexPolygon(v6, v7, v11, v10);
            mesh.UsePrimitive(yellow).AddConvexPolygon(v7, v8, v12, v11);
            mesh.UsePrimitive(yellow).AddConvexPolygon(v8, v5, v9, v12);

            mesh.Validate();

            // create the skeleton armature 1 for the skinned mesh.

            var armature1 = new NodeBuilder("Skeleton1");
            var joint0 = armature1.CreateNode("Joint 0").WithLocalTranslation(new Vector3(0, 0, 0)); // jointIdx0
            var joint1 = joint0.CreateNode("Joint 1").WithLocalTranslation(new Vector3(0, 40, 0));  // jointIdx1
            var joint2 = joint1.CreateNode("Joint 2").WithLocalTranslation(new Vector3(0, 40, 0));  // jointIdx2

            joint1.UseRotation("Base Track")
                .WithPoint(1, Quaternion.Identity)
                .WithPoint(2, Quaternion.CreateFromYawPitchRoll(0, 1, 0))
                .WithPoint(3, Quaternion.CreateFromYawPitchRoll(0, 0, 1))
                .WithPoint(4, Quaternion.Identity);

            // create the skeleton armature 2 for the skinned mesh.

            var armature2 = new NodeBuilder("Skeleton2").WithLocalTranslation(new Vector3(100,0,0));
            var joint3 = armature2.CreateNode("Joint 3").WithLocalTranslation(new Vector3(0, 0, 0)); // jointIdx0
            var joint4 = joint3.CreateNode("Joint 4").WithLocalTranslation(new Vector3(0, 40, 0));  // jointIdx1
            var joint5 = joint4.CreateNode("Joint 5").WithLocalTranslation(new Vector3(0, 40, 0));  // jointIdx2

            joint4.UseRotation("Base Track")
                .WithPoint(1, Quaternion.Identity)
                .WithPoint(2, Quaternion.CreateFromYawPitchRoll(0, 1, 0))
                .WithPoint(3, Quaternion.CreateFromYawPitchRoll(0, 0, 1))
                .WithPoint(4, Quaternion.Identity);

            // create scene

            var scene = new SceneBuilder();

            scene.AddSkinnedMesh
                (
                mesh,
                armature1.WorldMatrix,
                joint0, // joint used for skinning joint index 0
                joint1, // joint used for skinning joint index 1
                joint2  // joint used for skinning joint index 2
                );

            scene.AddSkinnedMesh
                (
                mesh,
                armature2.WorldMatrix,
                joint3, // joint used for skinning joint index 0
                joint4, // joint used for skinning joint index 1
                joint5  // joint used for skinning joint index 2
                );

            scene.AttachToCurrentTest("skinned.glb");
            scene.AttachToCurrentTest("skinned.gltf");
        }


        [TestCase("Avocado.glb")]
        [TestCase("RiggedFigure.glb")]
        [TestCase("RiggedSimple.glb")]
        [TestCase("BoxAnimated.glb")]
        [TestCase("AnimatedMorphCube.glb")]
        [TestCase("AnimatedMorphSphere.glb")]
        [TestCase("CesiumMan.glb")]
        [TestCase("Monster.glb")]
        [TestCase("BrainStem.glb")]
        public void TestRoundTrip(string path)
        {
            TestContext.CurrentContext.AttachShowDirLink();

            path = TestFiles
                .GetSampleModelsPaths()
                .FirstOrDefault(item => item.Contains(path));

            var modelSrc = Schema2.ModelRoot.Load(path);
            Assert.NotNull(modelSrc);

            // perform roundtrip

            var scene = Schema2.Schema2Toolkit.ToSceneBuilder(modelSrc.DefaultScene);

            var cube = new Cube<MaterialBuilder>(MaterialBuilder.CreateDefault(), 1, 0.01f, 1);
            scene.AddMesh(cube.ToMesh(Matrix4x4.Identity), Matrix4x4.Identity);

            var modelBis = scene.ToSchema2();

            // save file

            path = System.IO.Path.GetFileNameWithoutExtension(path);
            modelSrc.AttachToCurrentTest(path +"_src" + ".glb");
            modelBis.AttachToCurrentTest(path +"_bis" + ".glb");

            modelSrc.AttachToCurrentTest(path + "_src" + ".gltf");
            modelBis.AttachToCurrentTest(path + "_bis" + ".gltf");

            modelSrc.AttachToCurrentTest(path + "_src" + ".obj");
            modelBis.AttachToCurrentTest(path + "_bis" + ".obj");

            if (modelSrc.LogicalAnimations.Count > 0)
            {
                modelSrc.AttachToCurrentTest(path + "_src_at01" + ".obj", modelSrc.LogicalAnimations[0], 0.1f);

                if (modelBis.LogicalAnimations.Count > 0)
                    modelBis.AttachToCurrentTest(path + "_bis_at01" + ".obj", modelBis.LogicalAnimations[0], 0.1f);
            }
        }

    }
}
