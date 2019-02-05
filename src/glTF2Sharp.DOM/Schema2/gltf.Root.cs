using System;
using System.Collections.Generic;
using System.Linq;

namespace glTF2Sharp.Schema2
{    
    using Collections;    

    [System.Diagnostics.DebuggerDisplay("Model Root")]
    public partial class ModelRoot
    {
        #region lifecycle

        public static ModelRoot CreateNew()
        {
            var root = new ModelRoot();
            root._asset = Asset.CreateDefault("");

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

        #endregion

        #region internal API        

        internal Mesh _AddLogicalMesh()
        {
            var mesh = new Mesh();

            _meshes.Add(mesh);

            return mesh;
        }

        internal Skin _AddLogicalSkin()
        {
            var skin = new Skin();

            _skins.Add(skin);

            return skin;
        }        

        #endregion

        #region properties

        public Asset Asset => _asset;

        public IEnumerable<String> ExtensionsRequired           => this._extensionsRequired;

        public IEnumerable<String> IncompatibleExtensions       => this._extensionsRequired.Except(ExtensionsFactory.SupportedExtensions);
        
        internal IReadOnlyList<Material>    _LogicalMaterials   => _materials;
        internal IReadOnlyList<Texture>     _LogicalTextures    => _textures;
        internal IReadOnlyList<Sampler>     _LogicalSamplers    => _samplers;
        internal IReadOnlyList<Image>       _LogicalImages      => _images;

        public IReadOnlyList<Mesh>          LogicalMeshes      => _meshes;
        internal IReadOnlyList<Skin>        _LogicalSkins       => _skins;
        internal IReadOnlyList<Camera>      _LogicalCameras     => _cameras;

        internal IReadOnlyList<Buffer>      _LogicalBuffers     => _buffers;
        public IReadOnlyList<BufferView>    LogicalBufferViews => _bufferViews;
        internal IReadOnlyList<Accessor>    _LogicalAccessors => _accessors;

        internal IReadOnlyList<Node>        _LogicalNodes       => _nodes;

        public IReadOnlyList<Scene>       LogicalScenes   => _scenes;
        public IReadOnlyList<Animation>   Animations      => _animations;

        public Scene DefaultScene =>        _scenes.Count == 0 ? null : _scenes[_scene ?? 0];

        #endregion                

        #region validation

        public override IEnumerable<Exception> Validate()
        {
            var exx = new List<Exception>();

            // 1st check version number

            if (Asset == null) exx.Add(new ModelException(this, "missing Asset object, can't check glTF version")); // fix: create a default Asset                
            else exx.AddRange(Asset.Validate());

            if (exx.Count > 0) return exx;

            // 2nd check incompatible extensions            

            foreach (var iex in this.IncompatibleExtensions)
            {
                exx.Add(new ExtensionException(this, $"Incompatible Extension found: {iex}")); // fix: attempt to remove given extension
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
