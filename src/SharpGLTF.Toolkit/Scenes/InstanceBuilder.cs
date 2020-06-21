using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using SharpGLTF.Collections;

using SCHEMA2SCENE = SharpGLTF.Scenes.Schema2SceneBuilder.IOperator<SharpGLTF.Schema2.Scene>;

namespace SharpGLTF.Scenes
{
    [System.Diagnostics.DebuggerDisplay("{Content}")]
    public sealed class InstanceBuilder : SCHEMA2SCENE
    {
        #region lifecycle

        internal InstanceBuilder(SceneBuilder parent)
        {
            _Parent = parent;
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
        /// Gets The name of this instance.
        /// This name represents the name that will take the <see cref="Schema2.Node"/> containing this content.
        /// </summary>
        public string Name => _ContentTransformer?.Name;

        public ContentTransformer Content
        {
            get => _ContentTransformer;
            set => _ContentTransformer = value;
        }

        public IEnumerable<Materials.MaterialBuilder> Materials
        {
            get
            {
                var asset = Content.GetGeometryAsset();
                return asset != null ? asset.Materials : Enumerable.Empty<Materials.MaterialBuilder>();
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

        void SCHEMA2SCENE.Setup(Schema2.Scene dstScene, Schema2SceneBuilder context)
        {
            if (_ContentTransformer is SCHEMA2SCENE schema2scb) schema2scb.Setup(dstScene, context);
        }

        #endregion
    }
}
