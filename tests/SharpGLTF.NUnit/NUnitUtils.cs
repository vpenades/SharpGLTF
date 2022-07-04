using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using NUnit.Framework;

namespace SharpGLTF
{
    public static class NUnitUtils
    {
        public static string ToShortDisplayPath(this string path)
        {
            var dir = System.IO.Path.GetDirectoryName(path);
            var fxt = System.IO.Path.GetFileName(path);

            const int maxdir = 12;

            if (dir.Length > maxdir)
            {
                dir = "..." + dir.Substring(dir.Length - maxdir);
            }

            return System.IO.Path.Combine(dir, fxt);
        }             
    }
}
