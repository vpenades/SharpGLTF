using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;

namespace glTF2Sharp.Schema2
{
    using IO;
    using Collections;

    using ROOT = ModelRoot;    

    public abstract class glTFProperty : JsonSerializable
    {
        #region data

        // should be a hashset where the comparer is the Type
        private readonly List<JsonSerializable> _extensions = new List<JsonSerializable>();

        private Object _extras;

        #endregion

        #region API        

        public T GetExtension<T>() where T : JsonSerializable { return _extensions.OfType<T>().FirstOrDefault(); }

        public void SetExtension<T>(T value) where T : JsonSerializable
        {
            var idx = _extensions.IndexOf(item => item.GetType() == typeof(T));

            if (idx < 0) { _extensions.Add(value);  return; }
            
            if (value == null) _extensions.RemoveAt(idx);
            else _extensions[idx] = value;
        }

        #endregion

        #region serialization API

        protected override void SerializeProperties(JsonWriter writer)
        {
            SerializeProperty(writer, "extensions", _extensions);
            // SerializeProperty(writer, "extras", _extras);
        }

        protected override void DeserializeProperty(JsonReader reader, string property)
        {
            switch (property)
            {
                case "extras": reader.Skip(); break;
                case "extensions": _DeserializeExtensions(reader, _extensions); break;
                // case "extras": _extras = DeserializeValue<Object>(reader); break;
                
                default: reader.Skip(); break;                
            }
        }
        

        private static void _DeserializeExtensions(JsonReader reader, IList<JsonSerializable> extensions)
        {
            while (true)
            {
                reader.Read();

                if (reader.TokenType == JsonToken.EndObject) break;
                if (reader.TokenType == JsonToken.EndArray) break;

                if (reader.TokenType == JsonToken.StartArray)
                {
                    while(true)
                    {
                        if (reader.TokenType == JsonToken.EndArray) break;

                        _DeserializeExtensions(reader, extensions);
                    }

                    break;
                }

                if (reader.TokenType == JsonToken.StartObject) continue;                

                System.Diagnostics.Debug.Assert(reader.TokenType == JsonToken.PropertyName);
                var key = reader.Value as String;

                var val = ExtensionsFactory.Create(key);

                if (val == null) reader.Skip();
                else
                {
                    val.DeserializeObject(reader);
                    extensions.Add(val);
                }
            }
        }        

        #endregion
    }

    public abstract partial class LogicalChildOfRoot : IChildOf<ROOT>
    {
        public ROOT LogicalParent { get; private set; }

        void IChildOf<ROOT>._SetLogicalParent(ROOT parent) { LogicalParent = parent; }

        public String Name { get => _name; internal set => _name = value; }

        #region validation

        protected bool ShareLogicalParent(params LogicalChildOfRoot[] items)
        {
            return items.All(item => Object.ReferenceEquals(this.LogicalParent, item.LogicalParent));
        }

        public override IEnumerable<Exception> Validate()
        {
            foreach (var ex in base.Validate()) yield return ex;

            if (_name == null) yield break;

            // todo, verify the name does not have invalid characters
        }

        #endregion
    }

    [System.Diagnostics.DebuggerDisplay("{Version} {MinVersion} {Generator} {Copyright}")]
    public partial class Asset
    {
        #region lifecycle

        internal static Asset CreateDefault(string copyright)
        {
            return new Asset()
            {
                _generator = "glTF2Sharp",
                _copyright = copyright,
                _version = MAXVERSION.ToString()                
            };
        }

        #endregion

        #region properties

        private static readonly Version ZEROVERSION = new Version(0, 0);

        private static readonly Version MINVERSION = new Version(2, 0);
        private static readonly Version MAXVERSION = new Version(2, 0);

        public string Copyright { get => _copyright; set => _copyright = value; }
        public string Generator { get => _generator; set => _generator = value; }

        public Version Version { get => Version.TryParse(_version, out Version ver) ? ver : ZEROVERSION; }
        public Version MinVersion { get => Version.TryParse(_minVersion, out Version ver) ? ver : ZEROVERSION; }

        #endregion

        #region API

        public override IEnumerable<Exception> Validate()
        {
            foreach (var ex in base.Validate()) yield return ex;

            if (string.IsNullOrWhiteSpace(_version)) yield return new ModelException(this, "version number is missing");            

            var curVer = this.Version;
            var minVer = this.MinVersion;            

            if (curVer < MINVERSION) yield return new ModelException(this, $"invalid version number {this.Version} expected {MINVERSION}");
            if (curVer > MAXVERSION) yield return new ModelException(this, $"invalid version number {this.Version} expected {MAXVERSION}");            
        }

        #endregion
    }        

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

            _accessors = new ChildrenCollection<Accessor, ROOT>(this);
            _animations = new ChildrenCollection<Animation, ROOT>(this);
            _buffers = new ChildrenCollection<Buffer, ROOT>(this);
            _bufferViews = new ChildrenCollection<BufferView, ROOT>(this);
            _cameras = new ChildrenCollection<Camera, ROOT>(this);
            _images = new ChildrenCollection<Image, ROOT>(this);
            _materials = new ChildrenCollection<Material, ROOT>(this);
            _meshes = new ChildrenCollection<Mesh, ROOT>(this);
            _nodes = new ChildrenCollection<Node, ROOT>(this);
            _samplers = new ChildrenCollection<Sampler, ROOT>(this);
            _scenes = new ChildrenCollection<Scene, ROOT>(this);
            _skins = new ChildrenCollection<Skin, ROOT>(this);
            _textures = new ChildrenCollection<Texture, ROOT>(this);            
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
