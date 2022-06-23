using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using SharpGLTF.Schema2;

using MACCESSOR = SharpGLTF.Memory.MemoryAccessor;

namespace SharpGLTF.Geometry
{
    internal static class _PackedPrimitiveHelpers
    {
        public static void _GatherMorphTargetAttributes<TMaterial>(this IPrimitiveReader<TMaterial> srcPrim, HashSet<string> attributes)
        {
            var vertexEncodings = new PackedEncoding();
            vertexEncodings.ColorEncoding = EncodingType.FLOAT;

            for (int i = 0; i < srcPrim.MorphTargets.Count; ++i)
            {
                var accessors = srcPrim._GetMorphTargetAccessors(i, vertexEncodings, new HashSet<string>());

                if (accessors.Pos != null) attributes.Add("POSITIONDELTA");
                if (accessors.Nrm != null) attributes.Add("NORMALDELTA");
                if (accessors.Tgt != null) attributes.Add("TANGENTDELTA");

                if (accessors.Col0 != null) attributes.Add("COLOR_0DELTA");
                if (accessors.Col1 != null) attributes.Add("COLOR_1DELTA");

                if (accessors.Tuv0 != null) attributes.Add("TEXCOORD_0DELTA");
                if (accessors.Tuv1 != null) attributes.Add("TEXCOORD_1DELTA");
                if (accessors.Tuv2 != null) attributes.Add("TEXCOORD_2DELTA");
                if (accessors.Tuv3 != null) attributes.Add("TEXCOORD_3DELTA");
            }
        }

        public static (MACCESSOR Pos, MACCESSOR Nrm, MACCESSOR Tgt, MACCESSOR Col0, MACCESSOR Col1, MACCESSOR Tuv0, MACCESSOR Tuv1, MACCESSOR Tuv2, MACCESSOR Tuv3) _GetMorphTargetAccessors<TMaterial>(this IPrimitiveReader<TMaterial> srcPrim, int morphTargetIdx, PackedEncoding vertexEncodings, ISet<string> requiredAttributes)
        {
            var mtv = srcPrim.MorphTargets[morphTargetIdx].GetMorphTargetVertices(srcPrim.Vertices.Count);

            MACCESSOR _createAccessor(string attributeName)
            {
                var accessor = VertexTypes.VertexUtils.CreateVertexMemoryAccessor(mtv, attributeName, vertexEncodings);
                if (accessor == null) return null;

                if (requiredAttributes.Contains(attributeName)) return accessor; // required attribute, even if all deltas are zero

                // if delta is all 0s, and it's not required, then do not use the accessor
                return accessor.Data.All(b => b == 0) ? null : accessor;
            }

            var pAccessor = _createAccessor("POSITIONDELTA");
            var nAccessor = _createAccessor("NORMALDELTA");
            var tAccessor = _createAccessor("TANGENTDELTA");

            var c0Accessor = _createAccessor("COLOR_0DELTA");
            var c1Accessor = _createAccessor("COLOR_1DELTA");

            var uv0Accessor = _createAccessor("TEXCOORD_0DELTA");
            var uv1Accessor = _createAccessor("TEXCOORD_1DELTA");
            var uv2Accessor = _createAccessor("TEXCOORD_2DELTA");
            var uv3Accessor = _createAccessor("TEXCOORD_3DELTA");

            return (pAccessor, nAccessor, tAccessor, c0Accessor, c1Accessor, uv0Accessor, uv1Accessor, uv2Accessor, uv3Accessor);
        }
    }
}
