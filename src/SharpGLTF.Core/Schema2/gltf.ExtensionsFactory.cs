using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;

using SharpGLTF.IO;

namespace SharpGLTF.Schema2
{
    /// <summary>
    /// Global extensions manager.
    /// </summary>
    /// <remarks>
    /// Extensions must be registered at the beginning of the application<br/>
    /// Before using the APIs.
    /// </remarks>
    public static class ExtensionsFactory
    {
        // extensions design inconsistencies:
        // https://github.com/KhronosGroup/glTF/issues/1491

        #region supported extensions must be registered here

        static ExtensionsFactory()
        {
            RegisterExtension<ModelRoot, _ModelPunctualLights>("KHR_lights_punctual");
            RegisterExtension<Node, _NodePunctualLight>("KHR_lights_punctual");

            RegisterExtension<Node, MeshGpuInstancing>("EXT_mesh_gpu_instancing");

            RegisterExtension<Material, MaterialUnlit>("KHR_materials_unlit");
            RegisterExtension<Material, MaterialSheen>("KHR_materials_sheen");
            RegisterExtension<Material, MaterialIOR>("KHR_materials_ior");
            RegisterExtension<Material, MaterialSpecular>("KHR_materials_specular");
            RegisterExtension<Material, MaterialClearCoat>("KHR_materials_clearcoat");
            RegisterExtension<Material, MaterialTransmission>("KHR_materials_transmission");
            RegisterExtension<Material, MaterialVolume>("KHR_materials_volume");
            RegisterExtension<Material, MaterialEmissiveStrength>("KHR_materials_emissive_strength");
            RegisterExtension<Material, MaterialPBRSpecularGlossiness>("KHR_materials_pbrSpecularGlossiness");
            RegisterExtension<Material, MaterialIridescence>("KHR_materials_iridescence");

            RegisterExtension<TextureInfo, TextureTransform>("KHR_texture_transform");

            RegisterExtension<Texture, TextureDDS>("MSFT_texture_dds");
            RegisterExtension<Texture, TextureWEBP>("EXT_texture_webp");
            RegisterExtension<Texture, TextureKTX2>("KHR_texture_basisu");

            RegisterExtension<ModelRoot, XmpPackets>("KHR_xmp_json_ld");
            RegisterExtension<ExtraProperties, XmpPacketReference>("KHR_xmp_json_ld");                        
        }

        #endregion

        #region data

        private static readonly List<(string Name, Type ParentType, Type ExtType)> _Extensions = new List<(string, Type, Type)>();

        #endregion

        #region API

        public static IEnumerable<string> SupportedExtensions => _Extensions
            .Select(item => item.Name)
            .Concat(new[] { "KHR_mesh_quantization" }); // special case because it's a "typeless" extension.

        /// <summary>
        /// Registers a new extensions to be used globally.
        /// </summary>
        /// <typeparam name="TParent">The parent type to which this extension is attached.</typeparam>
        /// <typeparam name="TExtension">The extension type.</typeparam>
        /// <param name="persistentName">The extension name.</param>
        /// <remarks>
        /// The <paramref name="persistentName"/> is the value used for serialization<br/>
        /// and it must meet <see href="https://github.com/KhronosGroup/glTF/blob/master/extensions/Prefixes.md">extension naming constraints</see>.
        /// </remarks>
        public static void RegisterExtension<TParent, TExtension>(string persistentName)
            where TParent : JsonSerializable
            where TExtension : JsonSerializable
        {
            Guard.NotNullOrEmpty(persistentName, nameof(persistentName));
            Guard.MustBeNull(Identify(typeof(TParent), typeof(TExtension)), $"{nameof(TExtension)} already registered for {nameof(TParent)}");

            // TODO: check that persistentName has a valid extension name.

            _Extensions.Add( (persistentName, typeof(TParent), typeof(TExtension)) );
        }

        #endregion

        #region internals

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

            Guard.HasDynamicallyAccessedMembers(extType, true, false, false, false, nameof(extensionName));

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
        /// Given a parentType and an extensionType, it identifies the extension code name.
        /// </summary>
        /// <param name="parentType">The type of the parent object.</param>
        /// <param name="extensionType">The type of the extension object.</param>
        /// <returns>An extension identifier code, like "KHR_texture_transform".</returns>
        /// <remarks>
        /// Extensions must be registered in advanced using <see cref="RegisterExtension{TParent, TExtension}(string)"/>.
        /// </remarks>
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
            var used = GatherUsedExtensions();

            // update the used list
            this._extensionsUsed.Clear();
            this._extensionsUsed.AddRange(used);

            _SetExtensionUsage("KHR_mesh_quantization", this._extensionsUsed.Contains("KHR_mesh_quantization"), true);
        }

        internal IEnumerable<string> GatherUsedExtensions()
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

        internal void _ValidateExtensions(Validation.ValidationContext validate)
        {
            foreach (var iex in this.IncompatibleExtensions)
            {
                validate._LinkThrow("Extensions", iex);
            }

            foreach (var ext in GatherUsedExtensions())
            {
                if (!this._extensionsUsed.Contains(ext)) validate._LinkThrow("Extensions", ext);
            }
        }

        #endregion
    }
}
