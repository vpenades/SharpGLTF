using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

using NUnit.Framework;

using SharpGLTF.Geometry;
using SharpGLTF.Materials;

using VERTEX = SharpGLTF.Geometry.VertexTypes.VertexPositionNormal;

namespace SharpGLTF.ThirdParty
{
    public class EdMackeyTests
    {
        [Test]
        public void TestVertexEquality()
        {
            var material1 = new MaterialBuilder()
                .WithDoubleSide(true)
                .WithMetallicRoughnessShader()
                .WithChannelParam(KnownChannel.BaseColor, KnownProperty.RGBA, new Vector4(1, 0, 0, 1));

            var mesh = new MeshBuilder<VERTEX>("mesh");

            var v0 = new VERTEX(15, 5, 0, 1, 0, 0);
            var v1 = new VERTEX(15, 5, 10, 1, 0, 0);
            var v2 = new VERTEX(15, 0, 10, 1, 0, 0);
            var v3 = new VERTEX(15, 0, 0, 1, 0, 0);

            var prim = mesh.UsePrimitive(material1);
            prim.AddTriangle(v0, v1, v3);
            prim.AddTriangle(v3, v1, v2);

            v0 = new VERTEX(15, 5, 10, 0, 0, 1);
            v1 = new VERTEX(0, 5, 10, 0, 0, 1);
            v2 = new VERTEX(0, 0, 10, 0, 0, 1);
            v3 = new VERTEX(15, 0, 10, 0, 0, 1);

            prim.AddTriangle(v0, v1, v3);
            prim.AddTriangle(v3, v1, v2);

            Assert.AreEqual(8, prim.Vertices.Count);

            // create a scene

            var scene = new Scenes.SceneBuilder();

            scene.AddRigidMesh(mesh, Matrix4x4.Identity);

            // save the model in different formats

            scene.AttachToCurrentTest("mesh.glb");
            scene.AttachToCurrentTest("mesh.gltf");
        }

    }
}
