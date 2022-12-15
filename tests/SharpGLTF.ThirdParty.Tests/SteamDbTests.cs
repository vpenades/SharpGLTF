using System.Collections.Generic;
using System.IO;
using System.Linq;

using NUnit.Framework;

using SharpGLTF.Materials;
using SharpGLTF.Memory;
using SharpGLTF.Scenes;

using SharpGLTF.Geometry.Parametric;
using SharpGLTF.Geometry;

namespace SharpGLTF.ThirdParty
{
    [ResourcePathFormat("*\\Assets")]
    [AttachmentPathFormat("*\\?")]
    internal class SteamDbTests
    {
        [Test]
        public void TestWriteAlternateTexturePath()
        {
            // create material
            var material = new MaterialBuilder()
                .WithDoubleSide(true)
                .WithMetallicRoughnessShader();

            var imgBuilder = ImageBuilder.From(new MemoryImage(ResourceInfo.From("shannon.png")));
            imgBuilder.AlternateWriteFileName = "alternateTextureName.*";

            material.WithBaseColor(imgBuilder);            

            var meshBuilder = new MeshBuilder<MaterialBuilder, Geometry.VertexTypes.VertexPosition, Geometry.VertexTypes.VertexEmpty, Geometry.VertexTypes.VertexEmpty>();
            meshBuilder.AddCube(material, System.Numerics.Matrix4x4.Identity);

            var sceneBuilder = new SceneBuilder();
            sceneBuilder.AddRigidMesh(meshBuilder, System.Numerics.Matrix4x4.Identity);

            var model = sceneBuilder.ToGltf2();

            Assert.AreEqual("alternateTextureName.*", model.LogicalImages[0].AlternateWriteFileName);

            model = model.DeepClone();

            Assert.AreEqual("alternateTextureName.*", model.LogicalImages[0].AlternateWriteFileName);

            var dstPath = AttachmentInfo
                .From("model.gltf")
                .WriteObject(f => model.Save(f));

            var altPath = System.IO.Path.Combine(dstPath.Directory.FullName, "alternateTextureName.png");

            Assert.IsTrue(System.IO.File.Exists(altPath));
        }
    }
}
