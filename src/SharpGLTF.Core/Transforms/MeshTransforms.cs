using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

using TRANSFORM = System.Numerics.Matrix4x4;
using V3 = System.Numerics.Vector3;
using V4 = System.Numerics.Vector4;

namespace SharpGLTF.Transforms
{
    public interface ITransform
    {
        V3 TransformPosition(V3 position, params (int, float)[] skinWeights);
        V3 TransformNormal(V3 normal, params (int, float)[] skinWeights);
        V4 TransformTangent(V4 tangent, params (int, float)[] skinWeights);

        V3 TransformPosition(V3[] positions, params (int, float)[] skinWeights);
        V3 TransformNormal(V3[] normals, params (int, float)[] skinWeights);
        V4 TransformTangent(V4[] tangents, params (int, float)[] skinWeights);
    }

    public abstract class MorphTransform
    {
        #region constructor

        protected MorphTransform(float[] morphWeights)
        {
            if (morphWeights == null || morphWeights.Length == 0)
            {
                _MorphWeights = _NoWeights;
                return;
            }

            _MorphWeights = new float[morphWeights.Length];
            morphWeights.CopyTo(_MorphWeights, 0);
        }

        #endregion

        #region data

        private readonly float[] _MorphWeights;

        private static readonly float[] _NoWeights = new float[] { 1 };

        #endregion

        #region properties

        public IReadOnlyList<float> MorphWeights => _MorphWeights;

        #endregion

        #region API

        protected V3 MorphPositions(V3[] positions)
        {
            if (_MorphWeights == null) return positions[0];
            Guard.IsTrue(_MorphWeights.Length == positions.Length, nameof(positions));

            var p = V3.Zero;
            for (int i = 0; i < _MorphWeights.Length; ++i)
            {
                p += positions[i] * _MorphWeights[i];
            }

            return p;
        }

        protected V3 MorphNormals(V3[] normals)
        {
            if (_MorphWeights == null) return normals[0];
            Guard.IsTrue(_MorphWeights.Length == normals.Length, nameof(normals));

            var n = V3.Zero;
            for (int i = 0; i < _MorphWeights.Length; ++i)
            {
                n += normals[i] * _MorphWeights[i];
            }

            return V3.Normalize(n);
        }

        protected V4 MorphTangents(V4[] tangents)
        {
            if (_MorphWeights == null) return tangents[0];
            Guard.IsTrue(_MorphWeights.Length == tangents.Length, nameof(tangents));

            var t = V4.Zero;
            for (int i = 0; i < _MorphWeights.Length; ++i)
            {
                t += tangents[i] * _MorphWeights[i];
            }

            return t;
        }

        #endregion
    }

    public class StaticTransform : MorphTransform , ITransform
    {
        public StaticTransform(TRANSFORM xform, params float[] morphWeights)
            : base(morphWeights)
        {
            _Transform = xform;
        }

        private readonly TRANSFORM _Transform;

        public V3 TransformPosition(V3 position, params (int, float)[] skinWeights)
        {
            return V3.Transform(position, _Transform);
        }

        public V3 TransformNormal(V3 normal, params (int, float)[] skinWeights)
        {
            return V3.Normalize(V3.Transform(normal, _Transform));
        }

        public V4 TransformTangent(V4 tangent, params (int, float)[] skinWeights)
        {
            return V4.Transform(tangent, _Transform);
        }

        public V3 TransformPosition(V3[] positions, params (int, float)[] skinWeights)
        {
            var position = MorphPositions(positions);

            return V3.Transform(position, _Transform);
        }

        public V3 TransformNormal(V3[] normals, params (int, float)[] skinWeights)
        {
            var normal = MorphNormals(normals);

            return V3.Normalize(V3.TransformNormal(normal, _Transform));
        }

        public V4 TransformTangent(V4[] tangents, params (int, float)[] skinWeights)
        {
            var tangent = MorphTangents(tangents);

            var tangentV = new V3(tangent.X, tangent.Y, tangent.Z);

            tangentV = V3.TransformNormal(tangentV, _Transform);

            return new V4(tangentV, tangent.W);
        }
    }

    public class SkinTransform : MorphTransform , ITransform
    {
        public SkinTransform(TRANSFORM[] invBindings, TRANSFORM[] xforms, params float[] morphWeights)
            : base(morphWeights)
        {
            Guard.NotNull(invBindings, nameof(invBindings));
            Guard.NotNull(xforms, nameof(xforms));
            Guard.IsTrue(invBindings.Length == xforms.Length, nameof(xforms), $"{invBindings} and {xforms} length mismatch.");

            _JointTransforms = new TRANSFORM[invBindings.Length];

            for (int i = 0; i < _JointTransforms.Length; ++i)
            {
                _JointTransforms[i] = invBindings[i] * xforms[i];
            }
        }

        private readonly TRANSFORM[] _JointTransforms;

        public V3 TransformPosition(V3 localPosition, params (int, float)[] skinWeights)
        {
            var worldPosition = V3.Zero;

            foreach (var jw in skinWeights)
            {
                worldPosition += V3.Transform(localPosition, _JointTransforms[jw.Item1]) * jw.Item2;
            }

            return worldPosition;
        }

        public V3 TransformNormal(V3 localNormal, params (int, float)[] skinWeights)
        {
            var worldNormal = V3.Zero;

            foreach (var jw in skinWeights)
            {
                worldNormal += V3.TransformNormal(localNormal, _JointTransforms[jw.Item1]) * jw.Item2;
            }

            return V3.Normalize(localNormal);
        }

        public V4 TransformTangent(V4 localTangent, params (int, float)[] skinWeights)
        {
            var localTangentV = new V3(localTangent.X, localTangent.Y, localTangent.Z);
            var worldTangent = V3.Zero;

            foreach (var jw in skinWeights)
            {
                worldTangent += V3.TransformNormal(localTangentV, _JointTransforms[jw.Item1]) * jw.Item2;
            }

            return new V4(worldTangent, localTangentV.Z);
        }

        public V3 TransformPosition(V3[] positions, params (int, float)[] skinWeights)
        {
            return TransformPosition(MorphPositions(positions));
        }

        public V3 TransformNormal(V3[] normals, params (int, float)[] skinWeights)
        {
            return TransformNormal(MorphNormals(normals));
        }

        public V4 TransformTangent(V4[] tangents, params (int, float)[] skinWeights)
        {
            return TransformTangent(MorphTangents(tangents));
        }
    }
}
