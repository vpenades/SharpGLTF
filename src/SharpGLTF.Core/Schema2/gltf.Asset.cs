using System;
using System.Collections.Generic;
using System.Linq;

using SharpGLTF.Collections;
using SharpGLTF.IO;

namespace SharpGLTF.Schema2
{
    [System.Diagnostics.DebuggerDisplay("{Version} {MinVersion} {Generator} {Copyright}")]
    public sealed partial class Asset : Collections.IChildOf<ModelRoot>
    {
        #region lifecycle

        internal Asset() { }

        internal static Asset CreateDefault(string copyright)
        {
            var av = AssemblyInformationalVersion;

            var generator = string.IsNullOrWhiteSpace(av) ? "SharpGLTF" : "SharpGLTF " + av;

            return new Asset()
            {
                _generator = generator,
                _copyright = copyright,
                _version = MINVERSION.ToString(),
                _minVersion = null
            };
        }        

        void IChildOf<ModelRoot>.SetLogicalParent(ModelRoot parent) { LogicalParent = parent; }        

        public ModelRoot LogicalParent { get; private set; }

        #endregion        

        #region properties        

        public static string AssemblyInformationalVersion
        {
            get
            {
                var av = typeof(Asset).Assembly.GetCustomAttributes(true)
                .OfType<System.Reflection.AssemblyInformationalVersionAttribute>()
                .FirstOrDefault();

                return av?.InformationalVersion ?? string.Empty;
            }
        }

        private static readonly Version ZEROVERSION = new Version(0, 0);
        private static readonly Version MINVERSION = new Version(2, 0);
        private static readonly Version MAXVERSION = new Version(2, 0);

        public string Copyright { get => _copyright; set => _copyright = value.AsEmptyNullable(); }
        public string Generator { get => _generator; set => _generator = value.AsEmptyNullable(); }

        public Version Version => Version.TryParse(_version, out Version ver) ? ver : ZEROVERSION;

        public Version MinVersion => Version.TryParse(_minVersion, out Version ver) ? ver : MINVERSION;        

        #endregion

        #region extra properties

        // public String Title { get => _GetExtraInfo("title"); set => _SetExtraInfo("title", value); }

        // public String Author { get => _GetExtraInfo("author"); set => _SetExtraInfo("author", value); }

        // public String License { get => _GetExtraInfo("license"); set => _SetExtraInfo("license", value); }

        #endregion

        #region Validation

        protected override void OnValidateReferences(Validation.ValidationContext validate)
        {
            base.OnValidateReferences(validate);

            validate.IsTrue(nameof(Version), Version.TryParse(_version, out Version ver), $"Unknown glTF major asset version: {_version}.");

            validate.IsGreaterOrEqual(nameof(Version), Version, MINVERSION);

            // if (MinVersion > MAXVERSION) result.AddSemanticError( $"Maximum supported version is {MAXVERSION} but found:{MinVersion}");
        }                

        #endregion
    }

    partial class ModelRoot
    {
        public Asset Asset
        {
            get => _asset;
            set => SetProperty(this, ref _asset, value);
        }
    }
}