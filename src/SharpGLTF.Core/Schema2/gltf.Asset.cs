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

        internal Asset() { }

        internal static Asset CreateDefault(string copyright)
        {
            var av = typeof(Asset).Assembly.GetCustomAttributes(true)
                .OfType<System.Reflection.AssemblyInformationalVersionAttribute>()
                .FirstOrDefault();

            var generator = av == null ? "SharpGLTF" : "SharpGLTF " + av.InformationalVersion;

            return new Asset()
            {
                _generator = generator,
                _copyright = copyright,
                _version = MAXVERSION.ToString(),
                _minVersion = MINVERSION.ToString()
            };
        }

        #endregion

        #region properties

        private static readonly Version ZEROVERSION = new Version(0, 0);
        private static readonly Version MINVERSION = new Version(2, 0);
        private static readonly Version MAXVERSION = new Version(2, 0);

        public string Copyright { get => _copyright; set => _copyright = value.AsEmptyNullable(); }
        public string Generator { get => _generator; set => _generator = value.AsEmptyNullable(); }

        public Version Version      => Version.TryParse(   _version, out Version ver) ? ver : ZEROVERSION;
        public Version MinVersion   => Version.TryParse(_minVersion, out Version ver) ? ver : ZEROVERSION;

        #endregion

        #region extra properties

        // public String Title { get => _GetExtraInfo("title"); set => _SetExtraInfo("title", value); }

        // public String Author { get => _GetExtraInfo("author"); set => _SetExtraInfo("author", value); }

        // public String License { get => _GetExtraInfo("license"); set => _SetExtraInfo("license", value); }

        #endregion

        #region API

        internal override void Validate(IList<Exception> result)
        {
            base.Validate(result);

            if (string.IsNullOrWhiteSpace(_version)) result.Add(new EXCEPTION(this, "version number is missing"));

            var curVer = this.Version;
            var minVer = this.MinVersion;

            if (curVer < MINVERSION) result.Add(new EXCEPTION(this, $"invalid version number {this.Version} expected {MINVERSION}"));
            if (curVer > MAXVERSION) result.Add(new EXCEPTION(this, $"invalid version number {this.Version} expected {MAXVERSION}"));
        }

        private string _GetExtraInfo(string key)
        {
            if (this.Extras is IReadOnlyDictionary<string, Object> dict)
            {
                return dict.TryGetValue(key, out Object val) ? val as String : null;
            }
            else
            {
                return null;
            }
        }

        private void _SetExtraInfo(string key, string val)
        {
            throw new NotImplementedException();
            // if (this.Extras == null) this.Extras = new Dictionary<string, Object>();
        }

        #endregion
    }
}