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
            RegisterExtension<MaterialPBRSpecularGlossiness_KHR>("KHR_materials_pbrSpecularGlossiness");
            RegisterExtension<MaterialUnlit_KHR>("KHR_materials_unlit");
        }

        #endregion

        #region data

        private static readonly Dictionary<string, Type> _Extensions = new Dictionary<string, Type>();

        #endregion

        #region API

        public static IEnumerable<string> SupportedExtensions => _Extensions.Keys;

        public static void RegisterExtension<T>(string persistentName)
            where T : JsonSerializable
        {
            _Extensions[persistentName] = typeof(T);
        }

        internal static JsonSerializable Create(string key)
        {
            if (!_Extensions.TryGetValue(key, out Type t)) return null;

            var instance = Activator.CreateInstance(t);

            return instance as JsonSerializable;
        }

        internal static string Identify(Type type)
        {
            foreach (var kvp in _Extensions)
            {
                if (kvp.Value == type) return kvp.Key;
            }

            return null;
        }

        #endregion
    }
}
