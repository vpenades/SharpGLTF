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
            RegisterExtension<ModelRoot, _ModelPunctualLights>("KHR_lights_punctual");

            RegisterExtension<Node, _NodePunctualLight>("KHR_lights_punctual");

            RegisterExtension<Material, MaterialUnlit>("KHR_materials_unlit");
            RegisterExtension<Material, MaterialSheen>("KHR_materials_sheen");
            RegisterExtension<Material, MaterialClearCoat>("KHR_materials_clearcoat");
            RegisterExtension<Material, MaterialTransmission>("KHR_materials_transmission");
            RegisterExtension<Material, MaterialPBRSpecularGlossiness>("KHR_materials_pbrSpecularGlossiness");

            RegisterExtension<TextureInfo, TextureTransform>("KHR_texture_transform");

            RegisterExtension<Texture, TextureDDS>("MSFT_texture_dds");
            RegisterExtension<Texture, TextureWEBP>("EXT_texture_webp");
            RegisterExtension<Texture, TextureKTX2>("KHR_texture_basisu");
        }

        #endregion

        #region data

        private static readonly List<(string Name, Type ParentType, Type ExtType)> _Extensions = new List<(string, Type, Type)>();

        #endregion

        #region API

        public static IEnumerable<string> SupportedExtensions => _Extensions
            .Select(item => item.Name)
            .Concat(new[] { "KHR_mesh_quantization" });

        public static void RegisterExtension<TParent, TExtension>(string persistentName)
            where TParent : JsonSerializable
            where TExtension : JsonSerializable
        {
            _Extensions.Add( (persistentName, typeof(TParent), typeof(TExtension)) );
        }

        /// <summary>
        /// Creates an extension object based on the parent object and the extension code.
        /// </summary>
        /// <param name="parent">The parent object that will own the created object.</param>
        /// <param name="extensionName">The extension code, like "KHR_texture_transform".</param>
        /// <returns>The created object, or null.</returns>
        internal static JsonSerializable Create(JsonSerializable parent, string extensionName)
        {
            var ptype = parent.GetType();

            var (name, parentType, extType) = _Extensions.FirstOrDefault(item => item.Name == extensionName && item.ParentType.IsAssignableFrom(ptype));

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

        /// <summary>
        /// Given a parentType and an extensionType, it identifies the extension code.
        /// </summary>
        /// <param name="parentType">The type of the parent object.</param>
        /// <param name="extensionType">The type of the extension object.</param>
        /// <returns>An extension identifier code, like "KHR_texture_transform".</returns>
        internal static string Identify(Type parentType, Type extensionType)
        {
            foreach (var (name, baseType, extType) in _Extensions)
            {
                if (baseType.IsAssignableFrom(parentType) && extType == extensionType) return name;
            }

            return null;
        }

        #endregion
    }

    partial class ModelRoot
    {
        #region properties

        public bool MeshQuantizationAllowed => this._extensionsRequired.Contains("KHR_mesh_quantization");

        #endregion

        #region API

        internal void UpdateExtensionsSupport()
        {
            var used = RetrieveUsedExtensions();

            // update the used list
            this._extensionsUsed.Clear();
            this._extensionsUsed.AddRange(used);

            _SetExtensionUsage("KHR_mesh_quantization", this._extensionsUsed.Contains("KHR_mesh_quantization"), true);
        }

        private void _SetExtensionUsage(string extension, bool used, bool required)
        {
            if (!used)
            {
                this._extensionsUsed.Remove(extension);
                this._extensionsRequired.Remove(extension);
                return;
            }

            if (!this._extensionsUsed.Contains(extension)) this._extensionsUsed.Add(extension);
            if (required && !this._extensionsRequired.Contains(extension)) this._extensionsRequired.Add(extension);
        }

        internal IEnumerable<string> RetrieveUsedExtensions()
        {
            // retrieve ALL the property based objects of the whole model.
            var allObjects = new[] { this }
                .Concat(GetLogicalChildrenFlattened())
                .ToList();

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

            // search for special cases

            var isQuantized = MeshPrimitive.CheckAttributesQuantizationRequired(this);
            if (isQuantized) used.Add("KHR_mesh_quantization");

            return used;
        }

        internal void UsingExtension(Type parentType, Type extensionType)
        {
            var id = ExtensionsFactory.Identify(parentType, extensionType);
            if (string.IsNullOrWhiteSpace(id)) return;

            if (this._extensionsUsed.Contains(id)) return;

            this._extensionsUsed.Add(id);
        }

        internal void _ValidateExtensions(Validation.ValidationContext validate)
        {
            foreach (var iex in this.IncompatibleExtensions)
            {
                validate._LinkThrow("Extensions", iex);
            }

            foreach (var ext in RetrieveUsedExtensions())
            {
                if (!this._extensionsUsed.Contains(ext)) validate._LinkThrow("Extensions", ext);
            }
        }

        #endregion
    }
}
