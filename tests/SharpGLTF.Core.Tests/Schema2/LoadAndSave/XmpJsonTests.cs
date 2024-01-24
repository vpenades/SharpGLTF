using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;

using NUnit.Framework;

namespace SharpGLTF.Schema2.LoadAndSave
{
    /// <summary>
    /// Test cases for models found in <see href="https://github.com/KhronosGroup/glTF-Blender-Exporter"/>
    /// </summary>
    [TestFixture]
    [AttachmentPathFormat("*/TestResults/LoadAndSave/?", true)]
    [Category("XMP Json tests")]
    public class XmpJsonTests
    {
        [Test]
        public void LoadXmpModel()
        {
            var model = ModelRoot.Load(ResourceInfo.From("XmpJsonLd.gltf"));
            Assert.That(model, Is.Not.Null);

            var packets = model.GetExtension<XmpPackets>();
            Assert.That(packets, Is.Not.Null);
            Assert.That(packets.JsonPackets.Count, Is.EqualTo(1));

            var packet = packets.JsonPackets[0];

            model.AttachToCurrentTest("result.gltf");

            var model2 = ModelRoot.CreateModel();
            model2.UseExtension<XmpPackets>().AddPacket(packet);
            model2.Asset.UseExtension<XmpPacketReference>().SetPacket(0);
            model2.AttachToCurrentTest("result2.gltf");

        }
    }
}
