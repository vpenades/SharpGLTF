using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace SharpGLTF
{
    /// <summary>
    /// Wraps Khronos GLTF Validator command line tool.
    /// </summary>
    /// <see href="https://github.com/KhronosGroup/glTF-Validator"/>
    /// <remarks>
    /// LINUX execution path has not been tested!
    /// </remarks>
    public static class gltf_validator
    {
        static gltf_validator()
        {
            if (RuntimeInformation.OSArchitecture != Architecture.X64) return;

            ValidatorExePath = System.IO.Path.GetDirectoryName(typeof(gltf_validator).Assembly.Location);

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                ValidatorExePath = System.IO.Path.Combine(ValidatorExePath, "gltf_validator.exe");
            }

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                ValidatorExePath = System.IO.Path.Combine(ValidatorExePath, "gltf_validator");
            }
        }

        public static string ValidatorExePath { get; set; }

        public static ValidationReport ValidateFile(string gltfFilePath)
        {
            if (string.IsNullOrWhiteSpace(ValidatorExePath)) return null;

            if (!System.IO.File.Exists(ValidatorExePath)) throw new System.IO.FileNotFoundException(ValidatorExePath);

            if (!System.IO.Path.IsPathRooted(gltfFilePath)) gltfFilePath = System.IO.Path.GetFullPath(gltfFilePath);

            var psi = new System.Diagnostics.ProcessStartInfo(ValidatorExePath);
            psi.Arguments = $"-p -r -a \"{gltfFilePath}\"";
            psi.UseShellExecute = false;            
            psi.RedirectStandardError = true;

            using (var p = System.Diagnostics.Process.Start(psi))
            {
                if (!p.WaitForExit(1000 * 10))
                {
                    try { p.Kill(); } catch { }
                }

                var rawReport = p.StandardError.ReadToEnd();

                if (string.IsNullOrWhiteSpace(rawReport)) return null;

                return new ValidationReport(gltfFilePath, rawReport);
            }
        }

        public sealed class ValidationReport
        {
            internal ValidationReport(string srcPath, string rawReport)
            {
                var lines = rawReport.Split('\n');

                var status = 0;

                var www = new List<string>();
                var eee = new List<string>();

                foreach (var l in lines)
                {
                    if (l == "\tWarnings:") { status = 1; continue; }
                    if (l == "\tErrors:") { status = 2; continue; }

                    if (status == 1) www.Add(l.Trim());
                    if (status == 2) eee.Add(l.Trim());
                }

                FilePath = srcPath;
                RawReport = rawReport;
                Warnings = www;
                Errors = eee;
            }

            public string RawReport { get; private set; }

            public readonly IReadOnlyList<String> Warnings;
            public readonly IReadOnlyList<String> Errors;

            public string FilePath { get; private set; }

            public bool HasWarnings => Warnings.Count > 0;
            public bool HasErrors => Errors.Count > 0;

            public override string ToString()
            {
                return RawReport;

                var sb = new StringBuilder();

                sb.AppendLine(FilePath);

                if (HasWarnings)
                {
                    sb.AppendLine("\tWarnings:");
                    foreach (var w in Warnings) sb.AppendLine($"\t\t{w}");
                }

                if (HasErrors)
                {
                    sb.AppendLine("\tErrors:");
                    foreach (var e in Errors) sb.AppendLine($"\t\t{e}");
                }

                return sb.ToString();
            }
        }
    }
}
