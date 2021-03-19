using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace SharpGLTF.Geometry.VertexTypes
{
    static partial class VertexUtils
    {
        private static readonly char[] _SubscriptNumbers = new char[] { '₀', '₁', '₂', '₃', '₄', '₅', '₆', '₇', '₈', '₉' };

        public static string _GetDebuggerDisplay(IVertexGeometry geo)
        {
            var txt = $"𝐏:{geo.GetPosition()}";

            if (geo.TryGetNormal(out Vector3 n)) txt += $" 𝚴:{n}";
            if (geo.TryGetTangent(out Vector4 t)) txt += $" 𝚻:{t}";

            return txt;
        }

        public static string _GetDebuggerDisplay(IVertexMaterial mat)
        {
            var txt = string.Empty;

            for (int i = 0; i < mat.MaxColors; ++i)
            {
                if (txt.Length > 0) txt += " ";
                txt += $"𝐂{_SubscriptNumbers[i]}:{mat.GetColor(i)}";
            }

            for (int i = 0; i < mat.MaxTextCoords; ++i)
            {
                if (txt.Length > 0) txt += " ";
                txt += $"𝐔𝐕{_SubscriptNumbers[i]}:{mat.GetTexCoord(i)}";
            }

            return txt;
        }

        public static string _GetDebuggerDisplay(IVertexSkinning skin)
        {
            var txt = string.Empty;

            for (int i = 0; i < skin.MaxBindings; ++i)
            {
                var (joint, weight) = skin.GetBinding(i);
                if (weight == 0) continue;

                if (txt.Length != 0) txt += " ";

                txt += $"<𝐉:{joint} 𝐖:{weight}>";
            }

            return txt;
        }
    }
}
