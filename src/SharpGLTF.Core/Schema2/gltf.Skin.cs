using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace SharpGLTF.Schema2
{
    [System.Diagnostics.DebuggerDisplay("Skin[{LogicalIndex}] {Name}")]
    public sealed partial class Skin
    {
        // https://registry.khronos.org/glTF/specs/2.0/glTF-2.0.html#skins

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
        /// Gets a collection of <see cref="Node"/> instances using this <see cref="Skin"/>.
        /// </summary>
        public IEnumerable<Node> VisualParents => Node.FindNodesUsingSkin(this);

        /// <summary>
        /// Gets the number of joints.
        /// </summary>
        public int JointsCount => _joints.Count;

        /// <summary>
        /// Gets the list of joints.
        /// </summary>
        /// <remarks>
        /// Each joint is correlated to its respective IBM in <see cref="InverseBindMatrices"/>.
        /// </remarks>
        public IReadOnlyList<Node> Joints => _joints.SelectList(idx => this.LogicalParent.LogicalNodes[idx]);

        /// <summary>
        /// Gets the list of Inverse Bind Matrices.
        /// </summary>
        /// <remarks>
        /// Each IBM is correlated to its respective Joint in <see cref="Joints"/>.
        /// </remarks>
        public IReadOnlyList<Matrix4x4> InverseBindMatrices
        {
            get
            {
                var matrices = GetInverseBindMatricesAccessor();
                if (matrices == null) return Array.Empty<Matrix4x4>();

                System.Diagnostics.Debug.Assert(matrices.Count == _joints.Count, "IBM and Joints count mismatch");

                return matrices.AsMatrix4x4Array();
            }
        }

        /// <summary>
        /// Gets or sets the Skeleton <see cref="Node"/>, which represents the root of a joints hierarchy AKA the Armature Root.
        /// </summary>
        /// <remarks>
        /// <para>
        /// As per <see href="https://registry.khronos.org/glTF/specs/2.0/glTF-2.0.html#skins-overview">glTF specification</see>,
        /// this value is optional. So don't expect all glTF models to have this property set.
        /// </para>
        /// <para>
        /// Although the skeleton property is not needed for computing skinning transforms, it may be used to provide a specific “pivot point” for the skinned geometry.
        /// </para>
        /// </remarks>
        public Node Skeleton
        {
            get
            {
                return this._skeleton.HasValue
                    ? this.LogicalParent.LogicalNodes[this._skeleton.Value]
                    : null;
            }

            set
            {
                if (value != null) Guard.MustShareLogicalParent(this.LogicalParent, nameof(this.LogicalParent), value, nameof(value));
                this._skeleton = value == null
                    ? (int?)null
                    : value.LogicalIndex;
            }
        }

        #endregion

        #region API

        public Accessor UseInverseBindMatricesAccessor()
        {
            var accessor = GetInverseBindMatricesAccessor();
            if (accessor == null)
            {
                accessor = LogicalParent.CreateAccessor("Bind Matrices");
                this._inverseBindMatrices = accessor.LogicalIndex;
            }

            return accessor;
        }

        public Accessor GetInverseBindMatricesAccessor()
        {
            return _inverseBindMatrices.HasValue
                ? this.LogicalParent.LogicalAccessors[this._inverseBindMatrices.Value]
                : null;
        }

        public (Node Joint, Matrix4x4 InverseBindMatrix) GetJoint(int idx)
        {
            var nodeIdx = _joints[idx];

            var node = this.LogicalParent.LogicalNodes[nodeIdx];

            IReadOnlyList<Matrix4x4> matrices = GetInverseBindMatricesAccessor()?.AsMatrix4x4Array();

            var matrix = matrices == null
                ? Matrix4x4.Identity
                : matrices[idx];

            return (node, matrix);
        }

        public void BindJoints(params Node[] joints)
        {
            var rootJoint = _FindCommonAncestor(joints);

            BindJoints(rootJoint.WorldMatrix, joints);
        }

        /// <summary>
        /// Binds the armature <see cref="Node"/>s to the associated skinned mesh.
        /// </summary>
        /// <param name="meshBindTransform">The world transform matrix of the mesh at the time of binding.</param>
        /// <param name="joints">A collection of <see cref="Node"/> joints.</param>
        /// <remarks>
        /// This method uses the <see cref="Node.WorldMatrix"/> value of each joint to computer the inverse bind matrix.
        /// </remarks>
        public void BindJoints(Matrix4x4 meshBindTransform, params Node[] joints)
        {
            Guard.NotNull(joints, nameof(joints));

            var pairs = new (Node, Matrix4x4)[joints.Length];

            for (int i = 0; i < pairs.Length; ++i)
            {
                Guard.NotNull(joints[i], nameof(joints));

                var xform = (Matrix4x4)Transforms.SkinnedTransform.CalculateInverseBinding((Transforms.Matrix4x4Double)meshBindTransform, joints[i].WorldMatrixPrecise);

                pairs[i] = (joints[i], xform);
            }

            BindJoints(pairs);
        }

        /// <summary>
        /// Binds a bone armature of <see cref="Node"/> to the associated skinned mesh.
        /// </summary>
        /// <param name="joints">
        /// A collection of <see cref="Node"/> joints,
        /// where each joint has an Inverse Bind Matrix.
        /// </param>
        public void BindJoints(IReadOnlyList<(Node Joint, Matrix4x4 InverseBindMatrix)> joints)
        {
            Guard.NotNull(joints, nameof(joints));

            _FindCommonAncestor(joints.Select(item => item.Joint));
            
            // sanitize IBMs

            Matrix4x4 _SanitizedIBM(Matrix4x4 ibm, int idx)
            {
                Transforms.Matrix4x4Factory.GuardMatrix($"{nameof(joints)}[{idx}]", ibm, Transforms.Matrix4x4Factory.MatrixCheck.InverseBindMatrix, 0.01f);

                // Acording to gltf specs https://www.khronos.org/registry/glTF/specs/2.0/glTF-2.0.html#skins-overview
                // "The fourth row of each matrix MUST be set to [0.0, 0.0, 0.0, 1.0]."                

                // fourth column (row in schema) has passed the guard and it is within tolerance so we can enforce exact values,
                ibm.M14 = 0;
                ibm.M24 = 0;
                ibm.M34 = 0;
                ibm.M44 = 1;

                return ibm;
            }

            var ibms = joints.Select((item, idx) => _SanitizedIBM(item.InverseBindMatrix, idx));

            // inverse bind matrices accessor

            var data = new Byte[joints.Count * 16 * 4];
            var matrices = new Memory.Matrix4x4Array(data, 0, EncodingType.FLOAT, false);
            matrices.Fill(ibms);

            var ibmsView = LogicalParent.UseBufferView(data);

            UseInverseBindMatricesAccessor().SetData(ibmsView, 0, joints.Count, DimensionType.MAT4, EncodingType.FLOAT, false);            

            // joints

            _joints.Clear();
            _joints.AddRange(joints.Select(item => item.Joint.LogicalIndex));
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
                var (j, ibm) = GetJoint(i);

                if (!ReferenceEquals(src.Key, j)) return false;
                if (src.Value != ibm) return false;
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
            if (nodes == null) return null;

            nodes = nodes.EnsureList();

            foreach (var j in nodes)
            {
                Guard.NotNull(j, nameof(nodes));
                Guard.MustShareLogicalParent(this, j, nameof(nodes));
            }            

            var rootJoint = nodes.First();

            while (true)
            {
                if (nodes.All(j => rootJoint == j || rootJoint._ContainsVisualNode(j, true))) return rootJoint;

                if (rootJoint.VisualParent == null) break;

                rootJoint = rootJoint.VisualParent;
            }

            Guard.IsTrue(false, nameof(nodes), "Common ancestor not found");
            return null;
        }

        #endregion

        #region validation

        protected override void OnValidateReferences(Validation.ValidationContext validate)
        {
            validate
                .IsNullOrIndex("Skeleton", _skeleton, this.LogicalParent.LogicalNodes)
                .IsNullOrIndex("InverseBindMatrices", _inverseBindMatrices, this.LogicalParent.LogicalAccessors)
                .IsGreaterOrEqual("Joints", _joints.Count, _jointsMinItems); // althought mathematically correct, there's no point in having a skin with just one node.

            var ibxAccessor = GetInverseBindMatricesAccessor();
            if (ibxAccessor != null) validate.AreEqual("InverseBindMatrices", ibxAccessor.Count, _joints.Count);

            Node commonRoot = null;

            for (int i = 0; i < _joints.Count; ++i)
            {
                var jidx = _joints[i];

                validate.IsNullOrIndex("Joints", jidx, this.LogicalParent.LogicalNodes);

                var jnode = this.LogicalParent.LogicalNodes[jidx];
                var jroot = jnode.VisualRoot;

                if (commonRoot == null) { commonRoot = jroot; continue; }

                validate.GetContext(jroot).AreSameReference("Root", commonRoot, jroot);
            }

            base.OnValidateReferences(validate);
        }

        protected override void OnValidateContent(Validation.ValidationContext validate)
        {
            var ibxAccessor = GetInverseBindMatricesAccessor();

            if (ibxAccessor != null)
            {
                validate.AreEqual("InverseBindMatrices", _joints.Count, ibxAccessor.Count);

                ibxAccessor.ValidateMatrices4x3(validate, true, false);
            }

            if (_skeleton.HasValue)
            {
                var skeletonNode = this.Skeleton;

                for (int i = 0; i < this.JointsCount; ++i)
                {
                    var jointNode = GetJoint(i).Joint;

                    if (skeletonNode == jointNode) continue;
                    if (skeletonNode._ContainsVisualNode(jointNode, true)) continue;

                    // result.AddError(this, $"Skeleton node is not a common ancestor of Joint[{i}]");
                }
            }

            base.OnValidateContent(validate);
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
