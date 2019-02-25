using System;
using System.Collections.Generic;
using System.Text;

namespace SharpGLTF.Schema2
{
    using IO;

    static class ExtensionsFactory
    {
        #region supported extensions must be registered here

        static ExtensionsFactory()
        {
            RegisterExtension("KHR_materials_pbrSpecularGlossiness", () => new MaterialPBRSpecularGlossiness_KHR());
            RegisterExtension("KHR_materials_unlit", () => new MaterialUnlit_KHR());
        }

        #endregion

        #region data

        private static readonly Dictionary<string, Func<JsonSerializable>> _Extensions = new Dictionary<string, Func<JsonSerializable>>();

        #endregion

        #region API

        public static IEnumerable<string> SupportedExtensions => _Extensions.Keys;

        public static void RegisterExtension(string persistentName, Func<JsonSerializable> activator)
        {
            _Extensions[persistentName] = activator;
        }

        internal static JsonSerializable Create(string key)
        {
            if (!_Extensions.TryGetValue(key, out Func<JsonSerializable> activator)) return null;

            return activator.Invoke();
        }

        #endregion
    }
}
