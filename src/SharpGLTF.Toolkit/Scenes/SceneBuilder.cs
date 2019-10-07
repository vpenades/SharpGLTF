using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;

using MESHBUILDER = SharpGLTF.Geometry.IMeshBuilder<SharpGLTF.Materials.MaterialBuilder>;

namespace SharpGLTF.Scenes
{
    public partial class SceneBuilder
    {
        #region lifecycle

        public SceneBuilder() { }

        public SceneBuilder(string name)
        {
            _Name = name;
        }

        public SceneBuilder Clone()
        {
            var clone = new SceneBuilder();
            clone._Name = this._Name;

            clone._Instances.AddRange(this._Instances.Select(item => item.Clone(this)));

            return clone;
        }

        #endregion

        #region data

        private String _Name;

        [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.RootHidden)]
        private readonly List<InstanceBuilder> _Instances = new List<InstanceBuilder>();

        #endregion

        #region properties

        public String Name
        {
            get => _Name;
            set => _Name = value;
        }

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

        public InstanceBuilder AddSkinnedMesh(MESHBUILDER mesh, Matrix4x4 meshWorldMatrix, params NodeBuilder[] joints)
        {
            var instance = new InstanceBuilder(this);
            instance.Content = new SkinTransformer(mesh, meshWorldMatrix, joints);

            _Instances.Add(instance);

            return instance;
        }

        public InstanceBuilder AddSkinnedMesh(MESHBUILDER mesh, params (NodeBuilder Joint, Matrix4x4 InverseBindMatrix)[] joints)
        {
            var instance = new InstanceBuilder(this);
            instance.Content = new SkinTransformer(mesh, joints);

            _Instances.Add(instance);

            return instance;
        }

        public InstanceBuilder AddCamera(CameraBuilder camera, NodeBuilder node)
        {
            var content = new CameraContent(camera);
            var instance = new InstanceBuilder(this);
            instance.Content = new NodeTransformer(content, node);
            _Instances.Add(instance);
            return instance;
        }

        public InstanceBuilder AddCamera(CameraBuilder camera, Matrix4x4 cameraWorldMatrix)
        {
            var content = new CameraContent(camera);
            var instance = new InstanceBuilder(this);
            instance.Content = new StaticTransformer(content, cameraWorldMatrix);
            _Instances.Add(instance);
            return instance;
        }

        public InstanceBuilder AddCamera(CameraBuilder camera, Vector3 position, Vector3 lookat)
        {
            var xform = Matrix4x4.CreateWorld(position, Vector3.Normalize(lookat - position), Vector3.UnitY);
            return AddCamera(camera, xform);
        }

        public InstanceBuilder AddLight(LightBuilder light, NodeBuilder node)
        {
            var content = new LightContent(light);
            var instance = new InstanceBuilder(this);
            instance.Content = new NodeTransformer(content, node);
            _Instances.Add(instance);
            return instance;
        }

        public InstanceBuilder AddLight(LightBuilder light, Matrix4x4 lightWorldMatrix)
        {
            var content = new LightContent(light);
            var instance = new InstanceBuilder(this);
            instance.Content = new StaticTransformer(content, lightWorldMatrix);
            _Instances.Add(instance);
            return instance;
        }

        public void RenameAllNodes(string namePrefix)
        {
            var allNodes = Instances
                .Select(item => item.Content.GetArmatureAsset())
                .Where(item => item != null)
                .SelectMany(item => NodeBuilder.Flatten(item))
                .Distinct()
                .ToList();

            NodeBuilder.Rename(allNodes, namePrefix);
        }

        #endregion
    }
}
