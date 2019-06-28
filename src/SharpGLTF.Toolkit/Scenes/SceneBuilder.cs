using System;
using System.Collections.Generic;
using System.Text;

namespace SharpGLTF.Scenes
{
    public partial class SceneBuilder
    {
        #region data

        private readonly List<InstanceBuilder> _Instances = new List<InstanceBuilder>();

        #endregion

        #region properties

        public IReadOnlyList<InstanceBuilder> Instances => _Instances;

        #endregion

        #region API

        public InstanceBuilder CreateInstance()
        {
            var inst = new InstanceBuilder(this);
            _Instances.Add(inst);
            return inst;
        }

        #endregion
    }
}
