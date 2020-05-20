using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;

using SharpGLTF.IO;

namespace SharpGLTF.Schema2
{
    static class ExtensionsFactory
    {
        // extensions design inconsistencies:
        // https://github.com/KhronosGroup/glTF/issues/1491

        #region supported extensions must be registered here

        static ExtensionsFactory()
        {
            RegisterExtension<Material, MaterialPBRSpecularGlossiness>("KHR_materials_pbrSpecularGlossiness");

            RegisterExtension<Material, MaterialUnlit>("KHR_materials_unlit");
            RegisterExtension<Material, MaterialClearCoat>("KHR_materials_clearcoat");

            RegisterExtension<ModelRoot, KHR_lights_punctualglTFextension>("KHR_lights_punctual");
            RegisterExtension<Node, KHR_lights_punctualnodeextension>("KHR_lights_punctual");

            RegisterExtension<TextureInfo, TextureTransform>("KHR_texture_transform");

            RegisterExtension<Texture, TextureDDS>("MSFT_texture_dds");
            RegisterExtension<Texture, TextureWEBP>("EXT_texture_webp");
        }

        #endregion

        #region data

        private static readonly List<(string Name, Type ParentType, Type ExtType)> _Extensions = new List<(string, Type, Type)>();

        #endregion

        #region API

        public static IEnumerable<string> SupportedExtensions => _Extensions.Select(item => item.Name);

        public static void RegisterExtension<TParent, TExtension>(string persistentName)
            where TParent : JsonSerializable
            where TExtension : JsonSerializable
        {
            _Extensions.Add( (persistentName, typeof(TParent), typeof(TExtension)) );
        }

        internal static JsonSerializable Create(JsonSerializable parent, string key)
        {
            var ptype = parent.GetType();

            var (name, parentType, extType) = _Extensions.FirstOrDefault(item => item.Name == key && item.ParentType == ptype);

            if (name == null) return null;

            var instance = Activator.CreateInstance
                (
                extType,
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance,
                null,
                new Object[] { parent },
                null
                );

            return instance as JsonSerializable;
        }

        internal static string Identify(Type parentType, Type extensionType)
        {
            foreach (var entry in _Extensions)
            {
                if (entry.ParentType == parentType && entry.ExtType == extensionType) return entry.Name;
            }

            return null;
        }

        #endregion
    }

    partial class ModelRoot
    {
        #region properties

        public bool MeshQuantizationAllowed { get; private set; }

        #endregion

        #region API

        /// <summary>
        /// Immediatelly called after deserialization, it assigns
        /// </summary>
        private void _FindMeshQuantizationExtension()
        {
            MeshQuantizationAllowed = this._extensionsRequired.Contains("KHR_mesh_quantization");
        }

        internal void UpdateExtensionsSupport()
        {
            var used = RetrieveUsedExtensions();

            // update the used list
            this._extensionsUsed.Clear();
            this._extensionsUsed.AddRange(used);

            _SetRequiredExtension("KHR_mesh_quantization", MeshQuantizationAllowed);
        }

        private void _SetRequiredExtension(string extension, bool enabled)
        {
            if (!enabled) { this._extensionsRequired.Remove(extension); return; }

            if (this._extensionsRequired.Contains(extension)) return;
            this._extensionsRequired.Add(extension);
        }

        internal IEnumerable<ExtraProperties> GetLogicalChildrenFlattened()
        {
            return GetLogicalChildren()
                .SelectMany(item => ExtraProperties.Flatten(item)
                .ToList());
        }

        internal IEnumerable<string> RetrieveUsedExtensions()
        {
            // retrieve ALL the property based objects of the whole model.
            var allObjects = new[] { this }.Concat(GetLogicalChildrenFlattened());

            // check all the extensions used by each object
            var used = new HashSet<string>();

            // search for known extensions
            foreach (var c in allObjects)
            {
                var ids = c.Extensions
                    .Select(item => ExtensionsFactory.Identify(c.GetType(), item.GetType()))
                    .Where(item => !string.IsNullOrWhiteSpace(item));

                used.UnionWith(ids);
            }

            // search for unknown extensions
            foreach (var unk in allObjects.SelectMany(item => item.Extensions).OfType<UnknownNode>())
            {
                used.Add(unk.Name);
            }

            return used;
        }

        internal void UsingExtension(Type parentType, Type extensionType)
        {
            var id = ExtensionsFactory.Identify(parentType, extensionType);
            if (string.IsNullOrWhiteSpace(id)) return;

            if (this._extensionsUsed.Contains(id)) return;

            this._extensionsUsed.Add(id);
        }

        #endregion
    }
}
