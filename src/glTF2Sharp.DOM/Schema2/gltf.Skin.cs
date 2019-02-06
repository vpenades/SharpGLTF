using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;

namespace glTF2Sharp.Schema2
{
    using Collections;

    using ROOT = ModelRoot;

    [System.Diagnostics.DebuggerDisplay("Skin[{LogicalIndex}] {Name}")]
    public partial class Skin
    {
        // https://github.com/KhronosGroup/glTF/issues/461
        // https://github.com/KhronosGroup/glTF/issues/100
        // https://github.com/KhronosGroup/glTF/issues/403
        // https://github.com/AnalyticalGraphicsInc/cesium/blob/master/Source/Scene/Model.js#L2526

        // max shader joints
        // https://github.com/KhronosGroup/glTF/issues/283

        #region lifecycle

        internal Skin()
        {
            _joints = new List<int>();
        }

        #endregion

        #region properties

        public int LogicalIndex => this.LogicalParent.LogicalSkins.IndexOfReference(this);

        public IEnumerable<Node> VisualParents => Node.GetNodesUsingSkin(this);

        public int JointsCount => _joints.Count;

        // Skeleton property points to the node that is the root of a joints hierarchy.
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

        public static IEnumerable<Skin> GetSkinsUsing(Node n)
        {
            var idx = n.LogicalIndex;

            return n.LogicalParent.LogicalSkins.Where(s => s._ContainsNode(idx));
        }

        internal bool _ContainsNode(int nodeIdx) { return _joints.Contains(nodeIdx); }

        public Accessor GetInverseBindMatricesAccessor()
        {
            if (!this._inverseBindMatrices.HasValue) return null;

            return this.LogicalParent.LogicalAccessors[this._inverseBindMatrices.Value];
        }

        public KeyValuePair<Node, Matrix4x4> GetJoint(int idx)
        {
            var nodeIdx = _joints[idx];

            var node = this.LogicalParent.LogicalNodes[nodeIdx];

            var matrix = (Matrix4x4)GetInverseBindMatricesAccessor().CastToMatrix4x4Accessor()[idx];

            return new KeyValuePair<Node, Matrix4x4>(node, matrix);
        }

        public override IEnumerable<Exception> Validate()
        {
            var exx = base.Validate().ToList();

            // note: this check will fail if the buffers are not set

            /*
            for(int i=0; i < _joints.Count; ++i)
            {
                var j = GetJoint(i);

                var invXform = j.Value;

                if (invXform.M44 != 1) exx.Add(new ModelException(this, $"Joint {i} has invalid inverse matrix"));                
            }*/

            return exx;
        }

        public bool IsMatch(Node skeleton, KeyValuePair<Node, Matrix4x4>[] joints)
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

        /*
        public void BindJoints(KeyValuePair<Node, Matrix4x4>[] joints)
        {
            // inverse bind matrices accessor

            var data = new Byte[joints.Length * 16 * 4];

            var indexer = new Runtime.Encoding.Matrix4x4Indexer(data, 16 * 4, 0, Runtime.Encoding.PackedType.F32);

            for(int i=0; i < joints.Length; ++i) { indexer[i] = joints[i].Value; }            

            var accessor = LogicalParent._CreateDataAccessor(data, Runtime.Encoding.DimensionType.Matrix4x4, joints.Length);
            this._inverseBindMatrices = accessor.LogicalIndex;

            // joints

            _joints.Clear();
            _joints.AddRange(joints.Select(item => item.Key.LogicalIndex));

        }*/

        #endregion
    }
}
