using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SharpGLTF
{
    /// <summary>
    /// Represents the resulting report of a glTF validation.
    /// </summary>
    public sealed class ValidationReport
    {
        public static ValidationReport Load(string filePath)
        {
            var json = System.IO.File.ReadAllText(filePath);
            return Parse(json);
        }

        public static ValidationReport Parse(string json)
        {
            var options = new JsonSerializerOptions();
            options.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;

            return JsonSerializer.Deserialize<ValidationReport>(json, options);
        }

        public string Uri { get; set; }
        public string MimeType { get; set; }
        public string ValidatorVersion { get; set; }
        public ValidationIssues Issues { get; set; }
        public ValidationInfo Info { get; set; }
    }

    public sealed class ValidationIssues
    {
        public int NumErrors { get; set; }
        public int NumWarnings { get; set; }
        public int NumInfos { get; set; }
        public int NumHints { get; set; }
        public ValidationMessage[] Messages { get; set; }
        public bool Truncated { get; set; }
    }

    [System.Diagnostics.DebuggerDisplay("{Code} {Severity} {Message}")]
    public sealed class ValidationMessage
    {
        public string Code { get; set; }
        public string Message { get; set; }
        public int Severity { get; set; }
        public string Pointer { get; set; }
    }

    public sealed class ValidationInfo
    {
        public string Version { get; set; }
        public string Generator { get; set; }
        public int AnimationCount { get; set; }
        public int MaterialCount { get; set; }
        public bool HasMorphTargets { get; set; }
        public bool HasSkins { get; set; }
        public bool HasTextures { get; set; }
        public bool HasDefaultScene { get; set; }
        public int DrawCallCount { get; set; }
        public int TotalTriangleCount { get; set; }
        public int MaxUVs { get; set; }
        public int MaxInfluences { get; set; }
        public int MaxAttributes { get; set; }
    }
}
