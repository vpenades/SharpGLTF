using System;
using System.Collections.Generic;
using System.Linq;

namespace glTF2Sharp.Schema2
{
    [System.Diagnostics.DebuggerDisplay("{Version} {MinVersion} {Generator} {Copyright}")]
    public partial class Asset
    {
        #region lifecycle

        internal static Asset CreateDefault(string copyright)
        {
            return new Asset()
            {
                _generator = "glTF2Sharp",
                _copyright = copyright,
                _version = MAXVERSION.ToString()
            };
        }

        #endregion

        #region properties

        private static readonly Version ZEROVERSION = new Version(0, 0);

        private static readonly Version MINVERSION = new Version(2, 0);
        private static readonly Version MAXVERSION = new Version(2, 0);

        public string Copyright { get => _copyright; set => _copyright = value; }
        public string Generator { get => _generator; set => _generator = value; }

        public Version Version { get => Version.TryParse(_version, out Version ver) ? ver : ZEROVERSION; }
        public Version MinVersion { get => Version.TryParse(_minVersion, out Version ver) ? ver : ZEROVERSION; }

        #endregion

        #region API

        public override IEnumerable<Exception> Validate()
        {
            foreach (var ex in base.Validate()) yield return ex;

            if (string.IsNullOrWhiteSpace(_version)) yield return new ModelException(this, "version number is missing");

            var curVer = this.Version;
            var minVer = this.MinVersion;

            if (curVer < MINVERSION) yield return new ModelException(this, $"invalid version number {this.Version} expected {MINVERSION}");
            if (curVer > MAXVERSION) yield return new ModelException(this, $"invalid version number {this.Version} expected {MAXVERSION}");
        }

        #endregion
    }
}