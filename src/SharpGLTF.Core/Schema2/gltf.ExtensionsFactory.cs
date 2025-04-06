using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;

using SharpGLTF.IO;

#if NET6_0_OR_GREATER
using DYNAMICMEMBERS = System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembersAttribute;
using DYNAMICTYPES = System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes;
#endif

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
        #if NET6_0_OR_GREATER
        private const DYNAMICTYPES DYNAMICCONSTRUCTORS = DYNAMICTYPES.NonPublicConstructors | DYNAMICTYPES.PublicConstructors;
        #endif

        // extensions design inconsistencies:
        // https://github.com/KhronosGroup/glTF/issues/1491

        #region supported extensions must be registered here

        static ExtensionsFactory()
        {
            RegisterExtension<ModelRoot, _ModelPunctualLights>("KHR_lights_punctual", p=> new _ModelPunctualLights(p));
            RegisterExtension<Node, _NodePunctualLight>("KHR_lights_punctual", p=> new _NodePunctualLight(p));

            RegisterExtension<Node, MeshGpuInstancing>("EXT_mesh_gpu_instancing", p=> new MeshGpuInstancing(p));

            RegisterExtension<Material, MaterialUnlit>("KHR_materials_unlit", p => new MaterialUnlit(p));
            RegisterExtension<Material, MaterialSheen>("KHR_materials_sheen", p => new MaterialSheen(p));
            RegisterExtension<Material, MaterialIOR>("KHR_materials_ior", p => new MaterialIOR(p));
            RegisterExtension<Material, MaterialDispersion>("KHR_materials_dispersion", p => new MaterialDispersion(p));
            RegisterExtension<Material, MaterialSpecular>("KHR_materials_specular", p => new MaterialSpecular(p));
            RegisterExtension<Material, MaterialClearCoat>("KHR_materials_clearcoat", p => new MaterialClearCoat(p));
            RegisterExtension<Material, MaterialTransmission>("KHR_materials_transmission", p => new MaterialTransmission(p));
            RegisterExtension<Material, MaterialDiffuseTransmission>("KHR_materials_diffuse_transmission", p => new MaterialDiffuseTransmission(p));
            RegisterExtension<Material, MaterialVolume>("KHR_materials_volume", p => new MaterialVolume(p));
            RegisterExtension<Material, MaterialEmissiveStrength>("KHR_materials_emissive_strength", p => new MaterialEmissiveStrength(p));
            RegisterExtension<Material, MaterialPBRSpecularGlossiness>("KHR_materials_pbrSpecularGlossiness", p => new MaterialPBRSpecularGlossiness(p));
            RegisterExtension<Material, MaterialIridescence>("KHR_materials_iridescence", p => new MaterialIridescence(p));
            RegisterExtension<Material, MaterialAnisotropy>("KHR_materials_anisotropy", p => new MaterialAnisotropy(p));

            RegisterExtension<TextureInfo, TextureTransform>("KHR_texture_transform", p => new TextureTransform(p));

            RegisterExtension<Texture, TextureDDS>("MSFT_texture_dds", p => new TextureDDS(p));
            RegisterExtension<Texture, TextureWEBP>("EXT_texture_webp", p => new TextureWEBP(p));
            RegisterExtension<Texture, TextureKTX2>("KHR_texture_basisu", p => new TextureKTX2(p));

            RegisterExtension<ModelRoot, XmpPackets>("KHR_xmp_json_ld", p => new XmpPackets(p));
            RegisterExtension<ExtraProperties, XmpPacketReference>("KHR_xmp_json_ld", p => new XmpPacketReference(p));

            RegisterExtension<AnimationChannelTarget, AnimationPointer>("KHR_animation_pointer", p => new AnimationPointer(p));
        }

        #endregion

        #region data

        private static readonly List<ExtensionEntry> _Extensions = new List<ExtensionEntry>();

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
        [Obsolete("Use RegisterExtension(name, factory) instead.")]
        public static void RegisterExtension
            <TParent,
                #if NET6_0_OR_GREATER
                [DYNAMICMEMBERS(DYNAMICCONSTRUCTORS)]
                #endif
            TExtension>
            (string persistentName)
            where TParent : JsonSerializable
            where TExtension : JsonSerializable
        {
            Guard.NotNullOrEmpty(persistentName, nameof(persistentName));
            Guard.MustBeNull(Identify(typeof(TParent), typeof(TExtension)), $"{nameof(TExtension)} already registered for {nameof(TParent)}");

            // TODO: check that persistentName has a valid extension name.

            var ext = ExtensionEntry.Create<TParent,TExtension>(persistentName);

            _Extensions.Add(ext);
        }

        /// <summary>
        /// Registers a new extensions to be used globally.
        /// </summary>
        /// <typeparam name="TParent">The parent type to which this extension is attached.</typeparam>
        /// <typeparam name="TExtension">The extension type.</typeparam>
        /// <param name="persistentName">The extension name.</param>
        /// <param name="factory">callback used to create instances of type <typeparamref name="TExtension"/>.</param>
        /// <remarks>
        /// The <paramref name="persistentName"/> is the value used for serialization<br/>
        /// and it must meet <see href="https://github.com/KhronosGroup/glTF/blob/master/extensions/Prefixes.md">extension naming constraints</see>.
        /// </remarks>
        public static void RegisterExtension<TParent,TExtension>(string persistentName, Func<TParent, JsonSerializable> factory)
            where TParent : JsonSerializable
            where TExtension : JsonSerializable
        {
            Guard.NotNullOrEmpty(persistentName, nameof(persistentName));
            Guard.MustBeNull(Identify(typeof(TParent), typeof(TExtension)), $"{nameof(TExtension)} already registered for {nameof(TParent)}");

            // TODO: check that persistentName has a valid extension name.

            var ext = new ExtensionEntry(persistentName, typeof(TParent), typeof(TExtension), p => factory.Invoke((TParent)p));

            _Extensions.Add(ext);
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

            var extension = _Extensions.FirstOrDefault(item => item.IsMatch(ptype, extensionName));

            if (extension.Name == null) return null;

            var instance = extension.Factory.Invoke(parent);

            return instance ?? throw new InvalidOperationException($"Could not create an instance of {extensionName}");
        }

        /// <summary>
        /// Given a parentType and an extensionType, it identifies the extension code name.
        /// </summary>
        /// <param name="parentType">The type of the parent object.</param>
        /// <param name="extensionType">The type of the extension object.</param>
        /// <returns>An extension identifier code, like "KHR_texture_transform".</returns>
        /// <remarks>
        /// Extensions must be registered in advanced using <see cref="RegisterExtension{TParent, TExtension}(string, Func{TParent, JsonSerializable})"/>.
        /// </remarks>
        internal static string Identify(Type parentType, Type extensionType)
        {
            foreach (var ext in _Extensions)
            {
                if (ext.IsMatch(parentType,extensionType)) return ext.Name;
            }

            return null;
        }

        [System.Diagnostics.DebuggerDisplay("{Name} {ParentType} {ExtType}")]
        internal readonly struct ExtensionEntry
        {
            #region lifecycle
            public static ExtensionEntry Create
                <
                TParent,
                #if NET6_0_OR_GREATER
                [DYNAMICMEMBERS(DYNAMICCONSTRUCTORS)]
                #endif
                TExtension
                >(string persistentName)

            where TParent : JsonSerializable
            where TExtension : JsonSerializable
            {
                var extType = typeof(TExtension);

                JsonSerializable factory(JsonSerializable parent)
                {
                    return Activator.CreateInstance
                    (
                        extType,
                        System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance,
                        null,
                        new Object[] { parent },
                        null
                    ) as JsonSerializable;
                }

                return new ExtensionEntry(persistentName, typeof(TParent), extType, factory);
            }

            public ExtensionEntry(string n, Type p, Type e, Func<JsonSerializable, JsonSerializable> f)
            {
                Name= n;
                ParentType = p;
                ExtType = e;
                Factory = f;
            }

            #endregion

            #region data

            public readonly string Name;
            public readonly Type ParentType;
            public readonly Type ExtType;
            public readonly Func<JsonSerializable, JsonSerializable> Factory;

            #endregion

            #region API

            public readonly bool IsMatch(Type parentType, string extensionName)
            {
                return this.ParentType.IsAssignableFrom(parentType) && this.Name == extensionName;
            }

            public readonly bool IsMatch(Type parentType, Type extensionType)
            {
                return this.ParentType.IsAssignableFrom(parentType) && this.ExtType == extensionType;
            }

            #endregion
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
