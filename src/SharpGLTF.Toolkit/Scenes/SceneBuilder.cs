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

        public SceneBuilder DeepClone()
        {
            var clone = new SceneBuilder();

            clone._Name = this._Name;

            foreach (var inst in this._Instances)
            {
                inst._CopyTo(clone);
            }

            return clone;
        }

        #endregion

        #region data

        private String _Name;

        [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.RootHidden)]
        internal readonly List<InstanceBuilder> _Instances = new List<InstanceBuilder>();

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

        [Obsolete("Use AddRigidMesh")]
        public InstanceBuilder AddMesh(MESHBUILDER mesh, Matrix4x4 meshWorldMatrix)
        {
            return AddRigidMesh(mesh, meshWorldMatrix);
        }

        [Obsolete("Use AddRigidMesh")]
        public InstanceBuilder AddMesh(MESHBUILDER mesh, NodeBuilder node)
        {
            return AddRigidMesh(mesh, node);
        }

        public InstanceBuilder AddRigidMesh(MESHBUILDER mesh, Matrix4x4 meshWorldMatrix)
        {
            return AddRigidMesh(mesh, null, meshWorldMatrix);
        }

        public InstanceBuilder AddRigidMesh(MESHBUILDER mesh, String name, Matrix4x4 meshWorldMatrix)
        {
            Guard.NotNull(mesh, nameof(mesh));

            var instance = new InstanceBuilder(this);
            instance.Content = new FixedTransformer(mesh, meshWorldMatrix, name);

            _Instances.Add(instance);

            return instance;
        }

        public InstanceBuilder AddRigidMesh(MESHBUILDER mesh, NodeBuilder node)
        {
            Guard.NotNull(mesh, nameof(mesh));
            Guard.NotNull(node, nameof(node));

            var instance = new InstanceBuilder(this);
            instance.Content = new RigidTransformer(mesh, node);

            _Instances.Add(instance);

            return instance;
        }

        public InstanceBuilder AddSkinnedMesh(MESHBUILDER mesh, Matrix4x4 meshWorldMatrix, params NodeBuilder[] joints)
        {
            return AddSkinnedMesh(mesh, null, meshWorldMatrix, joints);
        }

        public InstanceBuilder AddSkinnedMesh(MESHBUILDER mesh, String name, Matrix4x4 meshWorldMatrix, params NodeBuilder[] joints)
        {
            Guard.NotNull(mesh, nameof(mesh));
            GuardAll.NotNull(joints, nameof(joints));

            var instance = new InstanceBuilder(this);
            instance.Content = new SkinnedTransformer(mesh, meshWorldMatrix, joints, name);

            _Instances.Add(instance);

            return instance;
        }

        public InstanceBuilder AddSkinnedMesh(MESHBUILDER mesh, params (NodeBuilder Joint, Matrix4x4 InverseBindMatrix)[] joints)
        {
            return AddSkinnedMesh(mesh, null, joints);
        }

        public InstanceBuilder AddSkinnedMesh(MESHBUILDER mesh, String name, params (NodeBuilder Joint, Matrix4x4 InverseBindMatrix)[] joints)
        {
            Guard.NotNull(mesh, nameof(mesh));
            GuardAll.NotNull(joints.Select(item => item.Joint), nameof(joints));

            var instance = new InstanceBuilder(this);
            instance.Content = new SkinnedTransformer(mesh, joints, name);

            _Instances.Add(instance);

            return instance;
        }

        public InstanceBuilder AddCamera(CameraBuilder camera, NodeBuilder node)
        {
            Guard.NotNull(camera, nameof(camera));
            Guard.NotNull(node, nameof(node));

            var content = new CameraContent(camera);
            var instance = new InstanceBuilder(this);
            instance.Content = new RigidTransformer(content, node);
            _Instances.Add(instance);
            return instance;
        }

        public InstanceBuilder AddCamera(CameraBuilder camera, Vector3 cameraPosition, Vector3 targetPosition)
        {
            return AddCamera(camera, null, cameraPosition, targetPosition);
        }

        public InstanceBuilder AddCamera(CameraBuilder camera, String name, Vector3 cameraPosition, Vector3 targetPosition)
        {
            Guard.NotNull(camera, nameof(camera));
            Guard.IsTrue(cameraPosition._IsFinite(), nameof(cameraPosition));
            Guard.IsTrue(targetPosition._IsFinite(), nameof(targetPosition));

            var xform = Matrix4x4.CreateWorld(cameraPosition, Vector3.Normalize(targetPosition - cameraPosition), Vector3.UnitY);
            return AddCamera(camera, name, xform);
        }

        public InstanceBuilder AddCamera(CameraBuilder camera, Matrix4x4 cameraWorldMatrix)
        {
            return AddCamera(camera, null, cameraWorldMatrix);
        }

        public InstanceBuilder AddCamera(CameraBuilder camera, String name, Matrix4x4 cameraWorldMatrix)
        {
            Guard.NotNull(camera, nameof(camera));

            var content = new CameraContent(camera);
            var instance = new InstanceBuilder(this);
            instance.Content = new FixedTransformer(content, cameraWorldMatrix, name);
            _Instances.Add(instance);
            return instance;
        }

        public InstanceBuilder AddLight(LightBuilder light, Matrix4x4 lightWorldMatrix)
        {
            return AddLight(light, null, lightWorldMatrix);
        }

        public InstanceBuilder AddLight(LightBuilder light, String name, Matrix4x4 lightWorldMatrix)
        {
            Guard.NotNull(light, nameof(light));

            var content = new LightContent(light);
            var instance = new InstanceBuilder(this);
            instance.Content = new FixedTransformer(content, lightWorldMatrix, name);
            _Instances.Add(instance);
            return instance;
        }

        public InstanceBuilder AddLight(LightBuilder light, NodeBuilder node)
        {
            Guard.NotNull(light, nameof(light));
            Guard.NotNull(node, nameof(node));

            var content = new LightContent(light);
            var instance = new InstanceBuilder(this);
            instance.Content = new RigidTransformer(content, node);
            _Instances.Add(instance);
            return instance;
        }

        /// <summary>
        /// Adds an instance from this or other <see cref="SceneBuilder"/>
        /// by making a copy of <paramref name="other"/>.
        /// </summary>
        /// <param name="other">The source <see cref="InstanceBuilder"/>.</param>
        /// <returns>The new <see cref="InstanceBuilder"/> added to this scene.</returns>
        public InstanceBuilder AddInstance(InstanceBuilder other)
        {
            if (other == null) return null;
            return other._CopyTo(this);
        }

        public void RenameAllNodes(string namePrefix)
        {
            var allNodes = Instances
                .Select(item => item.Content.GetArmatureRoot())
                .Where(item => item != null)
                .SelectMany(item => NodeBuilder.Flatten(item))
                .Distinct()
                .ToList();

            NodeBuilder.Rename(allNodes, namePrefix);
        }

        #endregion
    }
}
