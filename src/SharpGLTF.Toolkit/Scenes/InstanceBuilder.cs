using System;
using System.Collections.Generic;
using System.Text;

using SCHEMA2SCENE = SharpGLTF.Scenes.Schema2SceneBuilder.IOperator<SharpGLTF.Schema2.Scene>;

namespace SharpGLTF.Scenes
{
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

        public string Name => _ContentTransformer?.Name;

        public ContentTransformer Content
        {
            get => _ContentTransformer;
            set => _ContentTransformer = value;
        }

        #endregion

        #region API

        public void Remove()
        {
            if (_Parent == null) return;

            _Parent._Instances.Remove(this);
            _Parent = null;
        }

        internal InstanceBuilder _CopyTo(SceneBuilder other)
        {
            var clone = new InstanceBuilder(other);

            clone._ContentTransformer = this._ContentTransformer?.DeepClone();

            return clone;
        }

        void SCHEMA2SCENE.Setup(Schema2.Scene dstScene, Schema2SceneBuilder context)
        {
            if (_ContentTransformer is SCHEMA2SCENE schema2scb) schema2scb.Setup(dstScene, context);
        }

        #endregion
    }
}
