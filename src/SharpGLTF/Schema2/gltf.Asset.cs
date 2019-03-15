using System;
using System.Collections.Generic;
using System.Linq;

namespace SharpGLTF.Schema2
{
    using EXCEPTION = IO.ModelException;

    [System.Diagnostics.DebuggerDisplay("{Version} {MinVersion} {Generator} {Copyright}")]
    public sealed partial class Asset
    {
        #region lifecycle

        internal static Asset CreateDefault(string copyright)
        {
            return new Asset()
            {
                _generator = "SharpGLTF",
                _copyright = copyright,
                _version = MAXVERSION.ToString(),
                _minVersion = MINVERSION.ToString()
            };
        }

        #endregion

        #region properties

        private static readonly Version ZEROVERSION = new Version(0, 0);
        private static readonly Version MINVERSION = new Version(2, 0);
        private static readonly Version MAXVERSION = new Version(2, 1);

        public string Copyright { get => _copyright; set => _copyright = value.AsEmptyNullable(); }
        public string Generator { get => _generator; set => _generator = value.AsEmptyNullable(); }

        public Version Version      => Version.TryParse(   _version, out Version ver) ? ver : ZEROVERSION;
        public Version MinVersion   => Version.TryParse(_minVersion, out Version ver) ? ver : ZEROVERSION;

        #endregion

        #region API

        public override IEnumerable<Exception> Validate()
        {
            foreach (var ex in base.Validate()) yield return ex;

            if (string.IsNullOrWhiteSpace(_version)) yield return new EXCEPTION(this, "version number is missing");

            var curVer = this.Version;
            var minVer = this.MinVersion;

            if (curVer < MINVERSION) yield return new EXCEPTION(this, $"invalid version number {this.Version} expected {MINVERSION}");
            if (curVer > MAXVERSION) yield return new EXCEPTION(this, $"invalid version number {this.Version} expected {MAXVERSION}");
        }

        #endregion
    }
}