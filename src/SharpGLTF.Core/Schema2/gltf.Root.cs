using System;
using System.Collections.Generic;
using System.Linq;

using SharpGLTF.Collections;

namespace SharpGLTF.Schema2
{
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
            var dict = new Dictionary<string, ArraySegment<Byte>>();
            var settings = WriteContext.ForDeepClone(dict);

            System.Diagnostics.Debug.Assert(settings._NoCloneWatchdog, "invalid clone settings");

            this.Write(settings, "deepclone");

            var context = ReadContext.CreateFromDictionary(dict);
            context.Validation = Validation.ValidationMode.Strict;
            return context._ReadFromDictionary("deepclone.gltf");
        }

        #endregion

        #region properties

        public Asset Asset => _asset;

        public IEnumerable<String> ExtensionsUsed               => _extensionsUsed;

        public IEnumerable<String> ExtensionsRequired           => _extensionsRequired;

        public IEnumerable<String> IncompatibleExtensions       => _extensionsRequired.Except(ExtensionsFactory.SupportedExtensions).ToList();

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

        protected override void OnValidateReferences(Validation.ValidationContext result)
        {
            if (Asset == null) result.AddSchemaError(nameof(Asset), "is missing");

            result.CheckArrayIndexAccess(nameof(DefaultScene), _scene, this.LogicalScenes);

            foreach (var b in _buffers) b.ValidateReferences(result);
            foreach (var v in _bufferViews) v.ValidateReferences(result);
            foreach (var a in _accessors) a.ValidateReferences(result);

            foreach (var i in _images) i.ValidateReferences(result);
            foreach (var s in _samplers) s.ValidateReferences(result);
            foreach (var t in _textures) t.ValidateReferences(result);
            foreach (var m in _materials) m.ValidateReferences(result);

            foreach (var m in _meshes) m.ValidateReferences(result);
            foreach (var s in _skins) s.ValidateReferences(result);
            foreach (var c in _cameras) c.ValidateReferences(result);

            foreach (var n in _nodes) n.ValidateReferences(result);
            foreach (var s in _scenes) s.ValidateReferences(result);
            foreach (var a in _animations) a.ValidateReferences(result);

            base.OnValidateReferences(result);
        }

        protected override void OnValidate(Validation.ValidationContext result)
        {
            // 1st check version number

            Asset.Validate(result);

            if (result.Result.HasErrors) return;

            // 2nd check incompatible extensions

            foreach (var iex in this.IncompatibleExtensions)
            {
                result.UnsupportedExtensionError(iex);
            }

            if (result.Result.HasErrors) return;

            // 3rd check base class

            base.OnValidate(result);

            // 4th check contents

            foreach (var b in _buffers) b.Validate(result);
            foreach (var v in _bufferViews) v.Validate(result);
            foreach (var a in _accessors) a.Validate(result);

            foreach (var i in _images) i.Validate(result);
            foreach (var s in _samplers) s.Validate(result);
            foreach (var t in _textures) t.Validate(result);
            foreach (var m in _materials) m.Validate(result);

            foreach (var m in _meshes) m.Validate(result);
            foreach (var s in _skins) s.Validate(result);
            foreach (var c in _cameras) c.Validate(result);

            foreach (var n in _nodes) n.Validate(result);
            foreach (var s in _scenes) s.Validate(result);
            foreach (var a in _animations) a.Validate(result);
        }

        #endregion
    }
}
