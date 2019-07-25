using System;
using System.Collections.Generic;
using System.Text;

namespace SharpGLTF.Scenes
{
    public class InstanceBuilder
    {
        #region lifecycle

        internal InstanceBuilder(SceneBuilder parent)
        {
            _Parent = parent;
        }

        #endregion

        #region data

        [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
        private readonly SceneBuilder _Parent;

        [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
        private IContentRoot _Content;

        #endregion

        #region public

        internal IContentRoot Content
        {
            get => _Content;
            set => _Content = value;
        }

        #endregion

        #region API

        internal Geometry.IMeshBuilder<Materials.MaterialBuilder> GetGeometryAsset()
        {
            return _Content?.GetGeometryAsset();
        }

        internal NodeBuilder GetArmatureAsset()
        {
            return _Content?.GetArmatureAsset();
        }

        internal void Setup(Schema2.Scene dstScene, Schema2SceneBuilder context)
        {
            _Content.Setup(dstScene, context);
        }

        #endregion
    }
}
