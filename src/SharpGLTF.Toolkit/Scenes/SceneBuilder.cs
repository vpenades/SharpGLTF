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
    /// <summary>
    /// Represents the root scene for models, cameras and lights.
    /// </summary>
    [System.Diagnostics.DebuggerDisplay("Scene {Name}")]
    public partial class SceneBuilder : BaseBuilder
    {
        #region lifecycle

        public SceneBuilder(string name = null)
            : base(name) { }

        public SceneBuilder DeepClone(bool cloneArmatures = true)
        {
            var clone = new SceneBuilder();

            clone.SetNameAndExtrasFrom(this);

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

        [Obsolete("Use LoadDefaultScene(...); or LoadAllScenes(...) instead.", true)]
        public static SceneBuilder Load(string filePath, ReadSettings settings = null)
        {
            return LoadDefaultScene(filePath, settings);
        }

        public static SceneBuilder LoadDefaultScene(string filePath, ReadSettings settings = null)
        {
            var mdl = ModelRoot.Load(filePath, settings);
            return CreateFrom(mdl.DefaultScene);
        }

        public static SceneBuilder[] LoadAllScenes(string filePath, ReadSettings settings = null)
        {
            var mdl = ModelRoot.Load(filePath, settings);
            return CreateFrom(mdl);
        }

        #endregion

        #region data

        [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
        internal readonly List<InstanceBuilder> _Instances = new List<InstanceBuilder>();

        #endregion

        #region properties

        /// <summary>
        /// Gets all the instances in this scene.
        /// </summary>
        [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.RootHidden)]
        public IReadOnlyList<InstanceBuilder> Instances => _Instances;

        /// <summary>
        /// Gets all the unique material references shared by all the meshes in this scene.
        /// </summary>
        public IEnumerable<MaterialBuilder> Materials => _Instances.SelectMany(item => item.Materials).Distinct(MaterialBuilder.ReferenceComparer);

        internal IEnumerable<string> AnimationTrackNames => _Instances
            .SelectMany(item => item.Content.GetAnimationTracksNames())
            .Distinct();

        #endregion

        #region API

        public InstanceBuilder AddRigidMesh(MESHBUILDER mesh, NodeBuilder node)
        {
            Guard.NotNull(mesh, nameof(mesh));
            Guard.NotNull(node, nameof(node));

            var instance = new InstanceBuilder(this);
            instance.Content = new RigidTransformer(mesh, node);

            _Instances.Add(instance);

            return instance;
        }

        public InstanceBuilder AddRigidMesh(MESHBUILDER mesh, Transforms.AffineTransform meshWorldTransform)
        {
            Guard.NotNull(mesh, nameof(mesh));

            var instance = new InstanceBuilder(this);
            instance.Content = new FixedTransformer(mesh, meshWorldTransform);

            _Instances.Add(instance);

            return instance;
        }

        public InstanceBuilder AddRigidMesh(MESHBUILDER mesh, NodeBuilder node, Transforms.AffineTransform instanceTransform)
        {
            Guard.NotNull(mesh, nameof(mesh));
            Guard.NotNull(node, nameof(node));

            if (instanceTransform.IsIdentity) return AddRigidMesh(mesh, node);

            var instance = new InstanceBuilder(this);
            instance.Content = new FixedTransformer(mesh, node, instanceTransform);

            _Instances.Add(instance);

            return instance;
        }

        public InstanceBuilder AddSkinnedMesh(MESHBUILDER mesh, Matrix4x4 meshWorldMatrix, params NodeBuilder[] joints)
        {
            Guard.NotNull(mesh, nameof(mesh));
            Guard.IsTrue(meshWorldMatrix.IsValid(_Extensions.MatrixCheck.WorldTransform), nameof(meshWorldMatrix));
            Guard.NotNull(joints, nameof(joints));
            GuardAll.NotNull(joints, nameof(joints));

            var instance = new InstanceBuilder(this);
            instance.Content = new SkinnedTransformer(mesh, meshWorldMatrix, joints);

            _Instances.Add(instance);

            return instance;
        }

        public InstanceBuilder AddSkinnedMesh(MESHBUILDER mesh, params (NodeBuilder Joint, Matrix4x4 InverseBindMatrix)[] joints)
        {
            Guard.NotNull(mesh, nameof(mesh));
            GuardAll.NotNull(joints.Select(item => item.Joint), nameof(joints));
            GuardAll.AreTrue(joints.Select(item => item.InverseBindMatrix.IsValid(_Extensions.MatrixCheck.InverseBindMatrix)), nameof(joints), "Invalid matrix");

            var instance = new InstanceBuilder(this);
            instance.Content = new SkinnedTransformer(mesh, joints);

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
            Guard.NotNull(camera, nameof(camera));
            Guard.IsTrue(cameraPosition._IsFinite(), nameof(cameraPosition));
            Guard.IsTrue(targetPosition._IsFinite(), nameof(targetPosition));

            var xform = Matrix4x4.CreateWorld(cameraPosition, Vector3.Normalize(targetPosition - cameraPosition), Vector3.UnitY);
            return AddCamera(camera, xform);
        }

        public InstanceBuilder AddCamera(CameraBuilder camera, Matrix4x4 cameraMatrix)
        {
            Guard.NotNull(camera, nameof(camera));
            Guard.IsTrue(cameraMatrix.IsValid(_Extensions.MatrixCheck.LocalTransform), nameof(cameraMatrix));

            var content = new CameraContent(camera);
            var instance = new InstanceBuilder(this);
            instance.Content = new FixedTransformer(content, cameraMatrix);
            _Instances.Add(instance);
            return instance;
        }

        public InstanceBuilder AddLight(LightBuilder light, Matrix4x4 lightMatrix)
        {
            Guard.NotNull(light, nameof(light));
            Guard.IsTrue(lightMatrix.IsValid(_Extensions.MatrixCheck.LocalTransform), nameof(lightMatrix));

            var content = new LightContent(light);
            var instance = new InstanceBuilder(this);
            instance.Content = new FixedTransformer(content, lightMatrix);
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

        public InstanceBuilder AddNode(NodeBuilder node)
        {
            Guard.NotNull(node, nameof(node));

            var content = new EmptyContent();
            var instance = new InstanceBuilder(this);
            _Instances.Add(instance);
            instance.Content = new RigidTransformer(content, node);
            return instance;
        }

        /// <summary>
        /// Gets all the unique armatures used by this <see cref="SceneBuilder"/>.
        /// </summary>
        /// <returns>A collection of <see cref="NodeBuilder"/> objects representing the root of each armature.</returns>
        public IReadOnlyList<NodeBuilder> FindArmatures()
        {
            return _Instances
                .Select(item => item.Content.GetArmatureRoot())
                .Where(item => item != null)
                .Distinct()
                .ToList();
        }

        /// <summary>
        /// Applies a tranform the this <see cref="SceneBuilder"/>.
        /// </summary>
        /// <param name="basisTransform">The transform to apply.</param>
        /// <param name="basisNodeName">The name of the dummy root node.</param>
        /// <remarks>
        /// In some circunstances, it's not possible to apply the
        /// <paramref name="basisTransform"/> to the nodes in the scene.<br/>
        /// In these cases a dummy node is created, and these
        /// nodes are made children of this dummy node.
        /// </remarks>
        public void ApplyBasisTransform(Matrix4x4 basisTransform, string basisNodeName = "BasisTransform")
        {
            if (basisTransform == Matrix4x4.Identity) return;

            Guard.IsTrue(basisTransform.IsValid(_Extensions.MatrixCheck.WorldTransform), nameof(basisTransform));

            // find all explicit transforms
            foreach (var fixedXform in _Instances.Select(item => item.Content).OfType<FixedTransformer>())
            {
                fixedXform.ChildTransform = Transforms.AffineTransform.Multiply(fixedXform.ChildTransform, basisTransform);
            }

            // gather all root nodes:
            var rootNodes = this.FindArmatures();

            // find all the nodes that cannot be modified
            bool isExtrinsic(NodeBuilder node)
            {
                if (node.Scale != null) return true;
                if (node.Rotation != null) return true;
                if (node.Translation != null) return true;
                if (node.HasAnimations) return true;
                return false;
            }

            var extrinsicNodes = rootNodes
                .Where(item => isExtrinsic(item))
                .ToList();

            // find all the nodes that we can change their transform matrix safely.
            var intrinsicNodes = rootNodes
                .Except(extrinsicNodes)
                .ToList();

            // apply the transform to the nodes that can be safely changed.
            foreach (var n in intrinsicNodes)
            {
                n.LocalMatrix *= basisTransform;
            }

            if (extrinsicNodes.Count == 0) return;

            // create a proxy node to be used as the root for all sensible nodes.
            var basisNode = new NodeBuilder();
            basisNode.Name = basisNodeName;
            basisNode.LocalMatrix = basisTransform;

            // assign all the sensible nodes to the basis node.
            foreach (var n in extrinsicNodes)
            {
                basisNode.AddNode(n);
            }
        }

        /// <summary>
        /// Copies the instances from <paramref name="scene"/> to this <see cref="SceneBuilder"/>
        /// </summary>
        /// <param name="scene">The source scene.</param>
        /// <param name="sceneTransform">A transform to apply to <paramref name="scene"/> before addition.</param>
        /// <returns>The instances copied from <paramref name="scene"/>.</returns>
        public IReadOnlyList<InstanceBuilder> AddScene(SceneBuilder scene, Matrix4x4 sceneTransform)
        {
            Guard.NotNull(scene, nameof(scene));
            Guard.IsTrue(sceneTransform.IsValid(_Extensions.MatrixCheck.WorldTransform), nameof(sceneTransform));

            scene = scene.DeepClone();
            scene.ApplyBasisTransform(sceneTransform);

            this._Instances.AddRange(scene._Instances);

            return scene._Instances;
        }

        #endregion
    }
}
