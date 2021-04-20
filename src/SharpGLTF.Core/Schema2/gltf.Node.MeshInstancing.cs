using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;

using SharpGLTF.Collections;

using TRANSFORM = SharpGLTF.Transforms.AffineTransform;

namespace SharpGLTF.Schema2
{
    [System.Diagnostics.DebuggerDisplay("{Count}")]
    public partial class MeshGpuInstancing
    {
        #region lifecycle
        internal MeshGpuInstancing(Node node)
        {
            _Owner = node;
            _attributes = new Dictionary<string, int>();
        }

        #endregion

        #region data (not serializable)

        private readonly Node _Owner;

        #endregion

        #region properties

        public Node LogicalParent => _Owner;
        public Node VisualParent => _Owner;

        /// <summary>
        /// Gets a value indicating the number of instances to draw.
        /// </summary>
        public int Count => _GetCount();

        public IReadOnlyDictionary<string, Accessor> Accessors => _GetAccessors();

        public IEnumerable<TRANSFORM> LocalTransforms => _GetLocalTransforms();

        #endregion

        #region API

        private int _GetCount()
        {
            return _attributes.Count == 0
                ? 0
                : _attributes.Values
                .Select(item => _Owner.LogicalParent.LogicalAccessors[item].Count)
                .Min();
        }

        private IReadOnlyDictionary<string, Accessor> _GetAccessors()
        {
            return new ReadOnlyLinqDictionary<String, int, Accessor>(_attributes, alidx => this.LogicalParent.LogicalParent.LogicalAccessors[alidx]);
        }

        private IEnumerable<TRANSFORM> _GetLocalTransforms()
        {
            int c = _GetCount();
            for (int i = 0; i < c; ++i) yield return GetLocalTransform(i);
        }

        public void ClearAccessors()
        {
            _attributes.Clear();
        }

        public Accessor GetAccessor(string attributeKey)
        {
            Guard.NotNullOrEmpty(attributeKey, nameof(attributeKey));

            if (!_attributes.TryGetValue(attributeKey, out int idx)) return null;

            return _Owner.LogicalParent.LogicalAccessors[idx];
        }

        public void SetAccessor(string attributeKey, Accessor accessor)
        {
            Guard.NotNullOrEmpty(attributeKey, nameof(attributeKey));

            if (accessor != null)
            {
                Guard.MustShareLogicalParent(_Owner.LogicalParent, nameof(_Owner.LogicalParent), accessor, nameof(accessor));
                if (_attributes.Count > 0) Guard.MustBeEqualTo(Count, accessor.Count, nameof(accessor));

                _attributes[attributeKey] = accessor.LogicalIndex;
            }
            else
            {
                _attributes.Remove(attributeKey);
            }
        }

        public TRANSFORM GetLocalTransform(int index)
        {
            // var m = GetAccessor("TRANSFORM")?.AsMatrix4x4Array()?[index];
            var s = GetAccessor("SCALE")?.AsVector3Array()?[index];
            var r = GetAccessor("ROTATION")?.AsQuaternionArray()?[index];
            var t = GetAccessor("TRANSLATION")?.AsVector3Array()?[index];

            return TRANSFORM.CreateFromAny(null, s, r, t);
        }

        public Matrix4x4 GetLocalMatrix(int index)
        {
            return GetLocalTransform(index).Matrix;
        }

        public Matrix4x4 GetWorldMatrix(int index)
        {
            return GetLocalMatrix(index) * _Owner.WorldMatrix;
        }

        #endregion
    }

    partial class Node
    {
        public MeshGpuInstancing GetGpuInstancing()
        {
            return this.GetExtension<MeshGpuInstancing>();
        }

        public MeshGpuInstancing UseGpuInstancing()
        {
            var ext = GetGpuInstancing();
            if (ext == null)
            {
                ext = new MeshGpuInstancing(this);
                this.SetExtension(ext);
            }

            return ext;
        }

        public void RemoveGpuInstancing()
        {
            this.RemoveExtensions<MeshGpuInstancing>();
        }
    }
}
