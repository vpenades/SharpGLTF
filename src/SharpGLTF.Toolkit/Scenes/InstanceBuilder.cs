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

        private readonly SceneBuilder _Parent;

        private IContentRoot _Content;

        #endregion

        #region API

        internal Geometry.IMeshBuilder<Materials.MaterialBuilder> GetGeometryAsset()
        {
            if (_Content is IRenderableContent renderable) return renderable.GeometryAsset;

            return null;
        }

        internal NodeBuilder GetArmatureAsset()
        {
            throw new NotImplementedException();
        }

        internal void Setup(Schema2.Scene dstScene, Schema2SceneBuilder context)
        {
            _Content.Setup(dstScene, context);
        }

        #endregion
    }
}
