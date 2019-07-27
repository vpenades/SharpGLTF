using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace SharpGLTF.Scenes
{
    using MESHBUILDER = Geometry.IMeshBuilder<Materials.MaterialBuilder>;

    public partial class SceneBuilder
    {
        #region data

        [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.RootHidden)]
        private readonly List<InstanceBuilder> _Instances = new List<InstanceBuilder>();

        #endregion

        #region properties

        [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
        public IReadOnlyList<InstanceBuilder> Instances => _Instances;

        #endregion

        #region API

        public InstanceBuilder AddMesh(MESHBUILDER mesh, Matrix4x4 transform)
        {
            var instance = new InstanceBuilder(this);
            instance.Content = new StaticTransformer(mesh, transform);

            _Instances.Add(instance);

            return instance;
        }

        public InstanceBuilder AddMesh(MESHBUILDER mesh, NodeBuilder node)
        {
            var instance = new InstanceBuilder(this);
            instance.Content = new NodeTransformer(mesh, node);

            _Instances.Add(instance);

            return instance;
        }

        public InstanceBuilder AddSkinnedMesh(MESHBUILDER mesh, Matrix4x4 meshBindMatrix, params NodeBuilder[] joints)
        {
            var instance = new InstanceBuilder(this);
            instance.Content = new SkinTransformer(mesh, meshBindMatrix, joints);

            _Instances.Add(instance);

            return instance;
        }

        public InstanceBuilder AddSkinnedMesh(MESHBUILDER mesh, params (NodeBuilder, Matrix4x4)[] joints)
        {
            var instance = new InstanceBuilder(this);
            instance.Content = new SkinTransformer(mesh, joints);

            _Instances.Add(instance);

            return instance;
        }

        #endregion
    }
}
