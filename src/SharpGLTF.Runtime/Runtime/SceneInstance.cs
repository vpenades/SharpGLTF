using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using SharpGLTF.Transforms;

namespace SharpGLTF.Runtime
{
    /// <summary>
    /// Represents a specific and independent state of a <see cref="SceneTemplate"/>.
    /// </summary>
    public sealed class SceneInstance : IEnumerable<DrawableInstance>
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

        #endregion

        #region API

        IEnumerator IEnumerable.GetEnumerator() { return GetEnumerator(); }

        public IEnumerator<DrawableInstance> GetEnumerator()
        {
            for (int i = 0; i < _DrawableTransforms.Length; ++i)
            {
                var dref = _DrawableReferences[i];

                if (_Armature.LogicalNodes[dref.LogicalNodeIndex].IsVisible == false) continue;

                dref.UpdateGeometryTransform(_DrawableTransforms[i], _Armature);

                yield return new DrawableInstance(dref, _DrawableTransforms[i]);
            }
        }

        #endregion        
    }
}
