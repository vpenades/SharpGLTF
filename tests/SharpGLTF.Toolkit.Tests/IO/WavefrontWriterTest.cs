using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using NUnit.Framework;

using SharpGLTF.Scenes;
using SharpGLTF.Geometry;
using SharpGLTF.Geometry.Parametric;


namespace SharpGLTF.IO
{
    internal class WavefrontWriterTest
    {
        [Test]
        public void WriteWavefrontFileTest()
        {
            var material = Materials.MaterialBuilder.CreateDefault();
            material.WithBaseColor(Memory.MemoryImage.DefaultPngImage);

            var mesh = new MeshBuilder<Geometry.VertexTypes.VertexPositionNormal, Geometry.VertexTypes.VertexEmpty, Geometry.VertexTypes.VertexEmpty>("SphereMesh");
            mesh.AddSphere(material, 50, System.Numerics.Matrix4x4.Identity);

            var outPath = mesh.AttachToCurrentTest("result.obj");

            var pngPath = System.IO.Path.ChangeExtension(outPath, ".png");

            Assert.IsTrue(System.IO.File.Exists(pngPath));
        }
    }
}
