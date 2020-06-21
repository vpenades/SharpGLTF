using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;

using SharpGLTF.Materials;
using SharpGLTF.Schema2;

using MESHBUILDER = SharpGLTF.Geometry.IMeshBuilder<SharpGLTF.Materials.MaterialBuilder>;

namespace SharpGLTF.Scenes
{
    [System.Diagnostics.DebuggerDisplay("Scene {_Name}")]
    public partial class SceneBuilder
    {
        #region lifecycle

        public SceneBuilder() { }

        public SceneBuilder(string name)
        {
            _Name = name;
        }

        public SceneBuilder DeepClone(bool cloneArmatures = true)
        {
            var clone = new SceneBuilder();

            clone._Name = this._Name;

            var nodeMap = new Dictionary<NodeBuilder, NodeBuilder>();

            if (cloneArmatures)
            {
                foreach (var root in FindArmatures())
                {
                    var dict = root.DeepClone();

                    foreach (var pair in dict) nodeMap[pair.Key] = pair.Value;
                }
            }

            var args = new ContentTransformer.DeepCloneContext(nodeMap);

            foreach (var inst in this._Instances)
            {
                var cloneInst = inst._CopyTo(clone, args);

                clone._Instances.Add(cloneInst);
            }

            return clone;
        }

        public static SceneBuilder Load(string filePath, ReadSettings settings = null)
        {
            var mdl = ModelRoot.Load(filePath, settings);
            return mdl.DefaultScene.ToSceneBuilder();
        }

        #endregion

        #region data

        [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
        private String _Name;

        [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
        internal readonly List<InstanceBuilder> _Instances = new List<InstanceBuilder>();

        #endregion

        #region properties

        public String Name
        {
            get => _Name;
            set => _Name = value;
        }

        [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.RootHidden)]
        public IReadOnlyList<InstanceBuilder> Instances => _Instances;

        public IEnumerable<MaterialBuilder> Materials => _Instances.SelectMany(item => item.Materials).Distinct(MaterialBuilder.ReferenceComparer);

        #endregion

        #region API

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

        /// <summary>
        /// Gets all the unique armatures used by this <see cref="SceneBuilder"/>.
        /// </summary>
        /// <returns>A collection of <see cref="NodeBuilder"/> objects representing the root of each armature.</returns>
        public IReadOnlyList<NodeBuilder> FindArmatures()
        {
            return _Instances
                .Select(item => item.Content.GetArmatureRoot())
                .Distinct()
                .ToList();
        }

        public void ApplyBasisTransform(Matrix4x4 basisTransform, string basisNodeName = "BasisTransform")
        {
            // gather all root nodes:
            var rootNodes = this.FindArmatures();

            // find all the nodes that cannot be modified
            bool isSensible(NodeBuilder node)
            {
                if (node.Scale != null) return true;
                if (node.Rotation != null) return true;
                if (node.Translation != null) return true;
                if (node.HasAnimations) return true;
                return false;
            }

            var sensibleNodes = rootNodes
                .Where(item => isSensible(item))
                .ToList();

            // find all the nodes that we can change their transform matrix safely.
            var intrinsicNodes = rootNodes
                .Except(sensibleNodes)
                .ToList();

            // apply the transform to the nodes that are safe to change.
            foreach (var n in intrinsicNodes)
            {
                n.LocalMatrix *= basisTransform;
            }

            if (sensibleNodes.Count == 0) return;

            // create a proxy node to be used as the root for all sensible nodes.
            var basisNode = new NodeBuilder();
            basisNode.Name = basisNodeName;
            basisNode.LocalMatrix = basisTransform;

            // assign all the sensible nodes to the basis node.
            foreach (var n in sensibleNodes)
            {
                basisNode.AddNode(n);
            }
        }

        public IReadOnlyList<InstanceBuilder> AddScene(SceneBuilder scene, Matrix4x4 sceneTransform)
        {
            Guard.NotNull(scene, nameof(scene));

            scene = scene.DeepClone();

            if (sceneTransform != Matrix4x4.Identity) scene.ApplyBasisTransform(sceneTransform);

            this._Instances.AddRange(scene._Instances);

            return scene._Instances;
        }

        #endregion
    }
}
