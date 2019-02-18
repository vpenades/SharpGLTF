using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;

namespace glTF2Sharp.Schema2
{
    using Collections;

    using ROOT = ModelRoot;

    [System.Diagnostics.DebuggerDisplay("MeshPrimitive[{LogicalIndex}] {_mode} {_DebuggerDisplay_TryIdentifyContent()}")]
    public partial class MeshPrimitive : IChildOf<Mesh>
    {
        #region debug

        private String _DebuggerDisplay_TryIdentifyContent()
        {
            return String.Join(" ", VertexAccessors.Keys);
        }

        #endregion

        #region lifecycle

        internal MeshPrimitive()
        {
            _attributes = new Dictionary<string, int>();
            _targets = new List<Dictionary<string, int>>();
        }

        #endregion

        #region properties

        public int LogicalIndex => this.LogicalParent.Primitives.IndexOfReference(this);

        public Mesh LogicalParent { get; private set; }

        void IChildOf<Mesh>._SetLogicalParent(Mesh parent) { LogicalParent = parent; }

        public Material Material
        {
            get => this._material.HasValue ? LogicalParent.LogicalParent.LogicalMaterials[this._material.Value] : null;
            set
            {
                if (value != null) Guard.MustShareLogicalParent(LogicalParent.LogicalParent, value, nameof(value));

                this._material = value == null ? (int?)null : value.LogicalIndex;
            }
        }

        public PrimitiveType DrawPrimitiveType
        {
            get => this._mode.AsValue(_modeDefault);
            set => this._mode = value.AsNullable(_modeDefault);
        }

        public int MorpthTargetsCount => _targets.Count;

        public BoundingBox3? LocalBounds3 => VertexAccessors["POSITION"]?.LocalBounds3;

        public IReadOnlyDictionary<String, Accessor> VertexAccessors => new ReadOnlyLinqDictionary<String, int, Accessor>(_attributes, alidx => this.LogicalParent.LogicalParent.LogicalAccessors[alidx]);

        public Accessor IndexAccessor
        {
            get
            {
                if (!this._indices.HasValue) return null;

                return this.LogicalParent.LogicalParent.LogicalAccessors[this._indices.Value];
            }

            set
            {
                if (value == null)
                {
                    this._indices = null;
                }
                else
                {
                    Guard.MustShareLogicalParent(this.LogicalParent.LogicalParent, value, nameof(value));
                    this._indices = value.LogicalIndex;
                }
            }
        }

        #endregion

        #region API

        public IEnumerable<BufferView> GetBufferViews(bool includeIndices, bool includeVertices, bool includeMorphs)
        {
            var accessors = new List<Accessor>();

            var attributes = this._attributes.Keys.ToArray();

            if (includeIndices)
            {
                if (IndexAccessor != null) accessors.Add(IndexAccessor);
            }

            if (includeVertices)
            {
                accessors.AddRange(attributes.Select(k => VertexAccessors[k]));
            }

            if (includeMorphs)
            {
                for (int i = 0; i < MorpthTargetsCount; ++i)
                {
                    foreach (var key in attributes)
                    {
                        var morpthAccessors = GetMorphTargetAccessors(i);
                        if (morpthAccessors.TryGetValue(key, out Accessor accessor)) accessors.Add(accessor);
                    }
                }
            }

            var indices = accessors
                .Select(item => item._LogicalBufferViewIndex)
                .Where(item => item >= 0)
                .Distinct();

            return indices.Select(idx => this.LogicalParent.LogicalParent.LogicalBufferViews[idx]);
        }

        public Accessor GetVertexAccessor(string attributeKey)
        {
            Guard.NotNullOrEmpty(attributeKey, nameof(attributeKey));

            if (!_attributes.TryGetValue(attributeKey, out int idx)) return null;

            return this.LogicalParent.LogicalParent.LogicalAccessors[idx];
        }

        public void SetVertexAccessor(string attributeKey, Accessor accessor)
        {
            Guard.NotNullOrEmpty(attributeKey, nameof(attributeKey));

            if (accessor != null)
            {
                Guard.MustShareLogicalParent(this.LogicalParent.LogicalParent, accessor, nameof(accessor));
                _attributes[attributeKey] = accessor.LogicalIndex;
            }
            else
            {
                _attributes.Remove(attributeKey);
            }
        }

        public IReadOnlyDictionary<String, Accessor> GetMorphTargetAccessors(int idx)
        {
            return new ReadOnlyLinqDictionary<String, int, Accessor>(_targets[idx], alidx => this.LogicalParent.LogicalParent.LogicalAccessors[alidx]);
        }

        public void SetMorphTargetAccessors(int idx, IReadOnlyDictionary<String, Accessor> accessors)
        {
            Guard.NotNull(accessors, nameof(accessors));
            foreach (var kvp in accessors)
            {
                Guard.MustShareLogicalParent(this.LogicalParent, kvp.Value, nameof(accessors));
            }

            while (_targets.Count <= idx) _targets.Add(new Dictionary<string, int>());

            var target = _targets[idx];

            target.Clear();

            foreach (var kvp in accessors)
            {
                target[kvp.Key] = kvp.Value.LogicalIndex;
            }
        }

        public IReadOnlyList<KeyValuePair<String, Accessor>> GetVertexAccessorsByBuffer(BufferView vb)
        {
            Guard.NotNull(vb, nameof(vb));
            Guard.MustShareLogicalParent(this.LogicalParent, vb, nameof(vb));

            return VertexAccessors
                .Where(key => key.Value.SourceBufferView == vb)
                .OrderBy(item => item.Value.ByteOffset)
                .ToArray();
        }

        public Memory.IntegerArray GetIndices() => IndexAccessor.CastToIndicesAccessor();

        public Geometry.MemoryAccessor GetVertices(string attributeKey) => GetVertexAccessor(attributeKey)._GetMemoryAccessor();

        #endregion

        #region validation

        public override IEnumerable<Exception> Validate()
        {
            var exx = base.Validate().ToList();

            if (IndexAccessor != null)
            {
                switch (DrawPrimitiveType)
                {
                    case PrimitiveType.TRIANGLES:
                        if ((IndexAccessor.Count % 3) != 0) exx.Add(new ModelException(this, $"Indices count {IndexAccessor.Count} incompatible with Primitive.{DrawPrimitiveType}"));
                        break;
                }
            }

            return exx;
        }

        #endregion
    }
}
