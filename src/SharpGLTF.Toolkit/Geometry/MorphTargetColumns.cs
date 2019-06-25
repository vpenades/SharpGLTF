using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace SharpGLTF.Geometry
{
    /// <summary>
    /// Represents a collection of vertex attribute columns to be used as morph targets.
    /// </summary>
    /// <see href="https://github.com/KhronosGroup/glTF/blob/master/specification/2.0/README.md#morph-targets"/>
    public class MorphTargetColumns
    {
        #region lifecycle

        internal MorphTargetColumns() { }

        #endregion

        #region columns

        public IList<Vector3> Positions { get; set; }
        public IList<Vector3> Normals { get; set; }

        /// <remarks>
        /// Note that the w component for handedness is omitted when targeting TANGENT data since handedness cannot be displaced.
        /// </remarks>
        public IList<Vector3> Tangents { get; set; }

        /// <remarks>
        /// glTF v2 specification does not forbid morphing Color0 attribute, but it also states that it is not required by engines
        /// to support it.
        /// </remarks>
        public IList<Vector4> Colors0 { get; set; }

        #endregion

        #region API

        private static IList<T> _IsolateColumn<T>(IList<T> column)
        {
            if (column == null) return null;

            var newColumn = new T[column.Count];

            column.CopyTo(newColumn, 0);

            return newColumn;
        }

        /// <summary>
        /// Performs an in-place copy of the contents of every column,
        /// which guarantees that the columns of this <see cref="MorphTargetColumns"/>
        /// are not shared by any other object and can be modified safely.
        /// </summary>
        public void IsolateColumns()
        {
            this.Positions = _IsolateColumn(this.Positions);
            this.Normals = _IsolateColumn(this.Normals);
            this.Tangents = _IsolateColumn(this.Tangents);

            this.Colors0 = _IsolateColumn(this.Colors0);
        }

        #endregion
    }
}
