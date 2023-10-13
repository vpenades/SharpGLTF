using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using JSONEXTRAS = System.Text.Json.Nodes.JsonNode;

namespace SharpGLTF.Scenes
{
    /// <summary>
    /// Represents an element within <see cref="SceneBuilder.Instances"/>
    /// </summary>
    [System.Diagnostics.DebuggerDisplay("{Content}")]
    public sealed class InstanceBuilder
    {
        #region lifecycle

        internal InstanceBuilder(SceneBuilder parent)
        {
            _Parent = parent;
        }

        public InstanceBuilder WithName(string name)
        {
            if (this.Content != null) this.Content.Name = name;
            return this;
        }

        public InstanceBuilder WithExtras(JSONEXTRAS extras)
        {
            if (this.Content != null) this.Content.Extras = extras;
            return this;
        }

        #endregion

        #region data

        [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
        private SceneBuilder _Parent;

        [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
        private ContentTransformer _ContentTransformer;

        #endregion

        #region properties

        /// <summary>
        /// Gets the display text name of this object, or null.
        /// <para><b>⚠️ DO NOT USE AS AN OBJECT ID ⚠️</b> see remarks.</para>
        /// </summary>
        /// <remarks>
        /// glTF does not define any rule for object names.<br/>
        /// This means that names can be null or non unique.<br/>
        /// So don't use names for anything other than object name display.<br/>
        /// If you need to reference objects by some ID, use lookup tables instead.
        /// </remarks>
        public string Name => _ContentTransformer?.Name;

        /// <summary>
        /// Gets the custom data of this object.
        /// </summary>
        public JSONEXTRAS Extras => _ContentTransformer?.Extras;

        /// <summary>
        /// Gets or sets the content of this instance.<br/>
        /// It can be one of those types:<br/>
        /// - <see cref="FixedTransformer"/><br/>
        /// - <see cref="RigidTransformer"/><br/>
        /// - <see cref="SkinnedTransformer"/><br/>
        /// </summary>
        public ContentTransformer Content
        {
            get => _ContentTransformer;
            set => _ContentTransformer = value;
        }

        /// <summary>
        /// Gets the materials used by <see cref="Content"/>.
        /// </summary>
        public IEnumerable<Materials.MaterialBuilder> Materials
        {
            get
            {
                var asset = Content.GetGeometryAsset();
                return asset != null
                    ? asset.Materials
                    : Enumerable.Empty<Materials.MaterialBuilder>();
            }
        }

        #endregion

        #region API

        /// <summary>
        /// Removes this instance from its parent <see cref="SceneBuilder"/>.
        /// </summary>
        public void Remove()
        {
            if (_Parent == null) return;

            _Parent._Instances.Remove(this);
            _Parent = null;
        }

        #endregion

        #region internals

        internal InstanceBuilder _CopyTo(SceneBuilder other, ContentTransformer.DeepCloneContext args)
        {
            var clone = new InstanceBuilder(other);

            clone._ContentTransformer = this._ContentTransformer?.DeepClone(args);

            return clone;
        }

        #endregion
    }
}
