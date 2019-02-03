using System;
using System.Collections.Generic;
using System.Text;

namespace Epsylon.glTF2Toolkit.CodeGen
{
    static class CodeEmitUtils
    {
        public static string Indent(this string text, int indent)
        {
            while (indent > 0) { text = $"\t{text}"; --indent; }

            return text;
        }

        public static void EmitLine(this StringBuilder sb, int indent, string text)
        {
            text = text.Indent(indent);
            sb.AppendLine(text);
        }

        public static void EmitBlock(this StringBuilder sb, int indent, string body)
        {
            sb.EmitLine(indent, "{");

            var lines = body.Split(new[] { "\r\n", "\r", "\n" },StringSplitOptions.None);

            foreach (var line in lines)
            {
                sb.EmitLine(indent + 1, line);
            }

            sb.EmitLine(indent, "}");
        }

        public static IEnumerable<string> AsCodeBlock(this IEnumerable<string> lines)
        {
            yield return "{";

            foreach (var l in lines) yield return $"\t{l}";

            yield return "}";
        }

        public static IEnumerable<string> Indent(this IEnumerable<string> lines, int indent)
        {
            foreach (var l in lines) yield return l.Indent(indent);            
        }
    }
}
