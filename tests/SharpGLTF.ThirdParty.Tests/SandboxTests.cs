using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

using NUnit.Framework;

using SharpGLTF.Materials;
using SharpGLTF.Schema2;

namespace SharpGLTF.ThirdParty
{
    [ResourcePathFormat("*\\Assets")]
    [AttachmentPathFormat("*\\?")]
    internal class SandboxTests
    {
        [Test]
        public void MaterialCreate()
        {
            Memory.MemoryImage.TryParseMime64("data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAABAAAAAQCAIAAACQkWg2AAAACXBIWXMAAA7DAAAOwwHHb6hkAAAAHXpUWHRUaXRsZQAACJlzSU1LLM0pCUmtKCktSgUAKVIFt/VCuZ8AAAAoelRYdEF1dGhvcgAACJkLy0xOzStJVQhIzUtMSS1WcCzKTc1Lzy8BAG89CQyAoFAQAAAANElEQVQoz2O8cuUKAwxoa2vD2VevXsUqzsRAIqC9Bsb///8TdDey+CD0Awsx7h6NB5prAADPsx0VAB8VRQAAAABJRU5ErkJggg==", out var memoryImage);

            var material = new MaterialBuilder()
                .WithAlpha(Materials.AlphaMode.OPAQUE);            

            material.WithMetallicRoughnessShader()
                .WithMetallicRoughness(0, 1)
                .WithChannelImage(KnownChannel.BaseColor, memoryImage)
                .UseChannel(KnownChannel.BaseColor)
                    .Texture
                    .WithSampler(
                        TextureWrapMode.CLAMP_TO_EDGE,
                        TextureWrapMode.MIRRORED_REPEAT,
                        TextureMipMapFilter.NEAREST_MIPMAP_NEAREST,
                        TextureInterpolationFilter.NEAREST
                    );

            material
                .WithSpecularFactor(memoryImage, 0)
                .WithSpecularColor(memoryImage, Vector3.Zero);

            var model = ModelRoot.CreateModel();
            model.CreateMaterial(material);

            TestContext.WriteLine(model.GetJsonPreview());
        }
    }
}
