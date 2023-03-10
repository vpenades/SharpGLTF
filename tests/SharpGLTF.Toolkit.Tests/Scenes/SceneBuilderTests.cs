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
using System.Diagnostics;
using System.Collections.ObjectModel;
using SharpGLTF.Validation;

namespace SharpGLTF.Scenes
{
    using VPOSNRM = VertexBuilder<VertexPositionNormal, VertexEmpty, VertexEmpty>;

    using SKINNEDVERTEX4 = VertexBuilder<VertexPosition, VertexEmpty, VertexJoints4>;
    using SKINNEDVERTEX8 = VertexBuilder<VertexPosition, VertexEmpty, VertexJoints8>;


    [Category("Toolkit.Scenes")]
    public partial class SceneBuilderTests
    {
        [Test(Description = "Creates a simple triangle with Cesium outlining")]
        public void CreateCesiumOutlineTriangleScene()
        {
            TestContext.CurrentContext.AttachGltfValidatorLinks();

            var material = MaterialBuilder.CreateDefault();

            var mesh = new MeshBuilder<VertexPosition>("mesh");

            var prim = mesh.UsePrimitive(material);
            prim.AddTriangle(new VertexPosition(-10, 0, 0), new VertexPosition(10, 0, 0), new VertexPosition(0, 10, 0));

            var scene = new SceneBuilder();

            scene.AddRigidMesh(mesh, Matrix4x4.Identity);

            var model = scene.ToGltf2();

            var outlines = new uint[] { 0, 1, 1, 2, 2, 0};            
            model.LogicalMeshes[0].Primitives[0].SetCesiumOutline(outlines);

            var cesiumOutlineExtension = (CesiumPrimitiveOutline)model.LogicalMeshes[0].Primitives[0].Extensions.FirstOrDefault();
            Assert.NotNull(cesiumOutlineExtension.Indices);
            CollectionAssert.AreEqual(outlines, cesiumOutlineExtension.Indices.AsIndicesArray());

            var ctx = new ValidationResult(model, ValidationMode.Strict, true);
            model.ValidateContent(ctx.GetContext());

            scene.AttachToCurrentTest("cesium_outline_triangle.glb");
            scene.AttachToCurrentTest("cesium_outline_triangle.gltf");
            scene.AttachToCurrentTest("cesium_outline_triangle.plotly");
        }

        [Test(Description ="Creates a simple cube.")]
        public void CreateCubeScene()
        {            
            TestContext.CurrentContext.AttachGltfValidatorLinks();

            var material = MaterialBuilder.CreateDefault();            

            var mesh = new Cube<MaterialBuilder>(material).ToMesh(Matrix4x4.Identity);            

            var scene = new SceneBuilder();

            scene.AddRigidMesh(mesh, Matrix4x4.Identity);                       

            scene.AttachToCurrentTest("cube.glb");
            scene.AttachToCurrentTest("cube.gltf");
            scene.AttachToCurrentTest("cube.plotly");
        }

        [Test(Description = "Creates a simple cube with a light.")]
        public void CreateCubeWithLightScene()
        {
            TestContext.CurrentContext.AttachGltfValidatorLinks();

            var material = MaterialBuilder.CreateDefault();

            var mesh = new Cube<MaterialBuilder>(material).ToMesh(Matrix4x4.Identity);

            var scene = new SceneBuilder();

            scene.AddRigidMesh(mesh, Matrix4x4.Identity);

            var light = new LightBuilder.Point
            {
                Color = new Vector3(1, 0, 0),
                Intensity = 3,
                Range = 10,
            };
            
            scene.AddLight(light, new NodeBuilder("light").WithLocalTranslation(new Vector3(0, 100, 0)) );
            scene.AddLight(light, Matrix4x4.CreateTranslation(0, -100, 0));

            var lightInstances = scene.Instances
                .Select(item => item.Content.Content)
                .OfType<LightContent>()
                .ToList();

            Assert.AreEqual(2, lightInstances.Count);

            var gltf = scene.ToGltf2();

            Assert.AreEqual(2, gltf.LogicalPunctualLights.Count);

            gltf.AttachToCurrentTest("cube.glb");
            gltf.AttachToCurrentTest("cube.gltf");
            gltf.AttachToCurrentTest("cube.plotly");
        }

        [Test(Description = "Creates a simple cube.")]
        public void CreateCubeSceneWithExtras()
        {            
            TestContext.CurrentContext.AttachGltfValidatorLinks();

            var material = MaterialBuilder.CreateDefault();
            material.Name = "hello name";
            material.Extras = IO.JsonContent.Serialize(new KeyValuePair<string, int>("hello", 16));

            var mesh = new Cube<MaterialBuilder>(material).ToMesh(Matrix4x4.Identity);
            mesh.Name = "world name";
            mesh.Extras = "world extras";

            var scene = new SceneBuilder();

            scene.AddRigidMesh(mesh, Matrix4x4.Identity)
                .WithName("Cube")
                .WithExtras(17);

            scene.AttachToCurrentTest("cube.glb");
            scene.AttachToCurrentTest("cube.gltf");
            scene.AttachToCurrentTest("cube.plotly");
        }

        [Test(Description ="Creates a cube attached to an animated node.")]
        public void CreateAnimatedCubeScene()
        {            
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
        
        [TestCase(false)]
        [TestCase(true)]
        public void CreateSceneWithRandomShapes(bool useGpuInstancing)
        {            
            TestContext.CurrentContext.AttachGltfValidatorLinks();

            var rnd = new Random(177);

            // create materials

            var materials = Enumerable
                .Range(0, 10)
                .Select(idx => new MaterialBuilder()
                .WithChannelParam(KnownChannel.BaseColor, KnownProperty.RGBA, new Vector4(rnd.NextVector3(), 1)))
                .ToList();

            // create meshes

            var sphereMeshes = Enumerable
                .Range(0, 10)
                .Select(idx => materials[idx])
                .Select(mat =>
                {
                    var mesh = VPOSNRM.CreateCompatibleMesh("shape");
                    #if DEBUG
                    mesh.VertexPreprocessor.SetValidationPreprocessors();
                    #else
                    mesh.VertexPreprocessor.SetSanitizerPreprocessors();
                    #endif
                    mesh.AddSphere(mat, 0.5f, Matrix4x4.Identity);
                    mesh.Validate();
                    return mesh;
                });

            var cubeMeshes = Enumerable
                .Range(0, 10)
                .Select(idx => materials[idx])
                .Select(mat =>
                {
                    var mesh = VPOSNRM.CreateCompatibleMesh("shape");
                    #if DEBUG
                    mesh.VertexPreprocessor.SetValidationPreprocessors();
                    #else
                    mesh.VertexPreprocessor.SetSanitizerPreprocessors();
                    #endif
                    mesh.AddCube(mat, Matrix4x4.Identity);
                    mesh.Validate();
                    return mesh;
                });

            var meshes = sphereMeshes.Concat(cubeMeshes).ToArray();

            // create scene            

            var scene = new SceneBuilder();

            for (int i = 0; i < 100; ++i)
            {                
                var mesh = meshes[rnd.Next(0, 20)];

                // create random transform
                var r = rnd.NextQuaternion();                
                var t = rnd.NextVector3() * 25;

                scene.AddRigidMesh(mesh, (r, t));
            }

            // collapse to glTF

            
            var gltf = scene.ToGltf2(useGpuInstancing ? SceneBuilderSchema2Settings.WithGpuInstancing : SceneBuilderSchema2Settings.Default);

            var bounds = Runtime.MeshDecoder.EvaluateBoundingBox(gltf.DefaultScene);

            // Assert.AreEqual(defaultBounds,instancedBounds);

            // save the model as GLB

            gltf.AttachToCurrentTest("shapes.glb");
            scene.AttachToCurrentTest("shapes.plotly");
        }

        [Test]
        public void CreateSceneWithMixedVertexFormats()
        {            
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
            TestContext.CurrentContext.AttachGltfValidatorLinks();
            
            // create two materials

            var pink = new MaterialBuilder("material1")
                .WithChannelParam(KnownChannel.BaseColor,KnownProperty.RGBA, new Vector4(1, 0, 1, 1))
                .WithDoubleSide(true);

            var yellow = new MaterialBuilder("material2")
                .WithChannelParam(KnownChannel.BaseColor, KnownProperty.RGBA, new Vector4(1, 1, 0, 1))
                .WithDoubleSide(true);

            // create the mesh            

            const int jointIdx0 = 0; // index of joint node 0
            const int jointIdx1 = 1; // index of joint node 1
            const int jointIdx2 = 2; // index of joint node 2

            var v1 = new SKINNEDVERTEX4(new Vector3(-10, 0, +10), (jointIdx0, 1));
            var v2 = new SKINNEDVERTEX4(new Vector3(+10, 0, +10), (jointIdx0, 1));
            var v3 = new SKINNEDVERTEX4(new Vector3(+10, 0, -10), (jointIdx0, 1));
            var v4 = new SKINNEDVERTEX4(new Vector3(-10, 0, -10), (jointIdx0, 1));

            var v5 = new SKINNEDVERTEX4(new Vector3(-10, 40, +10), (jointIdx0, 0.5f), (jointIdx1, 0.5f));
            var v6 = new SKINNEDVERTEX4(new Vector3(+10, 40, +10), (jointIdx0, 0.5f), (jointIdx1, 0.5f));
            var v7 = new SKINNEDVERTEX4(new Vector3(+10, 40, -10), (jointIdx0, 0.5f), (jointIdx1, 0.5f));
            var v8 = new SKINNEDVERTEX4(new Vector3(-10, 40, -10), (jointIdx0, 0.5f), (jointIdx1, 0.5f));

            var v9  = new SKINNEDVERTEX4(new Vector3(-5, 80, +5), (jointIdx2, 1));
            var v10 = new SKINNEDVERTEX4(new Vector3(+5, 80, +5), (jointIdx2, 1));
            var v11 = new SKINNEDVERTEX4(new Vector3(+5, 80, -5), (jointIdx2, 1));
            var v12 = new SKINNEDVERTEX4(new Vector3(-5, 80, -5), (jointIdx2, 1));

            var mesh = SKINNEDVERTEX4.CreateCompatibleMesh("mesh1");

            #if DEBUG
            mesh.VertexPreprocessor.SetValidationPreprocessors();
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
            TestContext.CurrentContext.AttachGltfValidatorLinks();

            // create two materials

            var pink = new MaterialBuilder("material1")
                .WithChannelParam(KnownChannel.BaseColor, KnownProperty.RGBA, new Vector4(1, 0, 1, 1))
                .WithDoubleSide(true);

            var yellow = new MaterialBuilder("material2")
                .WithChannelParam(KnownChannel.BaseColor, KnownProperty.RGBA, new Vector4(1, 1, 0, 1))
                .WithDoubleSide(true);

            // create the mesh            

            const int jointIdx0 = 0; // index of joint node 0
            const int jointIdx1 = 1; // index of joint node 1
            const int jointIdx2 = 2; // index of joint node 2

            var v1 = new SKINNEDVERTEX4(new Vector3(-10, 0, +10), (jointIdx0, 1));
            var v2 = new SKINNEDVERTEX4(new Vector3(+10, 0, +10), (jointIdx0, 1));
            var v3 = new SKINNEDVERTEX4(new Vector3(+10, 0, -10), (jointIdx0, 1));
            var v4 = new SKINNEDVERTEX4(new Vector3(-10, 0, -10), (jointIdx0, 1));

            var v5 = new SKINNEDVERTEX4(new Vector3(-10, 40, +10), (jointIdx0, 0.5f), (jointIdx1, 0.5f));
            var v6 = new SKINNEDVERTEX4(new Vector3(+10, 40, +10), (jointIdx0, 0.5f), (jointIdx1, 0.5f));
            var v7 = new SKINNEDVERTEX4(new Vector3(+10, 40, -10), (jointIdx0, 0.5f), (jointIdx1, 0.5f));
            var v8 = new SKINNEDVERTEX4(new Vector3(-10, 40, -10), (jointIdx0, 0.5f), (jointIdx1, 0.5f));

            var v9 = new SKINNEDVERTEX4(new Vector3(-5, 80, +5), (jointIdx2, 1));
            var v10 = new SKINNEDVERTEX4(new Vector3(+5, 80, +5), (jointIdx2, 1));
            var v11 = new SKINNEDVERTEX4(new Vector3(+5, 80, -5), (jointIdx2, 1));
            var v12 = new SKINNEDVERTEX4(new Vector3(-5, 80, -5), (jointIdx2, 1));

            var mesh = SKINNEDVERTEX4.CreateCompatibleMesh("mesh1");

            #if DEBUG
            mesh.VertexPreprocessor.SetValidationPreprocessors();
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

        [Test(Description = "Creates a morph animated cube")]
        public void CreateAllAnimationTypesScene()
        {
            // 3D View 7.1908.9012.0 has an issue displaying off-center meshes with animated morph targets.
                        
            TestContext.CurrentContext.AttachGltfValidatorLinks();

            // create two materials

            var pink = new MaterialBuilder("material1")
                .WithChannelParam(KnownChannel.BaseColor, KnownProperty.RGBA, new Vector4(1, 0, 1, 1));

            var yellow = new MaterialBuilder("material2")
                .WithChannelParam(KnownChannel.BaseColor, KnownProperty.RGBA, new Vector4(1, 1, 0, 1));            

            var mesh1 = VPOSNRM.CreateCompatibleMesh("shape1");
            mesh1.AddCube(pink, Matrix4x4.Identity);            

            var mesh2 = VPOSNRM.CreateCompatibleMesh("shape2");
            mesh2.AddCube(yellow, Matrix4x4.Identity);            

            var scene = new SceneBuilder();            

            var inst1 = scene.AddRigidMesh(mesh1, Matrix4x4.Identity);

            // meshes intended to support animation must be created using an armature
            var armature = new NodeBuilder();
            armature.LocalTransform = Matrix4x4.CreateTranslation(2, 0, 0);
            var inst2 = scene.AddRigidMesh(mesh2, armature); 

            scene.AttachToCurrentTest("static.glb");
            scene.AttachToCurrentTest("static.gltf");

            // up to this point, the scene has two plain unanimated cubes.

            var morphBuilder = mesh2.UseMorphTarget(0);
            morphBuilder.SetVertexDelta(morphBuilder.Positions.ElementAt(0), (Vector3.UnitY, Vector3.Zero));
            morphBuilder.SetVertexDelta(morphBuilder.Positions.ElementAt(1), (Vector3.UnitY, Vector3.Zero));
            morphBuilder.SetVertexDelta(morphBuilder.Positions.ElementAt(2), (Vector3.UnitY, Vector3.Zero));
            morphBuilder.SetVertexDelta(morphBuilder.Positions.ElementAt(3), (Vector3.UnitY, Vector3.Zero));

            // set default value.
            inst2.Content.UseMorphing().SetValue(1);
            
            // ser animation curve.
            var curve = inst2.Content.UseMorphing().UseTrackBuilder("Default");
            curve.SetPoint(0, true, 0);
            curve.SetPoint(1, true, 1);
            curve.SetPoint(2, true, 0);

            var gltf = scene.ToGltf2();

            TestContext.WriteLine(gltf.GetJsonPreview());

            var meshIdx = 1;

            Assert.AreEqual(1, gltf.LogicalMeshes[meshIdx].Primitives[0].MorphTargetsCount);
            Assert.AreEqual(1, gltf.LogicalMeshes[meshIdx].MorphWeights[0]);
            Assert.AreEqual(1, gltf.LogicalAnimations.Count);

            scene.AttachToCurrentTest("mopth.glb");
            scene.AttachToCurrentTest("mopth.gltf");
        }

        [Test]
        public void CreateSharedNodeInstanceScene()
        {
            // SceneBuilder API supports reusing a NodeBuilder in multiple instances with different content.
            // but glTF nodes can only hold one mesh per node, so if we find this case we need to internally
            // add an additional child node to give room to the the extra mesh.
                        
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
            TestContext.CurrentContext.AttachGltfValidatorLinks();

            path = TestFiles
                .GetSampleModelsPaths()
                .FirstOrDefault(item => item.Contains(path));

            var srcModel = Schema2.ModelRoot.Load(path, Validation.ValidationMode.TryFix);
            Assert.NotNull(srcModel);            

            // perform roundtrip

            var srcScene = Toolkit.ToSceneBuilder(srcModel.DefaultScene);            

            var rowModel = srcScene.ToGltf2();

            var settings = SceneBuilderSchema2Settings.Default;
            settings.UseStridedBuffers = false;
            var colModel = srcScene.ToGltf2(settings);

            var rowScene = Toolkit.ToSceneBuilder(rowModel.DefaultScene);
            var colScene = Toolkit.ToSceneBuilder(colModel.DefaultScene);

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

            var fileName = AttachmentInfo.From("empty.glb").File.FullName;

            Assert.Throws<SharpGLTF.Validation.SchemaException>(() => schema.SaveGLB(fileName));
        }

        [Test]
        public void TestEmptyNodeRoundtrip()
        {
            // create a scenebuilder with an empty node
            var sb = new SceneBuilder();

            sb.AddNode(new NodeBuilder()); // unnamed nodes will be optimized out
            sb.AddNode(new NodeBuilder("Named"));            

            var gltf = sb.ToGltf2();

            Assert.AreEqual(2, gltf.LogicalNodes.Count);

            // roundtrip
            sb = SceneBuilder.CreateFrom(gltf.DefaultScene);

            var instance = sb.Instances.FirstOrDefault(item => item.Name == "Named");

            Assert.NotNull(instance);            
        }

        [Test]
        public void CreateSceneWithEmptyMeshes()
        {
            // Schema2 does NOT allow meshes to be empty, or meshes with empty MeshPrimitives.
            // but MeshBuilder and SceneBuilder should be able to handle them.
                        
            TestContext.CurrentContext.AttachGltfValidatorLinks();

            var rnd = new Random(177);

            // create materials
            var materials = Enumerable
                .Range(0, 10)
                .Select(idx => new MaterialBuilder($"material{idx}")
                .WithChannelParam(KnownChannel.BaseColor, KnownProperty.RGBA, new Vector4(rnd.NextVector3(), 1)))
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
            var polly = SceneBuilder.LoadDefaultScene(TestFiles.GetPollyFileModelPath(), Validation.ValidationMode.TryFix);
            
            var xform0 = Matrix4x4.CreateFromYawPitchRoll(1, 0, 0) * Matrix4x4.CreateTranslation(1.5f, 0, 0);
            var xform1 = Matrix4x4.CreateFromYawPitchRoll(0, 1, 0) * Matrix4x4.CreateTranslation(-1.5f, 1, 0);

            var scene = new SceneBuilder();

            scene.AddScene(polly, xform0);
            scene.AddScene(polly, xform1);

            scene.AttachToCurrentTest("construction.glb");
        }

        [Test(Description = "Creates a scene using 8 weight skins")]
        public void CreateSkinnedSceneWith8Weights()
        {
            // create plane mesh

            (int, float)[] binds = new[]
            {
                (0, 0.125f),
                (1, 0.125f),
                (2, 0.125f),
                (3, 0.125f),
                (4, 0.125f),
                (5, 0.125f),
                (6, 0.125f),
                (7, 0.125f),
            };

            var v0 = new SKINNEDVERTEX8(new Vector3(1, 0, 1), binds);
            var v1 = new SKINNEDVERTEX8(new Vector3(-1, 0, 1), binds);
            var v2 = new SKINNEDVERTEX8(new Vector3(-1, 0, -1), binds);
            var v3 = new SKINNEDVERTEX8(new Vector3(1, 0, -1), binds);

            var mesh = SKINNEDVERTEX8.CreateCompatibleMesh();
            var prim = mesh.UsePrimitive(MaterialBuilder.CreateDefault());

            prim.AddTriangle(v0, v1, v2);
            prim.AddTriangle(v0, v2, v3);

            // create armature

            var root = new NodeBuilder();
            var n0 = root.CreateNode();
            var n1 = n0.CreateNode();
            var n2 = n1.CreateNode();
            var n3 = n2.CreateNode();
            var n4 = n3.CreateNode();
            var n5 = n4.CreateNode();
            var n6 = n5.CreateNode();
            var n7 = n6.CreateNode();

            // create scene

            var scene = new SceneBuilder();

            scene.AddSkinnedMesh(mesh, Matrix4x4.Identity, n0, n1, n2, n3, n4, n5, n6, n7);

            scene.AttachToCurrentTest("output.gltf");
        }



        [Test]
        public void CreateMorphScene()
        {
            // 3D View 7.1908.9012.0 has an issue displaying off-center meshes with animated morph targets.
                        
            TestContext.CurrentContext.AttachGltfValidatorLinks();

            var meshMorphs = CreateMeshWith16MorphTargets();

            var scene = new SceneBuilder();
            var node = new NodeBuilder();

            // var inst = scene.AddRigidMesh(mesh1, Matrix4x4.Identity);
            // inst.Content.UseMorphing().SetValue(1);

            var inst = scene.AddRigidMesh(meshMorphs, node);
            inst.Content.UseMorphing().SetValue(1.5f, 1.25f, 1, 1, 1, 1, 0.5f, 0.25f, 0.5f, 1, 1, 0, 0.25f, 0.5f, 0.75f, 1.5f);

            scene.AttachToCurrentTest("morph.glb");
            scene.AttachToCurrentTest("morph.gltf");
            scene.ToGltf2().DefaultScene.ToSceneBuilder().AttachToCurrentTest("morph-roundtrip.glb");

            var morphAnim = inst.Content.UseMorphing("Default");

            var wwww = new float[16];

            for(int i=0; i < 16; ++i)
            {
                Array.Clear(wwww, 0, wwww.Length);
                wwww[i] = 1;
                morphAnim.SetPoint(i, true, wwww);
            }

            var rnd = new Random(154);

            for (int i = 16; i < 24; ++i)
            {
                Array.Clear(wwww, 0, wwww.Length);
                for(int j=0; j < wwww.Length; ++j) wwww[j] = (float)rnd.NextDouble();
                morphAnim.SetPoint(i, true, wwww);
            }

            scene.AttachToCurrentTest("morph-anim.glb");
            scene.AttachToCurrentTest("morph-anim.gltf");
            scene.ToGltf2().DefaultScene.ToSceneBuilder().AttachToCurrentTest("morph-anim-roundtrip.glb");
        }        

        static MeshBuilder<VertexPositionNormal, VertexEmpty, VertexEmpty> CreateMeshWith16MorphTargets()
        {
            // create two materials

            var pink = new MaterialBuilder("material1")
                .WithChannelParam(KnownChannel.BaseColor, KnownProperty.RGBA, new Vector4(1, 0, 1, 1));

            var blue = new MaterialBuilder("material2")
                .WithChannelParam(KnownChannel.BaseColor, KnownProperty.RGBA, new Vector4(0, 0, 1, 1));

            var mesh1 = VPOSNRM.CreateCompatibleMesh("shape1");
            var prim1 = mesh1.UsePrimitive(pink);
            var prim2 = mesh1.UsePrimitive(blue);

            // create a mesh made of a strip of triangle pairs (quads), with 256 segments

            for (int i = 0; i < 256; ++i)
            {
                var a = new VertexPositionNormal(i + 0, 0, +10, 0, 1, 0);
                var b = new VertexPositionNormal(i + 1, 0, +10, 0, 1, 0);
                var c = new VertexPositionNormal(i + 1, 0, -10, 0, 1, 0);
                var d = new VertexPositionNormal(i + 0, 0, -10, 0, 1, 0);

                prim1.AddQuadrangle(a, b, c, d);
                prim2.AddQuadrangle(d, c, b, a);
            }

            // create a 16 morph targets

            for (int i = 0; i < 16; ++i)
            {
                var morphTarget = mesh1.UseMorphTarget(i);

                var idx = i * 16 + 8;

                const float waveWidth = 5;

                foreach (var baseVertex in morphTarget.Vertices)
                {
                    var morphedVertex = baseVertex;

                    var distance = Math.Abs(baseVertex.Position.X - idx);
                    if (distance > waveWidth) continue;

                    distance *= (float)Math.PI / (waveWidth * 2);
                    distance = 30 * (float)Math.Cos(distance);

                    morphedVertex.Position += new Vector3(0, distance, 0);

                    // this method sets a new modified vertex associated to the base vertex.
                    // notice that this method works with absolute values, deltas are calculated internally.
                    // alternatively, you can also set deltas with SetVertexDelta method.
                    morphTarget.SetVertex(baseVertex, morphedVertex);
                }                
            }

            return mesh1;
        }


        [Test]
        public void TestWholeModelConversionRoundtrip()
        {
            // create a cube mesh. This mesh will be shared along the way:

            var cube = new MeshBuilder<VertexPosition,VertexEmpty,VertexEmpty>("Cube");
            cube.AddCube(MaterialBuilder.CreateDefault(), Matrix4x4.Identity);

            // create a gltf with 2 scenes:

            var model1 = ModelRoot.CreateModel();
            var m = model1.CreateMesh(cube);
            model1.UseScene("Scene1").CreateNode("Node1").Mesh = m;
            model1.UseScene("Scene2").CreateNode("Node2").Mesh = m;

            // convert to SceneBuilder:

            var scenes = SceneBuilder.CreateFrom(model1).ToArray();
            Assert.AreEqual(2, scenes.Length);

            var mesh1 = scenes[0].Instances[0].Content.GetGeometryAsset();
            var mesh2 = scenes[1].Instances[0].Content.GetGeometryAsset();

            Assert.AreSame(mesh1, mesh2, "both scenes must share the same MeshBuilder");

            // convert back to gltf:

            var model2 = SceneBuilder.ToGltf2(scenes, SceneBuilderSchema2Settings.Default);

            // verify the mesh is still shared.

            Assert.AreEqual(2, model2.LogicalScenes.Count);
            Assert.AreEqual(2, model2.LogicalNodes.Count);
            Assert.AreEqual(1, model2.LogicalMeshes.Count); // check the mesh is shared between the 2 scenes
        }

        [Test]
        public void TestSceneAddition()
        {
            // create a cube mesh. This mesh will be shared along the way:

            var cube = new MeshBuilder<VertexPosition, VertexEmpty, VertexEmpty>("Cube");
            cube.AddCube(MaterialBuilder.CreateDefault(), Matrix4x4.Identity);

            // create a scene

            var scene1 = new SceneBuilder();
            scene1.AddRigidMesh(cube, Matrix4x4.Identity);

            // create another scene

            var scene2 = new SceneBuilder();
            scene2.AddScene(scene1, Matrix4x4.CreateTranslation(4, 0, 0));

            Assert.AreEqual(new Vector3(4, 0, 0), scene2.Instances.First().Content.GetPoseWorldMatrix().Translation);


            scene2.AddScene(scene1, Matrix4x4.CreateTranslation(2, 0, 0));
            scene2.AddScene(scene1, Matrix4x4.CreateTranslation(0, 0, 0));

            // convert to gltf

            var gltf = scene2.ToGltf2();

            Assert.AreEqual(1, gltf.LogicalMeshes.Count);
            Assert.AreEqual(3, gltf.LogicalNodes.Count);

            gltf.AttachToCurrentTest("Three cubes.glb");
        }

    }
}