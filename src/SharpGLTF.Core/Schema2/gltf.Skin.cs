using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace SharpGLTF.Schema2
{
    [System.Diagnostics.DebuggerDisplay("Skin[{LogicalIndex}] {Name}")]
    public sealed partial class Skin
    {
        // https://github.com/KhronosGroup/glTF/issues/461
        // https://github.com/KhronosGroup/glTF/issues/100
        // https://github.com/KhronosGroup/glTF/issues/403
        // https://github.com/AnalyticalGraphicsInc/cesium/blob/master/Source/Scene/Model.js#L2526

        // max shader joints
        // https://github.com/KhronosGroup/COLLADA2GLTF/issues/110

        #region lifecycle

        internal Skin()
        {
            _joints = new List<int>();
        }

        #endregion

        #region properties

        /// <summary>
        /// Gets the zero-based index of this <see cref="Skin"/> at <see cref="ModelRoot.LogicalSkins"/>
        /// </summary>
        public int LogicalIndex => this.LogicalParent.LogicalSkins.IndexOfReference(this);

        /// <summary>
        /// Gets a collection of <see cref="Node"/> instances using this <see cref="Skin"/>.
        /// </summary>
        public IEnumerable<Node> VisualParents => Node.FindNodesUsingSkin(this);

        /// <summary>
        /// Gets the number of joints
        /// </summary>
        public int JointsCount => _joints.Count;

        /// <summary>
        /// Gets or sets the Skeleton <see cref="Node"/>, which represents the root of a joints hierarchy.
        /// </summary>
        public Node Skeleton
        {
            get => this._skeleton.HasValue ? this.LogicalParent.LogicalNodes[this._skeleton.Value] : null;
            set
            {
                if (value != null) Guard.MustShareLogicalParent(this.LogicalParent, value, nameof(value));
                this._skeleton = value == null ? (int?)null : value.LogicalIndex;
            }
        }

        #endregion

        #region API

        public Accessor GetInverseBindMatricesAccessor()
        {
            if (!this._inverseBindMatrices.HasValue) return null;

            return this.LogicalParent.LogicalAccessors[this._inverseBindMatrices.Value];
        }

        public KeyValuePair<Node, Matrix4x4> GetJoint(int idx)
        {
            var nodeIdx = _joints[idx];

            var node = this.LogicalParent.LogicalNodes[nodeIdx];

            var accessor = GetInverseBindMatricesAccessor();

            var matrix = accessor == null ? Matrix4x4.Identity : accessor.AsMatrix4x4Array()[idx];

            return new KeyValuePair<Node, Matrix4x4>(node, matrix);
        }

        public void BindJoints(params Node[] joints)
        {
            var rootJoint = _FindCommonAncestor(joints);

            BindJoints(rootJoint.WorldMatrix, joints);
        }

        /// <summary>
        /// Binds the tree of bones to the associated skinned mesh.
        /// </summary>
        /// <param name="meshBindTransform">The world transform matrix of the mesh at the time of binding.</param>
        /// <param name="joints">A collection of <see cref="Node"/> joints.</param>
        public void BindJoints(Matrix4x4 meshBindTransform, params Node[] joints)
        {
            var meshBingInverse = meshBindTransform.Inverse();

            var pairs = new KeyValuePair<Node, Matrix4x4>[joints.Length];

            for (int i = 0; i < pairs.Length; ++i)
            {
                var xform = (joints[i].WorldMatrix * meshBingInverse).Inverse();

                pairs[i] = new KeyValuePair<Node, Matrix4x4>(joints[i], xform);
            }

            BindJoints(pairs);
        }

        public void BindJoints(KeyValuePair<Node, Matrix4x4>[] joints)
        {
            _FindCommonAncestor(joints.Select(item => item.Key));

            foreach (var j in joints) { Guard.IsTrue(j.Value._IsReal(), nameof(joints)); }

            // inverse bind matrices accessor

            var data = new Byte[joints.Length * 16 * 4];

            var matrices = new Memory.Matrix4x4Array(data.Slice(0), 0, EncodingType.FLOAT, false);

            matrices.Fill(joints.Select(item => item.Value));

            var accessor = LogicalParent.CreateAccessor("Bind Matrices");

            accessor.SetData( LogicalParent.UseBufferView(data), 0, joints.Length, DimensionType.MAT4, EncodingType.FLOAT, false);

            this._inverseBindMatrices = accessor.LogicalIndex;

            // joints

            _joints.Clear();
            _joints.AddRange(joints.Select(item => item.Key.LogicalIndex));
        }

        #endregion

        #region helpers

        /// <summary>
        /// Returns true if this <see cref="Skin"/> matches the input values.
        /// </summary>
        /// <param name="skeleton">A <see cref="Node"/> instance that represents the skeleton root.</param>
        /// <param name="joints">A key value pair collection of <see cref="Node"/> joints and their binding matrices.</param>
        /// <returns>True if the input values match this <see cref="Skin"/>.</returns>
        internal bool IsMatch(Node skeleton, KeyValuePair<Node, Matrix4x4>[] joints)
        {
            if (!ReferenceEquals(skeleton, this.Skeleton)) return false;

            if (joints.Length != this._joints.Count) return false;

            for (int i = 0; i < this._joints.Count; ++i)
            {
                var src = joints[i];
                var dst = GetJoint(i);

                if (!ReferenceEquals(src.Key, dst.Key)) return false;
                if (src.Value != dst.Value) return false;
            }

            return true;
        }

        /// <summary>
        /// Finds all the skins that are using the given <see cref="Node"/> as a joint.
        /// </summary>
        /// <param name="jointNode">A <see cref="Node"/> joint.</param>
        /// <returns>A collection of <see cref="Skin"/> instances.</returns>
        internal static IEnumerable<Skin> FindSkinsUsingJoint(Node jointNode)
        {
            var idx = jointNode.LogicalIndex;

            return jointNode
                .LogicalParent
                .LogicalSkins
                .Where(s => s._joints.Contains(idx));
        }

        /// <summary>
        /// Finds all the skins that are using the given <see cref="Node"/> as a skeleton.
        /// </summary>
        /// <param name="skeletonNode">A <see cref="Node"/> skeleton.</param>
        /// <returns>A collection of <see cref="Skin"/> instances.</returns>
        internal static IEnumerable<Skin> FindSkinsUsingSkeleton(Node skeletonNode)
        {
            var idx = skeletonNode.LogicalIndex;

            return skeletonNode
                .LogicalParent
                .LogicalSkins
                .Where(s => s._skeleton == idx);
        }

        /// <summary>
        /// Validates the node tree, ensuring that all nodes share a common ancestor node, and returns it.
        /// </summary>
        /// <param name="nodes">A collection of <see cref="Node"/> joints arranged in a tree.</param>
        /// <returns>The <see cref="Node"/> root of the tree.</returns>
        private Node _FindCommonAncestor(IEnumerable<Node> nodes)
        {
            foreach (var j in nodes) Guard.MustShareLogicalParent(this, j, nameof(nodes));

            var workingNodes = nodes.ToList();

            var rootJoint = workingNodes.First();

            while (true)
            {
                if (workingNodes.All(j => rootJoint == j || rootJoint._ContainsVisualNode(j, true))) return rootJoint;

                if (rootJoint.VisualParent == null) break;

                rootJoint = rootJoint.VisualParent;
            }

            Guard.IsTrue(false, nameof(nodes), "Common ancestor not found");
            return null;
        }

        #endregion

        #region validation

        internal override void Validate(Validation.ValidationContext result)
        {
            base.Validate(result);

            var ibxAccessor = GetInverseBindMatricesAccessor();

            var logicalNodeCount = this.LogicalParent.LogicalNodes.Count;

            if (_joints == null || _joints.Count < _jointsMinItems)
            {
                result.AddError(this, $"Expected at least {_jointsMinItems} Joints");
                return;
            }

            for (int i = 0; i < _joints.Count; ++i)
            {
                var jidx = _joints[i];
                if (jidx < 0 || jidx >= logicalNodeCount) result.AddError(this, $"Joint {i} Node index reference is out of bounds.");

                if (ibxAccessor != null)
                {
                    var ibxform = ibxAccessor.AsMatrix4x4Array()[i];
                    try { ibxform.Inverse(); }
                    catch { result.AddError(this, $"Joint {i} has invalid bind matrix"); }
                }
            }

            if (_skeleton.HasValue)
            {
                if (_skeleton.Value < 0 || _skeleton.Value >= logicalNodeCount) result.AddError(this, $"Skeleton Node index reference is out of bounds.");

                var skeletonNode = this.Skeleton;

                for (int i = 0; i < this.JointsCount; ++i)
                {
                    var jointNode = GetJoint(i).Key;

                    if (skeletonNode == jointNode) continue;
                    if (skeletonNode._ContainsVisualNode(jointNode, true)) continue;

                    result.AddError(this, $"Skeleton node is not a common ancestor of Joint[{i}]");
                }
            }
        }

        #endregion
    }

    public partial class ModelRoot
    {
        /// <summary>
        /// Creates a new <see cref="Skin"/> instance
        /// and adds it to <see cref="ModelRoot.LogicalSkins"/>.
        /// </summary>
        /// <param name="name">The name of the instance.</param>
        /// <returns>A <see cref="Skin"/> instance.</returns>
        public Skin CreateSkin(string name = null)
        {
            var skin = new Skin();
            skin.Name = name;

            this._skins.Add(skin);

            return skin;
        }
    }
}
