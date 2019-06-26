using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

using NUnit.Framework;

namespace SharpGLTF
{
    [TestFixture]
    [Category("Core")]
    public class AnimationSamplingTests
    {
        private static (float, (Vector3, Vector3, Vector3))[] _TransAnim = new []
        {
            (0.0f, (new Vector3(0, 0, 0), new Vector3(-1, 0, 0),new Vector3(0, 0, 0))),
            (1.0f, (new Vector3(0, 0, 0), new Vector3(+1, 0, 0),new Vector3(0, 3, 0))),
            (2.0f, (new Vector3(0, 0, 0), new Vector3(-1, 0, 0),new Vector3(0, 0, 0)))
        };

        private static (float, (Quaternion, Quaternion, Quaternion))[] _RotAnim = new[]
        {
            (0.0f, (new Quaternion(0,0,0,0), Quaternion.CreateFromYawPitchRoll(+1.6f, 0, 0), new Quaternion(0,0,0,0))),
            (1.0f, (new Quaternion(0,0,0,0), Quaternion.Identity, new Quaternion(0,0,0,0))),
            (2.0f, (new Quaternion(0,0,0,0), Quaternion.CreateFromYawPitchRoll(-1.6f, 0, 0), new Quaternion(0,0,0,0))),
            (3.0f, (new Quaternion(0,0,0,0), Quaternion.Identity, new Quaternion(0,0,0,0))),
            (4.0f, (new Quaternion(0,0,0,0), Quaternion.CreateFromYawPitchRoll(+1.6f, 0, 0), new Quaternion(0,0,0,0))),
        };

        [Test]
        public void TestVector3CubicSplineSampling()
        {
            var hermite = Transforms.AnimationSamplerFactory.Hermite(new Vector3(1, 0, 0), new Vector3(1, 2, 0), new Vector3(3, 0, 0), new Vector3(3, -2, 0), 0.5f);

            var sampler = Transforms.AnimationSamplerFactory.CreateCubicSamplerFunc(_TransAnim);

            var a = sampler(0);
            var b = sampler(1);
            var bc = sampler(1.5f);
            var c = sampler(2);
        }

        [Test]
        public void TestQuaternionCubicSplineSampling()
        {
            var sampler = Transforms.AnimationSamplerFactory.CreateCubicSamplerFunc(_RotAnim);

            var a = sampler(0);
            var b = sampler(1);
            var bc = sampler(1.5f);
            var c = sampler(2);
        }

    }
}
