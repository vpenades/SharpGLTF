using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

using NUnit.Framework;

namespace SharpGLTF.Transforms
{
    [Category("Core.Transforms")]
    public class InverseBindMatrixTest
    {
        [TestCase(0, 0, 0, 0, 0, 0)]
        [TestCase(1, 2, 4, 3, 2, 1)]
        [TestCase(-1, 1, 3, 2, 0, 1)]
        [TestCase(0, 0, 1, 0, 1, 0)]
        [TestCase(0, -1, 1, -2, 1, 0)]
        public void CalculateInverseBindMatrix(float mx, float my, float mz, float jx, float jy, float jz)
        {
            var model = Matrix4x4.CreateFromYawPitchRoll(mx, my, mz);            
            var joint = Matrix4x4.CreateFromYawPitchRoll(jx, jy, jz);
            joint.Translation = new Vector3(jx, jy, jz);

            var invBindMatrix = SkinTransform.CalculateInverseBinding(model, joint);

            Matrix4x4.Invert(model, out Matrix4x4 xform);            
            Matrix4x4.Invert(joint * xform, out Matrix4x4 result);
            NumericsAssert.AreEqual(result, invBindMatrix, 0.000001f);

            Matrix4x4.Invert(joint, out Matrix4x4 invJoint);
            result = model * invJoint;
            NumericsAssert.AreEqual(result, invBindMatrix, 0.000001f);
        }


    }
}
