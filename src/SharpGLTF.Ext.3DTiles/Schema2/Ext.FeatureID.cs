using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;

namespace SharpGLTF.Schema2.Tiles3D
{
    using Collections;

    /// <summary>
    /// Mesh Feature Ids
    /// </summary>
    /// <remarks>
    /// Implemented by <see cref="MeshExtInstanceFeatureID"/> and <see cref="MeshExtMeshFeatureID"/>
    /// </remarks>
    public interface IMeshFeatureIDInfo
    {
        /// <summary>
        /// The number of unique features in the attribute or texture.
        /// </summary>
        public int FeatureCount { get; set; }

        /// <summary>
        /// A value that indicates that no feature is associated with this vertex or texel.
        /// </summary>
        public int? NullFeatureId { get; set; }

        /// <summary>
        /// An attribute containing feature IDs. When `attribute` and `texture` are omitted the 
        /// feature IDs are assigned to vertices by their index.
        /// </summary>
        public int? Attribute { get; set; }

        /// <summary>
        /// A label assigned to this feature ID set. Labels must be alphanumeric identifiers 
        /// matching the regular expression `^[a-zA-Z_][a-zA-Z0-9_]*$`.
        /// </summary>
        public string Label { get; set; }

        /// <summary>
        /// The index of the property table containing per-feature property values. Only applicable when using the `EXT_structural_metadata` extension.
        /// </summary>
        public int? PropertyTableIndex { get; set; }        
    }

    public sealed class FeatureIDBuilder : IMeshFeatureIDInfo , IEquatable<IMeshFeatureIDInfo>
    {
        public FeatureIDBuilder(int featureCount, int? attribute = null, string label = null, int? nullFeatureId = null)
        {
            FeatureCount = featureCount;
            Label = label;
            Attribute = attribute;
            NullFeatureId = nullFeatureId;
        }

        public FeatureIDBuilder(PropertyTable table, int? attribute = null, string label = null, int? nullFeatureId = null)
        {
            FeatureCount = table.Count;
            Attribute = attribute;
            _root = table.LogicalParent;
            PropertyTableIndex = table.LogicalIndex;
            Label = label;
            NullFeatureId = nullFeatureId;
        }

        private readonly EXTStructuralMetadataRoot _root;

        public int FeatureCount { get; set; }
        public int? NullFeatureId { get; set; }
        public int? Attribute { get; set; }
        public string Label { get; set; }
        public int? PropertyTableIndex { get; set; }

        public Texture Texture { get; set; }
        public IReadOnlyList<int> Channels { get; set; }

        public bool Equals(IMeshFeatureIDInfo other)
        {
            if (other == null) return false;
            if (this.FeatureCount != other.FeatureCount) return false;
            if (this.NullFeatureId != other.NullFeatureId) return false;
            if (this.Attribute != other.Attribute) return false;
            if (this.Label != other.Label) return false;
            if (this.PropertyTableIndex != other.PropertyTableIndex) return false;

            return true;
        }
    }

    /// <remarks>
    /// Use <see cref="MeshExtInstanceFeatures.CreateFeatureId"/> to create an instance of this class.
    /// </remarks>    
    public partial class MeshExtInstanceFeatureID : IChildOfList<MeshExtInstanceFeatures> , IMeshFeatureIDInfo
    {
        #region lifecycle

        /*
        public MeshExtInstanceFeatureID(int featureCount, int? attribute = null, int? propertyTable = null, string label = null, int? nullFeatureId = null)
        {
            FeatureCount = featureCount;
            Attribute = attribute;
            Label = label;
            PropertyTableIndex = propertyTable;
            NullFeatureId = nullFeatureId;
        }*/


        internal MeshExtInstanceFeatureID() { }

        #endregion

        #region child properties

        /// <summary>
        /// Gets the zero-based index of this <see cref="MeshExtInstanceFeatureID"/> at <see cref="MeshExtInstanceFeatures.FeatureIds"/>.
        /// </summary>
        public int LogicalIndex { get; private set; } = -1;

        /// <summary>
        /// Gets the <see cref="MeshExtInstanceFeatures"/> instance that owns this <see cref="MeshExtInstanceFeatureID"/> instance.
        /// </summary>
        public MeshExtInstanceFeatures LogicalParent { get; private set; }

        void IChildOfList<MeshExtInstanceFeatures>.SetLogicalParent(MeshExtInstanceFeatures parent, int index)
        {
            LogicalParent = parent;
            LogicalIndex = index;
        }

        #endregion

        #region IMeshFeatureIDInfo properties        
        public int FeatureCount
        {
            get => _featureCount;
            set
            {
                Guard.MustBeGreaterThanOrEqualTo(value, _featureCountMinimum, nameof(value));
                _featureCount = value;
            }
        }        
        public int? NullFeatureId
        {
            get => _nullFeatureId;
            set
            {
                if (value.HasValue) Guard.MustBeGreaterThanOrEqualTo(value.Value, _nullFeatureIdMinimum, nameof(value));
                _nullFeatureId = value;
            }
        }        
        public int? Attribute
        {
            get => _attribute;
            set => _attribute = value;
        }        
        public string Label
        {
            get => _label;
            set
            {
                if (value != null) Guard.IsTrue(System.Text.RegularExpressions.Regex.IsMatch(value, "^[a-zA-Z_][a-zA-Z0-9_]*$"), nameof(value));
                _label = value;
            }
        }        
        public int? PropertyTableIndex
        {
            get => _propertyTable;
            set
            {
                if (value.HasValue) Guard.MustBeGreaterThanOrEqualTo(value.Value, _propertyTableMinimum, nameof(value));
                _propertyTable = value;
            }
        }

        #endregion
    }

    /// <remarks>
    /// Use <see cref="MeshExtMeshFeatures.CreateFeatureID"/> to create an instance of this class.
    /// </remarks>
    public partial class MeshExtMeshFeatureID : IChildOfList<MeshExtMeshFeatures>, IMeshFeatureIDInfo
    {
        #region lifecycle

        internal MeshExtMeshFeatureID() { }

        protected override IEnumerable<ExtraProperties> GetLogicalChildren()
        {
            var items = base.GetLogicalChildren();

            if (_texture != null) items = items.Append(_texture);

            return items;
        }

        #endregion

        #region child properties

        public int LogicalIndex { get; private set; } = -1;

        public MeshExtMeshFeatures LogicalParent { get; private set; }

        void IChildOfList<MeshExtMeshFeatures>.SetLogicalParent(MeshExtMeshFeatures parent, int index)
        {
            LogicalParent = parent;
            LogicalIndex = index;
        }

        #endregion

        #region IMeshFeatureIDInfo properties        
        public int FeatureCount
        {
            get => _featureCount;
            set
            {
                Guard.MustBeGreaterThanOrEqualTo(value, _featureCountMinimum, nameof(value));
                _featureCount = value;
            }
        }        
        public int? NullFeatureId
        {
            get => _nullFeatureId;
            set
            {
                if (value.HasValue) Guard.MustBeGreaterThanOrEqualTo(value.Value, _nullFeatureIdMinimum, nameof(value));
                _nullFeatureId = value;
            }
        }        
        public int? Attribute
        {
            get => _attribute;
            set => _attribute = value;
        }        
        public string Label
        {
            get => _label;
            set
            {
                if (value != null) Guard.IsTrue(System.Text.RegularExpressions.Regex.IsMatch(value, "^[a-zA-Z_][a-zA-Z0-9_]*$"), nameof(value));
                _label = value;
            }
        }                
        public int? PropertyTableIndex
        {
            get => _propertyTable;
            set
            {
                if (value.HasValue) Guard.MustBeGreaterThanOrEqualTo(value.Value, _propertyTableMinimum, nameof(value));
                _propertyTable = value;
            }
        }

        #endregion

        #region API

        // question: is _texture required to always exist? if that would be the case, then it should be created
        // in the constructor an exposed as a read only property.

        /// <summary>
        /// Gets a texture containing feature IDs.
        /// </summary>
        public MeshExtMeshFeatureIDTexture GetTexture() => _texture;

        /// <summary>
        /// Gets or Creates a texture containing feature IDs.
        /// </summary>
        public MeshExtMeshFeatureIDTexture UseTexture()
        {
            if (_texture != null) return _texture;

            GetChildSetter(this).SetProperty(ref _texture, new MeshExtMeshFeatureIDTexture());

            return _texture;
        }

        #endregion
    }

    /// <remarks>
    /// Use <see cref="MeshExtMeshFeatureID.UseTexture"/> to create an instance of this class.
    /// </remarks>
    public partial class MeshExtMeshFeatureIDTexture : IChildOf<MeshExtMeshFeatureID>
    {
        #region lifecycle
        internal MeshExtMeshFeatureIDTexture()
        {
            _channels = new List<int>();
        }

        #endregion

        #region child properties        

        public MeshExtMeshFeatureID LogicalParent { get; private set; }

        void IChildOf<MeshExtMeshFeatureID>.SetLogicalParent(MeshExtMeshFeatureID parent)
        {
            LogicalParent = parent;            
        }

        #endregion

        #region properties        

        public Schema2.Texture Texture
        {
            get => _GetModelRoot().LogicalTextures[LogicalTextureIndex];
            set
            {
                Guard.NotNull(value, nameof(value));
                Guard.MustShareLogicalParent(_GetModelRoot(), nameof(MeshExtMeshFeatureIDTexture), value, nameof(value));

                LogicalTextureIndex = value.LogicalIndex;
            }
        }

        #endregion

        #region API

        private ModelRoot _GetModelRoot()
        {
            // traverse up to the root:
            return LogicalParent?.LogicalParent?.LogicalParent?.LogicalParent?.LogicalParent;
        }

        public void SetChannels(IReadOnlyList<int> channels)
        {
            Guard.NotNullOrEmpty(channels, nameof(channels));
            Guard.MustBeGreaterThanOrEqualTo(channels.Count, _channelsMinItems, nameof(channels));

            _channels.Clear();
            _channels.AddRange(channels);
        }

        #endregion
    }
}
