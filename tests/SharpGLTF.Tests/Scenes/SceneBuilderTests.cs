using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.Numerics;

using NUnit.Framework;

using SharpGLTF.Schema2.Authoring;

namespace SharpGLTF.Scenes
{
    using Geometry;
    
    using VPOSNRM = Geometry.VertexBuilder<Geometry.VertexTypes.VertexPositionNormal, Geometry.VertexTypes.VertexEmpty, Geometry.VertexTypes.VertexEmpty>;


    [Category("Toolkit.Scenes")]
    public class SceneBuilderTests
    {
        [Test]
        public void CreateCubeScene()
        {
            TestContext.CurrentContext.AttachShowDirLink();
            TestContext.CurrentContext.AttachGltfValidatorLinks();

            var mesh = new Cube<Materials.MaterialBuilder>(new Materials.MaterialBuilder())
                .ToMesh(Matrix4x4.Identity);

            var scene = new SceneBuilder();

            scene.AddMesh(mesh, Matrix4x4.Identity);

            scene.AttachToCurrentTest("cubes.glb");
        }

        [Test]
        public void CreateAnimatedCubeScene()
        {
            TestContext.CurrentContext.AttachShowDirLink();
            TestContext.CurrentContext.AttachGltfValidatorLinks();

            var mesh = new Cube<Materials.MaterialBuilder>(new Materials.MaterialBuilder())
                .ToMesh(Matrix4x4.Identity);

            var pivot = new NodeBuilder();

            var tcurve = pivot.UseTranslation().UseSplineCurve("default");
            tcurve.SetControlPoint(0, Vector3.Zero);

            tcurve.SetControlPoint(1, new Vector3(10, 0, 0));
            tcurve.SetCardinalPointOut(1, new Vector3(10, 10, 0));
            
            tcurve.SetControlPoint(2, new Vector3(10, 0, 0));
            tcurve.SetCardinalPointIn(2, new Vector3(20, 10, 0));

            tcurve.SetControlPoint(3, new Vector3(20, 0, 0));

            tcurve.SetControlPoint(4, new Vector3(10, -10, 0));

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
                var m = materials[rnd.Next(0, 10)];
                var s = VPOSNRM.CreateCompatibleMesh("shape");
                s.VertexPreprocessor.SetDebugPreprocessors();

                if ((i & 1) == 0) s.AddCube(m, Matrix4x4.Identity);
                else s.AddSphere(m, 0.5f, Matrix4x4.Identity);

                s.Validate();

                // create random transform
                var r = rnd.NextVector3() * 5;
                var xform = Matrix4x4.CreateFromYawPitchRoll(r.X, r.Y, r.Z) * Matrix4x4.CreateTranslation(rnd.NextVector3() * 25);

                scene.AddMesh(s, xform);                
            }

            // save the model as GLB

            scene.AttachToCurrentTest("shapes.glb");
        }
    }
}
