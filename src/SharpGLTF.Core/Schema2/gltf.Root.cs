using System;
using System.Collections.Generic;
using System.Linq;

using SharpGLTF.Collections;

namespace SharpGLTF.Schema2
{
    [System.Diagnostics.DebuggerDisplay("Model Root")]
    public sealed partial class ModelRoot : IConvertibleToGltf2
    {
        #region lifecycle

        /// <summary>
        /// Creates a new <see cref = "ModelRoot" /> instance.
        /// </summary>
        /// <returns>A <see cref="ModelRoot"/> instance.</returns>
        public static ModelRoot CreateModel()
        {
            var root = new ModelRoot();
            root.Asset = Asset.CreateDefault(string.Empty);

            return root;
        }

        internal ModelRoot()
        {
            _extensionsUsed = new List<string>();
            _extensionsRequired = new List<string>();

            _accessors = new ChildrenList<Accessor, ModelRoot>(this);
            _animations = new ChildrenList<Animation, ModelRoot>(this);
            _buffers = new ChildrenList<Buffer, ModelRoot>(this);
            _bufferViews = new ChildrenList<BufferView, ModelRoot>(this);
            _cameras = new ChildrenList<Camera, ModelRoot>(this);
            _images = new ChildrenList<Image, ModelRoot>(this);
            _materials = new ChildrenList<Material, ModelRoot>(this);
            _meshes = new ChildrenList<Mesh, ModelRoot>(this);
            _nodes = new ChildrenList<Node, ModelRoot>(this);
            _samplers = new ChildrenList<TextureSampler, ModelRoot>(this);
            _scenes = new ChildrenList<Scene, ModelRoot>(this);
            _skins = new ChildrenList<Skin, ModelRoot>(this);
            _textures = new ChildrenList<Texture, ModelRoot>(this);
        }

        /// <summary>
        /// Creates a complete clone of this <see cref="ModelRoot"/> instance.
        /// </summary>
        /// <returns>A new <see cref="ModelRoot"/> instance.</returns>
        /// <remarks>
        /// Deep cloning is performed as a brute force operation; by serializing
        /// the whole model to GLTF into memory, and then deserializing it back to DOM.
        /// </remarks>
        public ModelRoot DeepClone()
        {
            // prepare the in-memory temporary storage
            var dict = new Dictionary<string, ArraySegment<Byte>>();
            var wcontext = WriteContext
                .CreateFromDictionary(dict)
                .WithDeepCloneSettings();

            System.Diagnostics.Debug.Assert(wcontext._NoCloneWatchdog, "invalid clone settings");

            // write the model to the temporary storage

            wcontext.WriteTextSchema2("$$$deepclone$$$", this);

            // restore the model from the temporary storage

            var rcontext = ReadContext.CreateFromDictionary(dict, wcontext._UpdateSupportedExtensions);
            rcontext.Validation = Validation.ValidationMode.Skip;
            var cloned = rcontext.ReadSchema2("$$$deepclone$$$.gltf");

            // Restore MemoryImage's source URIs hints
            // and Image's AlternateWriteFileName
            // (they're not cloned as part of the serialization)
            foreach (var srcImg in this.LogicalImages)
            {
                var dstImg = cloned.LogicalImages[srcImg.LogicalIndex];
                var img = dstImg.Content;
                dstImg.Content = new Memory.MemoryImage(img, srcImg.Content.SourcePath);
                dstImg.AlternateWriteFileName = srcImg.AlternateWriteFileName;
            }

            return cloned;
        }

        #endregion

        #region properties        

        /// <summary>
        /// List of extensions used by the current model.
        /// </summary>
        public IEnumerable<String> ExtensionsUsed               => _extensionsUsed;

        /// <summary>
        /// List of extensions required by the current model.
        /// </summary>
        public IEnumerable<String> ExtensionsRequired           => _extensionsRequired;

        /// <summary>
        /// List of extensions not supported by the library.
        /// </summary>
        /// <remarks>
        /// Usually, this collection should be empty. Otherwise it signals this model is unusable.
        /// </remarks>
        public IEnumerable<String> IncompatibleExtensions       => _extensionsRequired.Except(ExtensionsFactory.SupportedExtensions).ToList();

        ModelRoot IConvertibleToGltf2.ToGltf2() { return this; }

        #endregion

        #region Logical resouces

        public IReadOnlyList<Material>          LogicalMaterials        => _materials;
        public IReadOnlyList<Texture>           LogicalTextures         => _textures;
        public IReadOnlyList<TextureSampler>    LogicalTextureSamplers  => _samplers;
        public IReadOnlyList<Image>             LogicalImages           => _images;

        public IReadOnlyList<Buffer>            LogicalBuffers          => _buffers;
        public IReadOnlyList<BufferView>        LogicalBufferViews      => _bufferViews;
        public IReadOnlyList<Accessor>          LogicalAccessors        => _accessors;

        public IReadOnlyList<Mesh>              LogicalMeshes           => _meshes;
        public IReadOnlyList<Skin>              LogicalSkins            => _skins;
        public IReadOnlyList<Camera>            LogicalCameras          => _cameras;

        public IReadOnlyList<Node>              LogicalNodes            => _nodes;
        public IReadOnlyList<Scene>             LogicalScenes           => _scenes;
        public IReadOnlyList<Animation>         LogicalAnimations       => _animations;
        
        internal IEnumerable<ExtraProperties> GetLogicalChildrenFlattened()
        {
            return GetLogicalChildren().SelectMany(item => Flatten(item));
        }

        #endregion

        #region Visual Tree

        public Scene DefaultScene
        {
            get => _scenes.Count == 0 ? null : _scenes[_scene.AsValue(0)];
            set
            {
                if (value == null)
                {
                    _scene = null;
                    return;
                }

                Guard.MustShareLogicalParent(this, "this", value, nameof(value));

                _scene = value.LogicalIndex;
            }
        }

        #endregion

        #region validation

        protected override void OnValidateReferences(Validation.ValidationContext validate)
        {
            validate
                .NotNull(nameof(Asset), this.Asset)
                .IsNullOrIndex(nameof(DefaultScene), _scene, this.LogicalScenes);

            Asset.ValidateReferences(validate);

            base.OnValidateReferences(validate);

            Node._ValidateParentHierarchy(this.LogicalNodes, validate);
        }

        protected override void OnValidateContent(Validation.ValidationContext validate)
        {
            Asset.ValidateContent(validate);

            base.OnValidateContent(validate);
        }

        #endregion
    }
}
