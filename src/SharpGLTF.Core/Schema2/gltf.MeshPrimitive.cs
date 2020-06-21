using System;
using System.Collections.Generic;
using System.Linq;

using SharpGLTF.Collections;

namespace SharpGLTF.Schema2
{
    [System.Diagnostics.DebuggerDisplay("{_DebuggerDisplay(),nq}")]
    public sealed partial class MeshPrimitive : IChildOf<Mesh>
    {
        #region debug

        private String _DebuggerDisplay()
        {
            var txt = $"Primitive[{this.LogicalIndex}]";

            return Debug.DebuggerDisplay.ToReport(this, txt);
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

        /// <summary>
        /// Gets the zero-based index of this <see cref="MeshPrimitive"/> at <see cref="Mesh.Primitives"/>.
        /// </summary>
        public int LogicalIndex => this.LogicalParent.Primitives.IndexOfReference(this);

        /// <summary>
        /// Gets the <see cref="Mesh"/> instance that owns this <see cref="MeshPrimitive"/> instance.
        /// </summary>
        public Mesh LogicalParent { get; private set; }

        void IChildOf<Mesh>._SetLogicalParent(Mesh parent) { LogicalParent = parent; }

        /// <summary>
        /// Gets or sets the <see cref="Material"/> instance, or null.
        /// </summary>
        public Material Material
        {
            get => this._material.HasValue ? LogicalParent.LogicalParent.LogicalMaterials[this._material.Value] : null;
            set
            {
                if (value != null) Guard.MustShareLogicalParent(LogicalParent.LogicalParent, nameof(LogicalParent.LogicalParent), value, nameof(value));

                this._material = value == null ? (int?)null : value.LogicalIndex;
            }
        }

        public PrimitiveType DrawPrimitiveType
        {
            get => this._mode.AsValue(_modeDefault);
            set => this._mode = value.AsNullable(_modeDefault);
        }

        public int MorphTargetsCount => _targets.Count;

        public IReadOnlyDictionary<String, Accessor> VertexAccessors => new ReadOnlyLinqDictionary<String, int, Accessor>(_attributes, alidx => this.LogicalParent.LogicalParent.LogicalAccessors[alidx]);

        public Accessor IndexAccessor { get => GetIndexAccessor(); set => SetIndexAccessor(value); }

        #endregion

        #region API - Buffers

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
                for (int i = 0; i < MorphTargetsCount; ++i)
                {
                    foreach (var key in attributes)
                    {
                        var morpthAccessors = GetMorphTargetAccessors(i);
                        if (morpthAccessors.TryGetValue(key, out Accessor accessor)) accessors.Add(accessor);
                    }
                }
            }

            var indices = accessors
                .Select(item => item._SourceBufferViewIndex)
                .Where(item => item >= 0)
                .Distinct();

            return indices.Select(idx => this.LogicalParent.LogicalParent.LogicalBufferViews[idx]);
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

        #endregion

        #region API - Vertices

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
                Guard.MustShareLogicalParent(this.LogicalParent.LogicalParent, nameof(this.LogicalParent.LogicalParent), accessor, nameof(accessor));
                _attributes[attributeKey] = accessor.LogicalIndex;
            }
            else
            {
                _attributes.Remove(attributeKey);
            }
        }

        public Memory.MemoryAccessor GetVertices(string attributeKey)
        {
            return GetVertexAccessor(attributeKey)._GetMemoryAccessor(attributeKey);
        }

        #endregion

        #region API - Indices

        public Accessor GetIndexAccessor()
        {
            if (!this._indices.HasValue) return null;

            return this.LogicalParent.LogicalParent.LogicalAccessors[this._indices.Value];
        }

        public void SetIndexAccessor(Accessor accessor)
        {
            if (accessor == null) { this._indices = null; return; }

            Guard.MustShareLogicalParent(this.LogicalParent.LogicalParent, nameof(this.LogicalParent.LogicalParent), accessor, nameof(accessor));

            this._indices = accessor.LogicalIndex;
        }

        /// <summary>
        /// Gets the raw list of indices of this primitive.
        /// </summary>
        /// <returns>A list of indices, or null.</returns>
        public IList<UInt32> GetIndices() => IndexAccessor?.AsIndicesArray();

        /// <summary>
        /// Decodes the raw indices and returns a list of indexed points.
        /// </summary>
        /// <returns>A sequence of indexed points.</returns>
        public IEnumerable<int> GetPointIndices()
        {
            if (this.DrawPrimitiveType.GetPrimitiveVertexSize() != 1) return Enumerable.Empty<int>();

            if (this.IndexAccessor == null) return Enumerable.Range(0, VertexAccessors.Values.First().Count);

            return this.IndexAccessor.AsIndicesArray().Select(item => (int)item);
        }

        /// <summary>
        /// Decodes the raw indices and returns a list of indexed lines.
        /// </summary>
        /// <returns>A sequence of indexed lines.</returns>
        public IEnumerable<(int A, int B)> GetLineIndices()
        {
            if (this.DrawPrimitiveType.GetPrimitiveVertexSize() != 2) return Enumerable.Empty<(int, int)>();

            if (this.IndexAccessor == null) return this.DrawPrimitiveType.GetLinesIndices(VertexAccessors.Values.First().Count);

            return this.DrawPrimitiveType.GetLinesIndices(this.IndexAccessor.AsIndicesArray());
        }

        /// <summary>
        /// Decodes the raw indices and returns a list of indexed triangles.
        /// </summary>
        /// <returns>A sequence of indexed triangles.</returns>
        public IEnumerable<(int A, int B, int C)> GetTriangleIndices()
        {
            if (this.DrawPrimitiveType.GetPrimitiveVertexSize() != 3) return Enumerable.Empty<(int, int, int)>();

            if (this.IndexAccessor == null) return this.DrawPrimitiveType.GetTrianglesIndices(VertexAccessors.Values.First().Count);

            return this.DrawPrimitiveType.GetTrianglesIndices(this.IndexAccessor.AsIndicesArray());
        }

        #endregion

        #region API - Morph Targets

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

        #endregion

        #region validation

        protected override void OnValidateReferences(Validation.ValidationContext validate)
        {
            base.OnValidateReferences(validate);

            var root = this.LogicalParent.LogicalParent;

            validate
                .IsNullOrIndex("Material", _material, root.LogicalMaterials)
                .IsNullOrIndex("Indices", _indices, root.LogicalAccessors);

            foreach (var idx in _attributes.Values)
            {
                validate.IsNullOrIndex("Attributes", idx, root.LogicalAccessors);
            }

            foreach (var idx in _targets.SelectMany(item => item.Values))
            {
                validate.IsNullOrIndex("Targets", idx, root.LogicalAccessors);
            }
        }

        protected override void OnValidateContent(Validation.ValidationContext validate)
        {
            base.OnValidateContent(validate);

            // all vertices must have the same vertex count

            int vertexCount = -1;

            foreach (var va in VertexAccessors)
            {
                if (vertexCount < 0) { vertexCount = va.Value.Count; continue; }
                validate.AreEqual(va.Key, va.Value.Count, vertexCount);
            }

            // check indices

            if (IndexAccessor != null)
            {
                IndexAccessor.ValidateIndices(validate, (uint)vertexCount, DrawPrimitiveType);

                var incompatibleMode = false;

                switch (this.DrawPrimitiveType)
                {
                    case PrimitiveType.LINE_LOOP:
                    case PrimitiveType.LINE_STRIP:
                        if (IndexAccessor.Count < 2) incompatibleMode = true;
                        break;

                    case PrimitiveType.TRIANGLE_FAN:
                    case PrimitiveType.TRIANGLE_STRIP:
                        if (IndexAccessor.Count < 3) incompatibleMode = true;
                        break;

                    case PrimitiveType.LINES:
                        if (!IndexAccessor.Count.IsMultipleOf(2)) incompatibleMode = true;
                        break;

                    case PrimitiveType.TRIANGLES:
                        if (!IndexAccessor.Count.IsMultipleOf(3)) incompatibleMode = true;
                        break;
                }
            }

            // check vertex attributes accessors ByteStride

            foreach (var group in this.VertexAccessors.Values.GroupBy(item => item.SourceBufferView))
            {
                if (!group.Skip(1).Any()) continue;

                // if more than one accessor shares a BufferView, it must define a ByteStride

                validate.IsGreater("ByteStride", group.Key.ByteStride, 0); // " must be defined when two or more accessors use the same BufferView."

                // determine if we're sequential or strided by checking if the memory buffers overlap
                var memories = group.Select(item => item._GetMemoryAccessor());
                var overlap = Memory.MemoryAccessor.HaveOverlappingBuffers(memories);

                bool ok = false;

                // strided buffer detected
                if (overlap)
                {
                    ok = group.Sum(item => item.Format.ByteSizePadded) == group.Key.ByteStride;
                }

                // sequential buffer detected
                else
                {
                    ok = group.All(item => item.Format.ByteSizePadded <= group.Key.ByteStride);
                }

                if (!ok)
                {
                    var accessors = string.Join(" ", group.Select(item => item.LogicalIndex));
                    validate._LinkThrow("Attributes", $"Inconsistent accessors configuration: {accessors}");
                }
            }

            // check vertex attributes

            if (validate.TryFix)
            {
                var vattributes = this.VertexAccessors
                    .Select(item => item.Value._GetMemoryAccessor(item.Key))
                    .ToArray();

                Memory.MemoryAccessor.SanitizeVertexAttributes(vattributes);
            }

            // find skins using this mesh primitive:

            var skins = this.LogicalParent
                .LogicalParent
                .LogicalNodes
                .Where(item => item.Mesh == this.LogicalParent)
                .Select(item => item.Skin)
                .Where(item => item != null)
                .Select(item => item.JointsCount);

            var maxJoints = skins.Any() ? skins.Max() : 0;

            Accessor.ValidateVertexAttributes(validate, this.VertexAccessors, maxJoints);
        }

        #endregion
    }
}
