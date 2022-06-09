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

        public static string GetAttachmentPath(this TestContext context, string fileName, bool ensureDirectoryExists = false)
        {
            var path = System.IO.Path.Combine(context.TestDirectory, "TestResults", $"{context.Test.ID}");
            var dir = path;

            if (!string.IsNullOrWhiteSpace(fileName))
            {
                if (System.IO.Path.IsPathRooted(fileName)) throw new ArgumentException(nameof(fileName), "path must be a relative path");
                path = System.IO.Path.Combine(path, fileName);

                dir = System.IO.Path.GetDirectoryName(path);
            }

            System.IO.Directory.CreateDirectory(dir);

            return path;
        }

        

        public static void AttachShowDirLink(this TestContext context)
        {
            context.AttachLink("📂 Show Directory", context.GetAttachmentPath(string.Empty));
        }

        public static void AttachLink(this TestContext context, string linkPath, string targetPath)
        {
            linkPath = context.GetAttachmentPath(linkPath);

            linkPath = ShortcutUtils.CreateLink(linkPath, targetPath);

            TestContext.AddTestAttachment(linkPath);
        }
    }
}
