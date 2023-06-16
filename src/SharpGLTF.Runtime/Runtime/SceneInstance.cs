using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using SharpGLTF.Transforms;

using XFORM = System.Numerics.Matrix4x4;

namespace SharpGLTF.Runtime
{
    /// <summary>
    /// Represents a specific and independent state of a <see cref="SceneTemplate"/>.
    /// </summary>
    public sealed class SceneInstance : IReadOnlyList<DrawableInstance>
    {
        #region lifecycle

        internal SceneInstance(ArmatureTemplate armature, DrawableTemplate[] drawables)
        {
            Guard.NotNull(armature, nameof(armature));
            Guard.NotNull(drawables, nameof(drawables));

            _Armature = new ArmatureInstance(armature);

            _DrawableReferences = drawables;
            _DrawableTransforms = new IGeometryTransform[_DrawableReferences.Length];

            for (int i = 0; i < _DrawableTransforms.Length; ++i)
            {
                _DrawableTransforms[i] = _DrawableReferences[i].CreateGeometryTransform();
            }
        }

        #endregion

        #region data

        /// <summary>
        /// Represents the skeleton that's going to be used by each drawing command to draw the model matrices.
        /// </summary>
        [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
        private readonly ArmatureInstance _Armature;

        [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
        private readonly DrawableTemplate[] _DrawableReferences;

        [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
        private readonly IGeometryTransform[] _DrawableTransforms;

        #endregion

        #region properties

        public ArmatureInstance Armature => _Armature;

        /// <inheritdoc/>
        public int Count => _DrawableTransforms.Length;

        /// <inheritdoc/>
        public DrawableInstance this[int index] => GetDrawableInstance(index);

        #endregion

        #region API

        /// <summary>
        /// Gets a <see cref="DrawableInstance"/> object, where:
        /// - Name is the name of this drawable instance. Originally, it was the name of <see cref="Schema2.Node"/>.
        /// - MeshIndex is the logical Index of a <see cref="Schema2.Mesh"/> in <see cref="Schema2.ModelRoot.LogicalMeshes"/>.
        /// - Transform is an <see cref="IGeometryTransform"/> that can be used to transform the <see cref="Schema2.Mesh"/> into world space.
        /// </summary>
        /// <param name="index">The index of the drawable reference, from 0 to <see cref="DrawableInstancesCount"/></param>
        /// <returns><see cref="DrawableInstance"/> object.</returns>
        public DrawableInstance GetDrawableInstance(int index)
        {
            var dref = _DrawableReferences[index];

            dref.UpdateGeometryTransform(_DrawableTransforms[index], _Armature);

            return new DrawableInstance(dref, _DrawableTransforms[index]);
        }

        public IEnumerator<DrawableInstance> GetEnumerator()
        {
            for (int i = 0; i < Count; ++i) { yield return this[i]; }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            for (int i = 0; i < Count; ++i) { yield return this[i]; }
        }

        #endregion

        #region obsolete

        [Obsolete("Use .Count", true)]
        [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
        public int DrawableInstancesCount => Count;

        [Obsolete("use <this>", true)]
        [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
        public IEnumerable<DrawableInstance> DrawableInstances
        {
            get
            {
                for (int i = 0; i < Count; ++i) { yield return this[i]; }
            }
        }

        #endregion
    }
}
