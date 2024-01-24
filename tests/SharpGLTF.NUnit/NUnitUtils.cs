using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using NUnit.Framework;

namespace SharpGLTF
{
    public static class NUnitUtils
    {
        public static string ToShortDisplayPath(this string path, int maxDirLength = 12)
        {
            var dir = System.IO.Path.GetDirectoryName(path);
            var fxt = System.IO.Path.GetFileName(path);            

            if (dir.Length > maxDirLength)
            {
                dir = "..." + dir.Substring(dir.Length - maxDirLength);
            }

            return System.IO.Path.Combine(dir, fxt);
        }             
    }
}
