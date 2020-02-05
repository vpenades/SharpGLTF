using System;
using System.Collections.Generic;
using System.Text;

namespace SharpGLTF
{
    public static class ShortcutUtils
    {
        public static string CreateLink(string localLinkPath, string targetPath)
        {
            if (string.IsNullOrWhiteSpace(localLinkPath)) throw new ArgumentNullException(nameof(localLinkPath));
            if (string.IsNullOrWhiteSpace(targetPath)) throw new ArgumentNullException(nameof(targetPath));

            if (!Uri.TryCreate(targetPath, UriKind.Absolute, out Uri uri)) throw new UriFormatException(nameof(targetPath));

            var sb = new StringBuilder();

            sb.AppendLine("[{000214A0-0000-0000-C000-000000000046}]");
            sb.AppendLine("Prop3=19,11");
            sb.AppendLine("[InternetShortcut]");
            sb.AppendLine("IDList=");
            sb.AppendLine($"URL={uri.AbsoluteUri}");

            if (uri.IsFile)
            {
                sb.AppendLine("IconIndex=1");
                string icon = targetPath.Replace('\\', '/');
                sb.AppendLine("IconFile=" + icon);
            }
            else
            {
                sb.AppendLine("IconIndex=0");
            }

            localLinkPath = System.IO.Path.ChangeExtension(localLinkPath, ".url");

            System.IO.File.WriteAllText(localLinkPath, sb.ToString());

            return localLinkPath;
        }
    }
}
