using System;
using System.Linq;
using System.Numerics;
using System.Collections.Generic;

using NUnit.Framework;

using SharpGLTF.Schema2;
using SharpGLTF.Geometry;
using SharpGLTF.Geometry.VertexTypes;
using SharpGLTF.Geometry.Parametric;
using SharpGLTF.Materials;

namespace SharpGLTF.Scenes
{
    using VPOSNRM = VertexBuilder<VertexPositionNormal, VertexEmpty, VertexEmpty>;

    using SKINNEDVERTEX = VertexBuilder<VertexPosition, VertexEmpty, VertexJoints4>;


    [Category("Toolkit.Scenes")]
    public class SceneBuilderTests
    {
        [Test(Description ="Creates a simple cube.")]
        public void CreateCubeScene()
        {
            TestContext.CurrentContext.AttachShowDirLink();
            TestContext.CurrentContext.AttachGltfValidatorLinks();

            var material = MaterialBuilder.CreateDefault();

            var mesh = new Cube<MaterialBuilder>(material).ToMesh(Matrix4x4.Identity);

            var scene = new SceneBuilder();

            scene.AddRigidMesh(mesh, Matrix4x4.Identity);

            scene.AttachToCurrentTest("cube.glb");
            scene.AttachToCurrentTest("cube.gltf");
            scene.AttachToCurrentTest("cube.plotly");
        }

        [Test(Description ="Creates a cube attached to an animated node.")]
        public void CreateAnimatedCubeScene()
        {
            TestContext.CurrentContext.AttachShowDirLink();
            TestContext.CurrentContext.AttachGltfValidatorLinks();

            var material = MaterialBuilder.CreateDefault();

            var mesh = new Cube<MaterialBuilder>(material)
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

            scene.AddRigidMesh(mesh, pivot);

            scene.AttachToCurrentTest("animated.glb");
            scene.AttachToCurrentTest("animated.gltf");
        }

        [Test(Description = "Checks that non convex quads are created correctly.")]
        public void CreateNonConvexQuadsScene()
        {
            TestContext.CurrentContext.AttachShowDirLink();
            TestContext.CurrentContext.AttachGltfValidatorLinks();
            
            var mesh = new MeshBuilder<VertexPosition>();
            var prim = mesh.UsePrimitive(MaterialBuilder.CreateDefault());

            var idx = prim.AddQuadrangle(new VertexPosition(0, -1, 0), new VertexPosition(1, 0, 0), new VertexPosition(0, 1, 0), new VertexPosition(-1, 0, 0));
            Assert.AreEqual((0, 1, 2, 3), idx);

            idx = prim.AddQuadrangle(new VertexPosition(0, -1, 1), new VertexPosition(1, 0, 1), new VertexPosition(0, 1, 1), new VertexPosition(0.5f, 0, 1));
            Assert.AreEqual((4, 5, 6, 7), idx);

            idx = prim.AddQuadrangle(new VertexPosition(0, 0.5f, 2), new VertexPosition(1, 0, 2), new VertexPosition(0, 1, 2), new VertexPosition(-1, 0, 2));
            Assert.AreEqual((8,9,10,11), idx);

            idx = prim.AddQuadrangle(new VertexPosition(1, 0, 3), new VertexPosition(0, 1, 3), new VertexPosition(0.5f, 0, 3), new VertexPosition(0, -1, 3));
            Assert.AreEqual((12,13,14,15), idx);

            idx = prim.AddQuadrangle(new VertexPosition(1, 0, 4), new VertexPosition(1, 0, 4), new VertexPosition(0, 1, 4), new VertexPosition(-1, 0, 4));
            Assert.AreEqual((-1, 16, 17, 18), idx);

            idx = prim.AddQuadrangle(new VertexPosition(1, 0, 4), new VertexPosition(1, 0, 4), new VertexPosition(0, 1, 4), new VertexPosition(0, 1, 4));
            Assert.AreEqual((-1, -1, -1, -1), idx);

            idx = prim.AddQuadrangle(new VertexPosition(0, 0, 5), new VertexPosition(10, -1, 5), new VertexPosition(9, 0, 5), new VertexPosition(10, 1, 5));
            Assert.AreEqual((19,20,21,22), idx);

            idx = prim.AddQuadrangle(new VertexPosition(10, -1, 6), new VertexPosition(9, 0, 6), new VertexPosition(10, 1, 6), new VertexPosition(0, 0, 6));
            Assert.AreEqual((23, 24, 25, 26), idx);

            var scene = new SceneBuilder();

            scene.AddRigidMesh(mesh, Matrix4x4.Identity);

            scene.AttachToCurrentTest("NonConvexQuads.glb");
            scene.AttachToCurrentTest("NonConvexQuads.gltf");
        }
        
        [Test(Description = "Creates a scene with multiple cubes and spheres.")]
        public void CreateSceneWithRandomShapes()
        {
            TestContext.CurrentContext.AttachShowDirLink();
            TestContext.CurrentContext.AttachGltfValidatorLinks();

            var rnd = new Random(177);

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

                scene.AddRigidMesh(mesh, xform);                
            }

            // save the model as GLB

            scene.AttachToCurrentTest("shapes.glb");
            scene.AttachToCurrentTest("shapes.plotly");
        }

        [Test]
        public void CreateSceneWithMixedVertexFormats()
        {
            TestContext.CurrentContext.AttachShowDirLink();
            TestContext.CurrentContext.AttachGltfValidatorLinks();

            var scene = new SceneBuilder();

            var mesh1 = new MeshBuilder<VertexPosition, VertexEmpty, VertexEmpty>();
            var mesh2 = new MeshBuilder<VertexPositionNormal, VertexEmpty, VertexEmpty>();

            mesh1.AddCube(MaterialBuilder.CreateDefault(), Matrix4x4.Identity);
            mesh2.AddCube(MaterialBuilder.CreateDefault(), Matrix4x4.Identity);

            scene.AddRigidMesh(mesh1, Matrix4x4.CreateTranslation(-2, 0, 0));
            scene.AddRigidMesh(mesh2, Matrix4x4.CreateTranslation(2, 0, 0));

            scene.AttachToCurrentTest("scene.glb");
        }
        
        [Test]
        public void CreateSkinnedScene()
        {
            TestContext.CurrentContext.AttachShowDirLink();
            TestContext.CurrentContext.AttachGltfValidatorLinks();
            
            // create two materials

            var pink = new MaterialBuilder("material1")
                .WithChannelParam(KnownChannel.BaseColor, new Vector4(1, 0, 1, 1))
                .WithDoubleSide(true);

            var yellow = new MaterialBuilder("material2")
                .WithChannelParam(KnownChannel.BaseColor, new Vector4(1, 1, 0, 1))
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

            mesh.UsePrimitive(pink).AddQuadrangle(v1, v2, v6, v5);
            mesh.UsePrimitive(pink).AddQuadrangle(v2, v3, v7, v6);
            mesh.UsePrimitive(pink).AddQuadrangle(v3, v4, v8, v7);
            mesh.UsePrimitive(pink).AddQuadrangle(v4, v1, v5, v8);

            mesh.UsePrimitive(yellow).AddQuadrangle(v5, v6, v10, v9);
            mesh.UsePrimitive(yellow).AddQuadrangle(v6, v7, v11, v10);
            mesh.UsePrimitive(yellow).AddQuadrangle(v7, v8, v12, v11);
            mesh.UsePrimitive(yellow).AddQuadrangle(v8, v5, v9, v12);

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
                .WithChannelParam(KnownChannel.BaseColor, new Vector4(1, 0, 1, 1))
                .WithDoubleSide(true);

            var yellow = new MaterialBuilder("material2")
                .WithChannelParam(KnownChannel.BaseColor, new Vector4(1, 1, 0, 1))
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

            mesh.UsePrimitive(pink).AddQuadrangle(v1, v2, v6, v5);
            mesh.UsePrimitive(pink).AddQuadrangle(v2, v3, v7, v6);
            mesh.UsePrimitive(pink).AddQuadrangle(v3, v4, v8, v7);
            mesh.UsePrimitive(pink).AddQuadrangle(v4, v1, v5, v8);

            mesh.UsePrimitive(yellow).AddQuadrangle(v5, v6, v10, v9);
            mesh.UsePrimitive(yellow).AddQuadrangle(v6, v7, v11, v10);
            mesh.UsePrimitive(yellow).AddQuadrangle(v7, v8, v12, v11);
            mesh.UsePrimitive(yellow).AddQuadrangle(v8, v5, v9, v12);

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

        [Test]
        public void CreateAllAnimationTypesScene()
        {
            // 3D View 7.1908.9012.0 has an issue displaying off-center meshes with animated morph targets.

            TestContext.CurrentContext.AttachShowDirLink();
            TestContext.CurrentContext.AttachGltfValidatorLinks();

            // create two materials

            var pink = new MaterialBuilder("material1")
                .WithChannelParam(KnownChannel.BaseColor, new Vector4(1, 0, 1, 1));

            var yellow = new MaterialBuilder("material2")
                .WithChannelParam(KnownChannel.BaseColor, new Vector4(1, 1, 0, 1));

            var scene = new SceneBuilder();

            var mesh1 = VPOSNRM.CreateCompatibleMesh("shape1");
            mesh1.AddCube(MaterialBuilder.CreateDefault(), Matrix4x4.Identity);
            var inst1 = scene.AddRigidMesh(mesh1, Matrix4x4.Identity);

            var mesh2 = VPOSNRM.CreateCompatibleMesh("shape2");
            mesh2.AddCube(pink, Matrix4x4.Identity);
            var inst2 = scene.AddRigidMesh(mesh2, Matrix4x4.CreateTranslation(2,0,0));

            scene.AttachToCurrentTest("static.glb");
            scene.AttachToCurrentTest("static.gltf");

            var morphBuilder = mesh2.UseMorphTarget(0);

            morphBuilder.SetVertexDelta(morphBuilder.Positions.ElementAt(0), (Vector3.UnitY, Vector3.Zero));
            morphBuilder.SetVertexDelta(morphBuilder.Positions.ElementAt(1), (Vector3.UnitY, Vector3.Zero));
            morphBuilder.SetVertexDelta(morphBuilder.Positions.ElementAt(2), (Vector3.UnitY, Vector3.Zero));
            morphBuilder.SetVertexDelta(morphBuilder.Positions.ElementAt(3), (Vector3.UnitY, Vector3.Zero));

            inst2.Content.UseMorphing().Value = Transforms.SparseWeight8.Create(1);
            
            var curve = inst2.Content.UseMorphing().UseTrackBuilder("Default");
            curve.SetPoint(0, Transforms.SparseWeight8.Create(0));
            curve.SetPoint(1, Transforms.SparseWeight8.Create(1));
            curve.SetPoint(2, Transforms.SparseWeight8.Create(0));

            var gltf = scene.ToGltf2();

            // Assert.AreEqual(1, gltf.LogicalMeshes[1].MorphWeights[0]);

            scene.AttachToCurrentTest("mopth.glb");
            scene.AttachToCurrentTest("mopth.gltf");
        }

        [Test]
        public void CreateSharedNodeInstanceScene()
        {
            // SceneBuilder API supports reusing a NodeBuilder in multiple instances with different content.
            // but glTF nodes can only hold one mesh per node, so if we find this case we need to internally
            // add an additional child node to give room to the the extra mesh.

            TestContext.CurrentContext.AttachShowDirLink();
            TestContext.CurrentContext.AttachGltfValidatorLinks();

            var m = MaterialBuilder.CreateDefault();

            var cube = new Cube<MaterialBuilder>(m, 1.7f, 1.7f, 1.7f).ToMesh(Matrix4x4.Identity);
            var sphere = new IcoSphere<MaterialBuilder>(m).ToMesh(Matrix4x4.Identity);

            var armature1 = new NodeBuilder("Skeleton1");
            var joint0 = armature1
                .CreateNode("Joint 0")
                .WithLocalTranslation(new Vector3(0, 1, 0));

            var scene = new SceneBuilder();
            scene.AddRigidMesh(cube, joint0);
            scene.AddRigidMesh(sphere, joint0);

            scene.AttachToCurrentTest("instanced.glb");
            scene.AttachToCurrentTest("instanced.gltf");
        }
        

        [TestCase("AnimatedMorphCube.glb")]
        [TestCase("AnimatedMorphSphere.glb")]
        [TestCase("Avocado.glb")]
        [TestCase("BoxAnimated.glb")]
        [TestCase("BrainStem.glb")]
        [TestCase("CesiumMan.glb")]
        [TestCase("GearboxAssy.glb")]        
        [TestCase("OrientationTest.glb")]
        [TestCase("RiggedFigure.glb")]
        [TestCase("RiggedSimple.glb")]
        public void TestRoundTrip(string path)
        {
            TestContext.CurrentContext.AttachShowDirLink();
            TestContext.CurrentContext.AttachGltfValidatorLinks();

            path = TestFiles
                .GetSampleModelsPaths()
                .FirstOrDefault(item => item.Contains(path));

            var srcModel = Schema2.ModelRoot.Load(path, Validation.ValidationMode.TryFix);
            Assert.NotNull(srcModel);            

            // perform roundtrip

            var srcScene = Schema2Toolkit.ToSceneBuilder(srcModel.DefaultScene);            

            var rowModel = srcScene.ToGltf2();

            var settings = SceneBuilderSchema2Settings.Default;
            settings.UseStridedBuffers = false;
            var colModel = srcScene.ToGltf2(settings);

            var rowScene = Schema2Toolkit.ToSceneBuilder(rowModel.DefaultScene);
            var colScene = Schema2Toolkit.ToSceneBuilder(colModel.DefaultScene);

            // compare files

            var srcTris = srcModel.DefaultScene.EvaluateTriangles().ToList();
            var rowTris = rowModel.DefaultScene.EvaluateTriangles().ToList();
            var colTris = colModel.DefaultScene.EvaluateTriangles().ToList();

            Assert.AreEqual(srcTris.Count, rowTris.Count);
            Assert.AreEqual(srcTris.Count, colTris.Count);

            var srcRep = Reporting.ModelReport.CreateReportFrom(srcModel);
            var rowRep = Reporting.ModelReport.CreateReportFrom(rowModel);
            var colRep = Reporting.ModelReport.CreateReportFrom(colModel);

            Assert.AreEqual(srcRep.NumTriangles, rowRep.NumTriangles);
            NumericsAssert.AreEqual(srcRep.Bounds.Min, rowRep.Bounds.Min, 0.0001f);
            NumericsAssert.AreEqual(srcRep.Bounds.Max, rowRep.Bounds.Max, 0.0001f);

            // save file

            path = System.IO.Path.GetFileNameWithoutExtension(path);

            srcModel.AttachToCurrentTest(path + "_src" + ".plotly");

            srcModel.AttachToCurrentTest(path + "_src" + ".glb");
            rowModel.AttachToCurrentTest(path + "_row" + ".glb");
            colModel.AttachToCurrentTest(path + "_col" + ".glb");

            srcModel.AttachToCurrentTest(path + "_src" + ".gltf");
            rowModel.AttachToCurrentTest(path + "_row" + ".gltf");
            colModel.AttachToCurrentTest(path + "_col" + ".gltf");

            srcModel.AttachToCurrentTest(path + "_src" + ".obj");
            rowModel.AttachToCurrentTest(path + "_row" + ".obj");
            colModel.AttachToCurrentTest(path + "_col" + ".obj");

            if (srcModel.LogicalAnimations.Count > 0)
            {
                srcModel.AttachToCurrentTest(path + "_src_at01" + ".obj", srcModel.LogicalAnimations[0], 0.1f);

                if (rowModel.LogicalAnimations.Count > 0)
                    rowModel.AttachToCurrentTest(path + "_row_at01" + ".obj", rowModel.LogicalAnimations[0], 0.1f);
            }
        }

        
        [TestCase("GearboxAssy.glb")]        
        public void ExportMeshes(string path)
        {
            TestContext.CurrentContext.AttachShowDirLink();
            TestContext.CurrentContext.AttachGltfValidatorLinks();

            path = TestFiles
                .GetSampleModelsPaths()
                .FirstOrDefault(item => item.Contains(path));

            // load the glTF model
            var srcModel = ModelRoot.Load(path, Validation.ValidationMode.TryFix);
            Assert.NotNull(srcModel);

            srcModel.AttachToCurrentTest("GearBoxAssy.plotly");

            // convert it to a SceneBuilder so we can manipulate it:
            var srcScene = srcModel.DefaultScene.ToSceneBuilder();

            // export all the individual meshes to OBJ:            
            for(int i=0; i < srcScene.Instances.Count; ++i)
            {
                var inst = srcScene.Instances[i].Content;

                // scan for meshes:
                if (inst.Content is MeshContent mesh)
                {
                    var newScene = new SceneBuilder();

                    newScene.AddRigidMesh(mesh.Mesh, inst.GetPoseWorldMatrix());

                    newScene.AttachToCurrentTest($"result_{i}.obj");
                }                
            }
        }

        [Test]
        public void TestCreateEmptyMesh()
        {
            // create a scenebuilder with an empty mesh
            var sb = new SceneBuilder();

            sb.AddRigidMesh(VPOSNRM.CreateCompatibleMesh("Empty"), Matrix4x4.Identity);

            var schema = sb.ToGltf2();

            Assert.AreEqual(0, schema.LogicalMeshes.Count,"SceneBuilder should detect empty meshes and remove them.");

            schema.CreateMesh("Empty2");

            var fileName = TestContext.CurrentContext.GetAttachmentPath("empty.glb", true);

            Assert.Throws<SharpGLTF.Validation.SchemaException>(() => schema.SaveGLB(fileName));
        }

        [Test]
        public void CreateSceneWithEmptyMeshes()
        {
            // Schema2 does NOT allow meshes to be empty, or meshes with empty MeshPrimitives.
            // but MeshBuilder and SceneBuilder should be able to handle them.

            TestContext.CurrentContext.AttachShowDirLink();
            TestContext.CurrentContext.AttachGltfValidatorLinks();

            var rnd = new Random(177);

            // create materials
            var materials = Enumerable
                .Range(0, 10)
                .Select(idx => new Materials.MaterialBuilder($"material{idx}")
                .WithChannelParam("BaseColor", new Vector4(rnd.NextVector3(), 1)))
                .ToList();

            // create meshes            

            var mesh1 = VPOSNRM.CreateCompatibleMesh("mesh1");
            mesh1.VertexPreprocessor.SetSanitizerPreprocessors();
            mesh1.AddCube(materials[0], Matrix4x4.Identity);
            mesh1.UsePrimitive(materials[1]).AddTriangle(default, default, default); // add degenerated triangle to produce an empty primitive
            mesh1.AddCube(materials[2], Matrix4x4.CreateTranslation(10, 0, 0));

            var mesh2_empty = VPOSNRM.CreateCompatibleMesh("mesh2"); // empty mesh

            var mesh3 = VPOSNRM.CreateCompatibleMesh("mesh3");
            mesh3.VertexPreprocessor.SetSanitizerPreprocessors();
            mesh3.AddCube(materials[3], Matrix4x4.Identity);

            // create scene

            var scene = new SceneBuilder();

            scene.AddRigidMesh(mesh1, Matrix4x4.Identity);
            scene.AddRigidMesh(mesh2_empty, Matrix4x4.Identity);
            scene.AddRigidMesh(mesh3, Matrix4x4.CreateTranslation(0, 10, 0));

            var model = scene.ToGltf2();

            Assert.AreEqual(3, model.LogicalMaterials.Count);
            CollectionAssert.AreEquivalent(new[] { "material0", "material2", "material3" }, model.LogicalMaterials.Select(item => item.Name));

            Assert.AreEqual(2, model.LogicalMeshes.Count);

            Assert.AreEqual("mesh1", model.LogicalMeshes[0].Name);
            Assert.AreEqual(2, model.LogicalMeshes[0].Primitives.Count);

            Assert.AreEqual("mesh3", model.LogicalMeshes[1].Name);
            Assert.AreEqual(1, model.LogicalMeshes[1].Primitives.Count);

            // save the model as GLB

            scene.AttachToCurrentTest("scene.glb");
        }


        [Test(Description ="Regression test for #37")]
        public void CreateNodeBuilderWithWorldMatrix()
        {
            var nbr = new NodeBuilder("Dummy1");
            var nb = nbr.CreateNode("Dummy2");
            nb.WorldMatrix = new Matrix4x4(1, 0, 0, 0, 0, 1, 0, 0, 0, 0, -1, 0, 0, 0, 0, 1);
            nb.UseTranslation().UseTrackBuilder("Default");
            nb.UseRotation().UseTrackBuilder("Default");
            nb.UseScale().UseTrackBuilder("Default");
        }

        [Test(Description = "Creates a new scene by merging multiple scenes")]
        public void CreateSceneComposition()
        {
            // load Polly model
            var polly = SceneBuilder.Load(TestFiles.GetPollyFileModelPath(), Validation.ValidationMode.TryFix);
            
            var xform0 = Matrix4x4.CreateFromYawPitchRoll(1, 0, 0) * Matrix4x4.CreateTranslation(1.5f, 0, 0);
            var xform1 = Matrix4x4.CreateFromYawPitchRoll(0, 1, 0) * Matrix4x4.CreateTranslation(-1.5f, 1, 0);

            var scene = new SceneBuilder();

            scene.AddScene(polly, xform0);
            scene.AddScene(polly, xform1);

            scene.AttachToCurrentTest("construction.glb");
        }

    }
}
