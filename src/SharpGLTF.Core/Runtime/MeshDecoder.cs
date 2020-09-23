using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using XY = System.Numerics.Vector2;
using XYZ = System.Numerics.Vector3;
using XYZW = System.Numerics.Vector4;

namespace SharpGLTF.Runtime
{
    public interface IMeshDecoder<TMaterial>
        where TMaterial : class
    {
        string Name { get; }
        int LogicalIndex { get; }
        IReadOnlyList<IMeshPrimitiveDecoder<TMaterial>> Primitives { get; }
    }

    public interface IMeshPrimitiveDecoder
    {
        #region properties

        /// <summary>
        /// Gets a value indicating the total number of vertices for this primitive.
        /// </summary>
        int VertexCount { get; }

        /// <summary>
        /// Gets a value indicating the total number of morph targets for this primitive.
        /// </summary>
        int MorphTargetsCount { get; }

        /// <summary>
        /// Gets a value indicating the number of color vertex attributes.
        /// In the range of 0 to 2.
        /// </summary>
        int ColorsCount { get; }

        /// <summary>
        /// Gets a value indicating the number of texture coordinate vertex attributes.
        /// In the range of 0 to 2.
        /// </summary>
        int TexCoordsCount { get; }

        /// <summary>
        /// Gets a value indicating the number of skinning joint-weight attributes.
        /// The values can be 0, 4 or 8.
        /// </summary>
        int JointsWeightsCount { get; }

        /// <summary>
        /// Gets a sequence of tuples where each item represents the vertex indices of a line.
        /// </summary>
        IEnumerable<(int A, int B)> LineIndices { get; }

        /// <summary>
        /// Gets a sequence of tuples where each item represents the vertex indices of a triangle.
        /// </summary>
        IEnumerable<(int A, int B, int C)> TriangleIndices { get; }

        #endregion

        #region API

        XYZ GetPosition(int vertexIndex);

        XYZ GetNormal(int vertexIndex);

        XYZW GetTangent(int vertexIndex);

        IReadOnlyList<XYZ> GetPositionDeltas(int vertexIndex);

        IReadOnlyList<XYZ> GetNormalDeltas(int vertexIndex);

        IReadOnlyList<XYZ> GetTangentDeltas(int vertexIndex);

        XY GetTextureCoord(int vertexIndex, int textureSetIndex);

        XYZW GetColor(int vertexIndex, int colorSetIndex);

        Transforms.SparseWeight8 GetSkinWeights(int vertexIndex);

        #endregion
    }

    public interface IMeshPrimitiveDecoder<TMaterial> : IMeshPrimitiveDecoder
        where TMaterial : class
    {
        TMaterial Material { get; }
    }

    /// <summary>
    /// Utility methods to help decode Meshes.
    /// </summary>
    public static class MeshDecoder
    {
        public static IMeshDecoder<Schema2.Material> Decode(this Schema2.Mesh mesh)
        {
            if (mesh == null) return null;

            var meshDecoder = new _MeshDecoder<Schema2.Material>(mesh);

            meshDecoder.GenerateNormalsAndTangents();

            return meshDecoder;
        }

        public static IMeshDecoder<Schema2.Material>[] Decode(this IReadOnlyList<Schema2.Mesh> meshes)
        {
            Guard.NotNull(meshes, nameof(meshes));
            return meshes.Select(item => item.Decode()).ToArray();
        }

        public static XYZ GetPosition(this IMeshPrimitiveDecoder primitive, int idx, Transforms.IGeometryTransform xform)
        {
            Guard.NotNull(primitive, nameof(primitive));
            Guard.MustBeBetweenOrEqualTo(idx, 0, primitive.VertexCount + 1, nameof(idx));
            Guard.NotNull(xform, nameof(xform));

            var p = primitive.GetPosition(idx);
            var d = primitive.GetPositionDeltas(idx);
            var w = primitive.GetSkinWeights(idx);

            return xform.TransformPosition(p, d, w);
        }

        public static XYZ GetNormal(this IMeshPrimitiveDecoder primitive, int idx, Transforms.IGeometryTransform xform)
        {
            Guard.NotNull(primitive, nameof(primitive));
            Guard.MustBeBetweenOrEqualTo(idx, 0, primitive.VertexCount + 1, nameof(idx));
            Guard.NotNull(xform, nameof(xform));

            var n = primitive.GetNormal(idx);
            var d = primitive.GetNormalDeltas(idx);
            var w = primitive.GetSkinWeights(idx);

            return xform.TransformNormal(n, d, w);
        }

        public static XYZW GetTangent(this IMeshPrimitiveDecoder primitive, int idx, Transforms.IGeometryTransform xform)
        {
            Guard.NotNull(primitive, nameof(primitive));
            Guard.MustBeBetweenOrEqualTo(idx, 0, primitive.VertexCount + 1, nameof(idx));
            Guard.NotNull(xform, nameof(xform));

            var t = primitive.GetTangent(idx);
            var d = primitive.GetTangentDeltas(idx);
            var w = primitive.GetSkinWeights(idx);

            return xform.TransformTangent(t, d, w);
        }

        public static (XYZ Min, XYZ Max) EvaluateBoundingBox(this Schema2.Scene scene, float samplingTimeStep = 1.0f)
        {
            Guard.NotNull(scene, nameof(scene));

            var decodedMeshes = scene.LogicalParent.LogicalMeshes.Decode();
            var sceneTemplate = SceneTemplate.Create(scene, false);
            var sceneInstance = sceneTemplate.CreateInstance();

            if (sceneInstance.AnimationTracksCount == 0)
            {
                sceneInstance.SetPoseTransforms();
                return sceneInstance.EvaluateBoundingBox(decodedMeshes);
            }

            var min = new XYZ(float.PositiveInfinity);
            var max = new XYZ(float.NegativeInfinity);

            for (int trackIdx = 0; trackIdx < sceneInstance.AnimationTracksCount; ++trackIdx)
            {
                var duration = sceneInstance.GetAnimationDuration(trackIdx);

                for (float time = 0; time < duration; time += samplingTimeStep)
                {
                    sceneInstance.SetAnimationFrame(trackIdx, time);
                    var (fMin, fMax) = sceneInstance.EvaluateBoundingBox(decodedMeshes);

                    min = XYZ.Min(min, fMin);
                    max = XYZ.Max(max, fMax);
                }
            }

            return (min, max);
        }

        public static (XYZ Center, Single Radius) EvaluateBoundingSphere(this Schema2.Scene scene, float samplingTimeStep = 1.0f)
        {
            Guard.NotNull(scene, nameof(scene));

            var decodedMeshes = scene.LogicalParent.LogicalMeshes.Decode();
            var sceneTemplate = SceneTemplate.Create(scene, false);
            var sceneInstance = sceneTemplate.CreateInstance();

            if (sceneInstance.AnimationTracksCount == 0)
            {
                sceneInstance.SetPoseTransforms();
                return sceneInstance.EvaluateBoundingSphere(decodedMeshes);
            }

            var center = XYZ.Zero;
            float radius = -1f;

            for (int trackIdx = 0; trackIdx < sceneInstance.AnimationTracksCount; ++trackIdx)
            {
                var duration = sceneInstance.GetAnimationDuration(trackIdx);

                for (float time = 0; time < duration; time += samplingTimeStep)
                {
                    sceneInstance.SetAnimationFrame(trackIdx, time);
                    var (fc, fr) = sceneInstance.EvaluateBoundingSphere(decodedMeshes);

                    _MergeSphere(ref center, ref radius, fc, fr);
                }
            }

            return (center, radius);
        }

        public static (XYZ Min, XYZ Max) EvaluateBoundingBox<TMaterial>(this SceneInstance instance, IReadOnlyList<IMeshDecoder<TMaterial>> meshes)
            where TMaterial : class
        {
            Guard.NotNull(instance, nameof(instance));
            Guard.NotNull(meshes, nameof(meshes));

            var min = new XYZ(float.PositiveInfinity);
            var max = new XYZ(float.NegativeInfinity);

            foreach (var pos in instance.GetWorldVertices(meshes))
            {
                min = XYZ.Min(min, pos);
                max = XYZ.Max(max, pos);
            }

            return (min, max);
        }

        public static (XYZ Center, Single Radius) EvaluateBoundingSphere<TMaterial>(this SceneInstance instance, IReadOnlyList<IMeshDecoder<TMaterial>> meshes)
            where TMaterial : class
        {
            Guard.NotNull(instance, nameof(instance));
            Guard.NotNull(meshes, nameof(meshes));

            var center = XYZ.Zero;
            var radius = -1f;

            foreach (var p1 in instance.GetWorldVertices(meshes))
            {
                _AddPointToSphere(ref center, ref radius, p1);
            }

            return (center, radius);
        }

        private static void _AddPointToSphere(ref XYZ c1, ref float r1, XYZ c2)
        {
            if (r1 < 0) { c1 = c2; r1 = 0; return; }

            var dir = c2 - c1;
            var len = dir.Length();
            if (len <= r1) return; // if inside, exit.

            dir /= len;
            var p1 = c1 - (dir * r1);

            c1 = (p1 + c2) / 2;
            r1 = (p1 - c2).Length() / 2;

            #if DEBUG
            var dist = (c2 - c1).Length();
            System.Diagnostics.Debug.Assert(dist <= (r1 + 0.001f));
            dist = (p1 - c1).Length();
            System.Diagnostics.Debug.Assert(dist <= (r1 + 0.001f));
            #endif
        }

        private static void _MergeSphere(ref XYZ c1, ref float r1, XYZ c2, float r2)
        {
            if (r1 < 0) { c1 = c2; r1 = r2; return; }

            var dir = c2 - c1;
            var len = dir.Length();
            if (r1 >= (r2 + len)) return; // new inside current, exit.
            if (r2 >= (r1 + len)) { c1 = c2; r1 = r2; return; } // current inside new, update & exit.

            // combine
            dir /= len;
            var p1 = c1 - (dir * r1);
            var p2 = c2 + (dir * r2);

            c1 = (p1 + p2) / 2;
            r1 = (p1 - p2).Length() / 2;

            #if DEBUG
            var dist = (c2 - c1).Length() + r2;
            System.Diagnostics.Debug.Assert(dist <= (r1 + 0.001f));
            #endif
        }

        public static IEnumerable<XYZ> GetWorldVertices<TMaterial>(this SceneInstance instance, IReadOnlyList<IMeshDecoder<TMaterial>> meshes)
            where TMaterial : class
        {
            Guard.NotNull(instance, nameof(instance));
            Guard.NotNull(meshes, nameof(meshes));

            for (int i = 0; i < meshes.Count; ++i)
            {
                Guard.MustBeEqualTo(meshes[i].LogicalIndex, i, nameof(meshes) + $"[{i}]");
            }

            return instance
                .DrawableInstances
                .Where(item => item.Transform.Visible)
                .SelectMany(item => meshes[item.Template.LogicalMeshIndex].GetWorldVertices(item.Transform));
        }

        public static IEnumerable<XYZ> GetWorldVertices<TMaterial>(this IMeshDecoder<TMaterial> mesh, Transforms.IGeometryTransform xform)
            where TMaterial : class
        {
            Guard.NotNull(mesh, nameof(mesh));
            Guard.NotNull(xform, nameof(xform));

            foreach (var primitive in mesh.Primitives)
            {
                for (int i = 0; i < primitive.VertexCount; ++i)
                {
                    yield return primitive.GetPosition(i, xform);
                }
            }
        }
    }
}
