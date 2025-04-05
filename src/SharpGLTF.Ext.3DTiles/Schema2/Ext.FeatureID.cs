using System.Collections.Generic;
using System.Linq;

namespace SharpGLTF.Schema2.Tiles3D
{
    using Collections;

    /// <remarks>
    /// Use <see cref="MeshExtInstanceFeatures.CreateFeatureId"/> to create an instance of this class.
    /// </remarks>    
    public partial class MeshExtInstanceFeatureID : IChildOfList<MeshExtInstanceFeatures> , IMeshFeatureIDInfo
    {
        #region lifecycle

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

            SetProperty(this, ref _texture, new MeshExtMeshFeatureIDTexture());

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

        public IReadOnlyList<int> GetChannels() => _channels;

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
