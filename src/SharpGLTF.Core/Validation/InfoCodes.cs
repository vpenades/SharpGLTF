using System;
using System.Collections.Generic;
using System.Text;

namespace SharpGLTF.Validation
{
    static class InfoCodes
    {
        #region DATA

        public const string ACCESSOR_INDEX_TRIANGLE_DEGENERATE = "Indices accessor contains {0} degenerate triangles.";
        public const string DATA_URI_GLB = "Data URI is used in GLB container.";
        public const string IMAGE_NPOT_DIMENSIONS = "Image has non-power-of-two dimensions: {0}x{1}.";

        #endregion

        #region LINK

        public const string MESH_PRIMITIVE_UNUSED_TEXCOORD = "Material does not use texture coordinates sets with indices ('%a', '%b', '%c').";
        public const string UNUSED_OBJECT = "This object may be unused.";

        #endregion

        #region SEMANTIC

        public const string EXTRA_PROPERTY = "This property should not be defined as it will not be used.";
        public const string NODE_EMPTY = "Empty node encountered.";
        public const string NODE_MATRIX_DEFAULT = "Do not specify default transform matrix.";
        public const string NON_OBJECT_EXTRAS = "Prefer JSON Objects for extras.";

        #endregion
    }
}
