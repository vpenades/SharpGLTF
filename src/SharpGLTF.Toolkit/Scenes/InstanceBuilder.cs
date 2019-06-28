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

        internal Geometry.IMeshBuilder<Materials.MaterialBuilder> GetMesh()
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}
