using System;
using System.Collections.Generic;
using System.Linq;

namespace SharpGLTF.Schema2
{
    using Collections;

    [System.Diagnostics.DebuggerDisplay("Model Root")]
    public sealed partial class ModelRoot
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
            _samplers = new ChildrenCollection<Sampler, ModelRoot>(this);
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
            var dictionary = this.WriteToDictionary("deepclone");
            return ModelRoot.ReadFromDictionary(dictionary, "deepclone.gltf");
        }

        #endregion

        #region properties

        public Asset Asset => _asset;

        public IEnumerable<String> ExtensionsUsed               => _extensionsUsed;

        public IEnumerable<String> ExtensionsRequired           => _extensionsRequired;

        public IEnumerable<String> IncompatibleExtensions       => _extensionsRequired.Except(ExtensionsFactory.SupportedExtensions).ToList();

        #endregion

        #region Logical resouces

        public IReadOnlyList<Material>      LogicalMaterials    => _materials;
        public IReadOnlyList<Texture>       LogicalTextures     => _textures;
        public IReadOnlyList<Sampler>       LogicalSamplers     => _samplers;
        public IReadOnlyList<Image>         LogicalImages       => _images;

        public IReadOnlyList<Buffer>        LogicalBuffers      => _buffers;
        public IReadOnlyList<BufferView>    LogicalBufferViews  => _bufferViews;
        public IReadOnlyList<Accessor>      LogicalAccessors    => _accessors;

        public IReadOnlyList<Mesh>          LogicalMeshes       => _meshes;
        public IReadOnlyList<Skin>          LogicalSkins        => _skins;
        public IReadOnlyList<Camera>        LogicalCameras      => _cameras;

        public IReadOnlyList<Node>          LogicalNodes        => _nodes;
        public IReadOnlyList<Scene>         LogicalScenes       => _scenes;
        public IReadOnlyList<Animation>     LogicalAnimations   => _animations;

        /// <inheritdoc />
        protected override IEnumerable<glTFProperty> GetLogicalChildren()
        {
            var containers = base.GetLogicalChildren();

            containers = containers.Concat(this.Asset);
            containers = containers.Concat(this.LogicalAccessors);
            containers = containers.Concat(this.LogicalAnimations);
            containers = containers.Concat(this.LogicalBuffers);
            containers = containers.Concat(this.LogicalBufferViews);
            containers = containers.Concat(this.LogicalCameras);
            containers = containers.Concat(this.LogicalImages);
            containers = containers.Concat(this.LogicalMaterials);
            containers = containers.Concat(this.LogicalMeshes);
            containers = containers.Concat(this.LogicalNodes);
            containers = containers.Concat(this.LogicalSamplers);
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

                Guard.MustShareLogicalParent(this, value, nameof(value));

                _scene = value.LogicalIndex;
            }
        }

        #endregion

        #region validation

        public override IEnumerable<Exception> Validate()
        {
            var exx = new List<Exception>();

            // 1st check version number

            if (Asset == null) exx.Add(new IO.ModelException(this, "missing Asset object, can't check glTF version")); // fix: create a default Asset
            else exx.AddRange(Asset.Validate());

            if (exx.Count > 0) return exx;

            // 2nd check incompatible extensions

            foreach (var iex in this.IncompatibleExtensions)
            {
                exx.Add(new IO.UnsupportedExtensionException(this, iex)); // fix: attempt to remove given extension
            }

            if (exx.Count > 0) return exx;

            // 3rd check base class

            exx.AddRange(base.Validate());

            // 4th check contents
            foreach (var s in _scenes) exx.AddRange(s.Validate());
            foreach (var n in _nodes) exx.AddRange(n.Validate());
            foreach (var a in _accessors) exx.AddRange(a.Validate());
            foreach (var m in _meshes) exx.AddRange(m.Validate());
            foreach (var s in _skins) exx.AddRange(s.Validate());

            return exx;
        }

        #endregion
    }
}
