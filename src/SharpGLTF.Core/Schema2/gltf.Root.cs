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
            root._asset = Asset.CreateDefault(string.Empty);

            return root;
        }

        internal ModelRoot()
        {
            _extensionsUsed = new List<string>();
            _extensionsRequired = new List<string>();

            _accessors = new ChildrenCollection<Accessor, ModelRoot>(this);
            _animations = new ChildrenCollection<Animation, ModelRoot>(this);
            _buffers = new ChildrenCollection<Buffer, ModelRoot>(this);
            _bufferViews = new ChildrenCollection<BufferView, ModelRoot>(this);
            _cameras = new ChildrenCollection<Camera, ModelRoot>(this);
            _images = new ChildrenCollection<Image, ModelRoot>(this);
            _materials = new ChildrenCollection<Material, ModelRoot>(this);
            _meshes = new ChildrenCollection<Mesh, ModelRoot>(this);
            _nodes = new ChildrenCollection<Node, ModelRoot>(this);
            _samplers = new ChildrenCollection<TextureSampler, ModelRoot>(this);
            _scenes = new ChildrenCollection<Scene, ModelRoot>(this);
            _skins = new ChildrenCollection<Skin, ModelRoot>(this);
            _textures = new ChildrenCollection<Texture, ModelRoot>(this);
        }

        /// <summary>
        /// Creates a complete clone of this <see cref="ModelRoot"/> instance.
        /// </summary>
        /// <returns>A new <see cref="ModelRoot"/> instance.</returns>
        /// <remarks>
        /// Deep cloning is performed as a brute force operation; by serializing
        /// the whole model to GLTF into memory, and then deserializing it back.
        /// </remarks>
        public ModelRoot DeepClone()
        {
            // prepare the in-memory temporary storage
            var dict = new Dictionary<string, ArraySegment<Byte>>();
            var wcontext = IO.WriteContext
                .CreateFromDictionary(dict)
                .WithDeepCloneSettings();

            System.Diagnostics.Debug.Assert(wcontext._NoCloneWatchdog, "invalid clone settings");

            // write the model to the temporary storage

            wcontext.WriteTextSchema2("deepclone", this);

            // restore the model from the temporary storage

            var rcontext = IO.ReadContext.CreateFromDictionary(dict);
            rcontext.Validation = Validation.ValidationMode.Skip;
            var cloned = rcontext._ReadFromDictionary("deepclone.gltf");

            // Restore MemoryImage source URIs (they're not cloned as part of the serialization)
            foreach (var srcImg in this.LogicalImages)
            {
                var dstImg = cloned.LogicalImages[srcImg.LogicalIndex];
                var img = dstImg.Content;
                dstImg.Content = new Memory.MemoryImage(img._GetBuffer(), srcImg.Content.SourcePath);
            }

            return cloned;
        }

        #endregion

        #region properties

        public Asset Asset => _asset;

        public IEnumerable<String> ExtensionsUsed               => _extensionsUsed;

        public IEnumerable<String> ExtensionsRequired           => _extensionsRequired;

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

        protected override IEnumerable<ExtraProperties> GetLogicalChildren()
        {
            var containers = base.GetLogicalChildren();

            containers = containers.ConcatItems(this.Asset);
            containers = containers.Concat(this.LogicalAccessors);
            containers = containers.Concat(this.LogicalAnimations);
            containers = containers.Concat(this.LogicalBuffers);
            containers = containers.Concat(this.LogicalBufferViews);
            containers = containers.Concat(this.LogicalCameras);
            containers = containers.Concat(this.LogicalImages);
            containers = containers.Concat(this.LogicalMaterials);
            containers = containers.Concat(this.LogicalMeshes);
            containers = containers.Concat(this.LogicalNodes);
            containers = containers.Concat(this.LogicalTextureSamplers);
            containers = containers.Concat(this.LogicalScenes);
            containers = containers.Concat(this.LogicalSkins);
            containers = containers.Concat(this.LogicalTextures);

            return containers;
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

            // check incompatible extensions

            foreach (var iex in this.IncompatibleExtensions)
            {
                validate._LinkThrow("Extensions", iex);
            }

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
