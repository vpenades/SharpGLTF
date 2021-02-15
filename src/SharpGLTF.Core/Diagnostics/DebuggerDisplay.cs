using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using SharpGLTF.Schema2;

namespace SharpGLTF.Diagnostics
{
    static class DebuggerDisplay
    {
        internal static string GetAttributeShortName(string attributeName)
        {
            if (attributeName == "POSITION") return "𝐏";
            if (attributeName == "NORMAL") return "𝚴";
            if (attributeName == "TANGENT") return "𝚻";
            if (attributeName == "COLOR_0") return "𝐂₀";
            if (attributeName == "COLOR_1") return "𝐂₁";
            if (attributeName == "TEXCOORD_0") return "𝐔𝐕₀";
            if (attributeName == "TEXCOORD_1") return "𝐔𝐕₁";

            if (attributeName == "JOINTS_0") return "𝐉₀";
            if (attributeName == "JOINTS_1") return "𝐉₁";

            if (attributeName == "WEIGHTS_0") return "𝐖₀";
            if (attributeName == "WEIGHTS_1") return "𝐖₁";
            return attributeName;
        }

        public static String ToReport(this Memory.MemoryAccessInfo minfo)
        {
            var txt = GetAttributeShortName(minfo.Name);
            if (minfo.ByteOffset != 0) txt += $" Offs:{minfo.ByteOffset}ᴮʸᵗᵉˢ";
            if (minfo.ByteStride != 0) txt += $" Strd:{minfo.ByteStride}ᴮʸᵗᵉˢ";
            txt += $" {minfo.Encoding.ToDebugString(minfo.Dimensions, minfo.Normalized)}[{minfo.ItemsCount}]";

            return txt;
        }

        public static string ToReport(this BufferView bv)
        {
            var path = string.Empty;

            if (bv.IsVertexBuffer) path += " VertexView";
            else if (bv.IsIndexBuffer) path += " IndexView";
            else path += " BufferView";

            var content = bv.Content;

            path += $"[{bv.LogicalIndex}ᴵᵈˣ]";
            path += $"[{content.Count}ᴮʸᵗᵉˢ]";

            if (bv.ByteStride > 0) path += $" Stride:{bv.ByteStride}ᴮʸᵗᵉˢ";

            return path;
        }

        public static string ToReportShort(this Accessor accessor)
        {
            return $"{accessor.Encoding.ToDebugString(accessor.Dimensions, accessor.Normalized)}[{accessor.Count}ᴵᵗᵉᵐˢ]";
        }

        public static string ToReportLong(this Accessor accessor)
        {
            var path = string.Empty;

            var bv = accessor.SourceBufferView;

            if (bv.IsVertexBuffer) path += "VertexBuffer";
            else if (bv.IsIndexBuffer) path += "IndexBuffer";
            else path += "BufferView";
            path += $"[{bv.LogicalIndex}ᴵᵈˣ] ⇨";

            path += $" Accessor[{accessor.LogicalIndex}ᴵᵈˣ] Offset:{accessor.ByteOffset}ᴮʸᵗᵉˢ ⇨";

            path += $" {accessor.Encoding.ToDebugString(accessor.Dimensions, accessor.Normalized)}[{accessor.Count}ᴵᵗᵉᵐˢ]";

            if (accessor.IsSparse) path += " SPARSE";

            return path;
        }

        public static string ToReport(this MeshPrimitive prim, string txt)
        {
            // gather vertex attribute information

            var vcounts = prim.VertexAccessors.Values
                .Select(item => item.Count)
                .Distinct();

            var vcount = vcounts.First();

            if (vcounts.Count() > 1)
            {
                var vAccessors = prim.VertexAccessors
                    .OrderBy(item => item.Key, Memory.MemoryAccessInfo.NameComparer)
                    .Select(item => $"{GetAttributeShortName(item.Key)}={item.Value.ToReportShort()}")
                    .ToList();

                txt += $" Vrts: {String.Join(" ", vAccessors)} ⚠️Vertex Count mismatch⚠️";
            }
            else
            {
                string toShort(string name, Accessor accessor)
                {
                    name = GetAttributeShortName(name);
                    var t = accessor.Encoding.ToDebugString(accessor.Dimensions, accessor.Normalized);
                    return $"{name}.{t}";
                }

                var vAccessors = prim.VertexAccessors
                    .OrderBy(item => item.Key, Memory.MemoryAccessInfo.NameComparer)
                    .Select(item => toShort(item.Key, item.Value))
                    .ToList();

                txt += $" Vrts: ( {String.Join(" ", vAccessors)} )[{vcount}]";
            }

            // gather index attribute information

            var indices = prim.IndexAccessor?.AsIndicesArray();
            var pcount = 0;

            switch (prim.DrawPrimitiveType)
            {
                case PrimitiveType.POINTS:
                    pcount = vcount;
                    break;
                case PrimitiveType.LINES:
                case PrimitiveType.LINE_LOOP:
                case PrimitiveType.LINE_STRIP:
                    pcount = indices.HasValue ? prim.DrawPrimitiveType.GetLinesIndices(indices.Value).Count() : prim.DrawPrimitiveType.GetLinesIndices(vcount).Count();
                    break;
                case PrimitiveType.TRIANGLES:
                case PrimitiveType.TRIANGLE_FAN:
                case PrimitiveType.TRIANGLE_STRIP:
                    pcount = indices.HasValue ? prim.DrawPrimitiveType.GetTrianglesIndices(indices.Value).Count() : prim.DrawPrimitiveType.GetTrianglesIndices(vcount).Count();
                    break;
            }

            var culture = System.Globalization.CultureInfo.CurrentCulture;

            var primName = culture.TextInfo.ToTitleCase(prim.DrawPrimitiveType.ToString().ToLower(culture));
            txt += $" {primName}[{pcount}]";

            // gather morph attributes information

            if (prim.MorphTargetsCount > 0)
            {
                txt += $" MorphTargets[{prim.MorphTargetsCount}]";
            }

            // materials

            if (prim.Material != null)
            {
                if (string.IsNullOrWhiteSpace(prim.Material.Name)) txt += $" Material[{prim.Material.LogicalIndex}]";
                else txt += "Material " + "\"" + prim.Material.Name + "\"";
            }

            return txt;
        }
    }
}
