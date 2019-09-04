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
                Guard.MustShareLogicalParent(this.LogicalParent.LogicalParent, nameof(this.LogicalParent.LogicalParent), accessor, nameof(accessor));
                _attributes[attributeKey] = accessor.LogicalIndex;
            }
            else
            {
                _attributes.Remove(attributeKey);
            }
        }

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

        public Memory.IntegerArray GetIndices() => IndexAccessor.AsIndicesArray();

        public Memory.MemoryAccessor GetVertices(string attributeKey) => GetVertexAccessor(attributeKey)._GetMemoryAccessor();

        #endregion

        #region validation

        protected override void OnValidateReferences(Validation.ValidationContext result)
        {
            base.OnValidateReferences(result);

            var root = this.LogicalParent.LogicalParent;

            result.CheckReferenceIndex("Material", _material, root.LogicalMaterials);
            result.CheckReferenceIndex("Indices", _indices, root.LogicalAccessors);

            foreach (var idx in _attributes.Values)
            {
                result.CheckReferenceIndex("Attributes", idx, root.LogicalAccessors);
            }

            foreach (var idx in _targets.SelectMany(item => item.Values))
            {
                result.CheckReferenceIndex("Targets", idx, root.LogicalAccessors);
            }
        }

        protected override void OnValidate(Validation.ValidationContext result)
        {
            base.OnValidate(result);

            // check vertex count

            var vertexCounts = VertexAccessors
                .Select(item => item.Value.Count)
                .Distinct();

            if (vertexCounts.Count() != 1)
            {
                result.AddLinkError(Validation.ErrorCodes.MESH_PRIMITIVE_UNEQUAL_ACCESSOR_COUNT);
                return;
            }

            var vertexCount = vertexCounts.First();

            // check indices

            if (IndexAccessor != null)
            {
                if (IndexAccessor.SourceBufferView.ByteStride != 0) result.AddLinkError(Validation.ErrorCodes.MESH_PRIMITIVE_INDICES_ACCESSOR_WITH_BYTESTRIDE);
                IndexAccessor.ValidateIndices(result, (uint)vertexCount, DrawPrimitiveType);

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

                if (incompatibleMode) result.AddLinkWarning(Validation.WarnCodes.MESH_PRIMITIVE_INCOMPATIBLE_MODE, IndexAccessor.Count, this.DrawPrimitiveType);
            }

            // check attributes

            foreach (var group in this.VertexAccessors.Values.GroupBy(item => item.SourceBufferView))
            {
                if (group.Skip(1).Any())
                {
                    if (group.Key.ByteStride == 0) result.AddLinkError(Validation.ErrorCodes.MESH_PRIMITIVE_ACCESSOR_WITHOUT_BYTESTRIDE);
                }
            }

            var positions = GetVertexAccessor("POSITION");
            if (positions != null)
            {
                positions.ValidatePositions(result);
            }
            else
            {
                result.AddSemanticWarning(Validation.WarnCodes.MESH_PRIMITIVE_NO_POSITION);
            }

            GetVertexAccessor("NORMAL")?.ValidateNormals(result);

            var tangents = GetVertexAccessor("TANGENT");
            if (tangents != null)
            {
                if (GetVertexAccessor("NORMAL") == null) result.AddSemanticWarning(Validation.WarnCodes.MESH_PRIMITIVE_TANGENT_WITHOUT_NORMAL);
                if (DrawPrimitiveType == PrimitiveType.POINTS) result.AddSemanticWarning(Validation.WarnCodes.MESH_PRIMITIVE_TANGENT_POINTS);
                tangents.ValidateTangents(result);
            }

        }

        internal void ValidateSkinning(Validation.ValidationContext result, int jointsCount)
        {
            var j0 = GetVertexAccessor("JOINTS_0");
            var w0 = GetVertexAccessor("WEIGHTS_0");
            ValidateSkinning(result, j0, w0, 0, jointsCount);

            var j1 = GetVertexAccessor("JOINTS_1");
            var w1 = GetVertexAccessor("WEIGHTS_1");
            if (j1 != null || w1 != null) ValidateSkinning(result, j1, w1, 1, jointsCount);
        }

        private void ValidateSkinning(Validation.ValidationContext result, Accessor j, Accessor w, int jwset, int jointsCount)
        {
            // if (j == null) result.AddError(this, $"Missing JOINTS_{jwset} vertex attribute");
            // if (w == null) result.AddError(this, $"Missing WEIGHTS_{jwset} vertex attribute");
            if (j == null || w == null) return;

            j.ValidateJoints(result, jwset, jointsCount);
            w.ValidateWeights(result, jwset);
        }

        #endregion
    }
}
