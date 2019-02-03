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
            get => this._material.HasValue ? LogicalParent.LogicalParent._LogicalMaterials[this._material.Value] : null;
            set
            {
                if (value != null) Guard.MustShareLogicalParent(LogicalParent.LogicalParent, value, nameof(value));

                this._material = value == null ? (int?)null : value.LogicalIndex;
            }
        }

        public PrimitiveType DrawPrimitiveType => this._mode.HasValue ? _mode.Value : _modeDefault;           

        public int MorpthTargets => _targets.Count;

        public BoundingBox3? LocalBounds3 => VertexAccessors["POSITION"]?.LocalBounds3;

        public IReadOnlyDictionary<String, Accessor> VertexAccessors => new LinqDictionary<String, int, Accessor>(_attributes, alidx => this.LogicalParent.LogicalParent._LogicalAccessors[alidx]);

        public Accessor IndexAccessor
        {
            get
            {
                if (!this._indices.HasValue) return null;

                return this.LogicalParent.LogicalParent._LogicalAccessors[this._indices.Value];
            }
        }

        #endregion

        #region API

        public IReadOnlyList<KeyValuePair<String,Accessor>> GetVertexAccessorsByBuffer(BufferView vb)
        {
            Guard.NotNull(vb,nameof(vb));
            Guard.MustShareLogicalParent(this.LogicalParent, vb, nameof(vb));

            return VertexAccessors
                .Where(key => key.Value.Buffer ==vb)
                .OrderBy(item => item.Value.ByteOffset)
                .ToArray();
        }

        public IReadOnlyDictionary<String, Accessor> GetMorphTargetAccessors(int idx)
        {
            return new LinqDictionary<String, int, Accessor>(_targets[idx], alidx => this.LogicalParent.LogicalParent._LogicalAccessors[alidx]);
        }

        public void SetMorphTargetAccessors(int idx, IReadOnlyDictionary<String, Accessor> accessors)
        {
            Guard.NotNull(accessors, nameof(accessors));
            foreach (var kvp in accessors)
            {
                Guard.MustShareLogicalParent(this.LogicalParent, kvp.Value,nameof(accessors));
            }

            while (_targets.Count <= idx) _targets.Add(new Dictionary<string, int>());

            var target = _targets[idx];

            target.Clear();

            foreach(var kvp in accessors)
            {
                target[kvp.Key] = kvp.Value.LogicalIndex;
            }
        }

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
                for (int i = 0; i < MorpthTargets; ++i)
                {
                    foreach(var key in attributes)
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

        internal void _SetVertexAccessors(IReadOnlyDictionary<string, Accessor> attributes)
        {
            this._attributes.Clear();

            foreach (var attribute in attributes)
            {
                this._attributes[attribute.Key] = attribute.Value.LogicalIndex;
            }            
        }

        internal void _SetIndexAccessors(Accessor indices, PrimitiveType ptype = PrimitiveType.TRIANGLES)
        {
            if (indices != null) this._indices = indices.LogicalIndex;

            _mode = ptype;
        }

        private Accessor _GetAccessor(IReadOnlyDictionary<string, int> attributes, string attribute)
        {
            if (!attributes.TryGetValue(attribute, out int idx)) return null;

            return this.LogicalParent.LogicalParent._LogicalAccessors[idx];
        }

        private String _DebuggerDisplay_TryIdentifyContent()
        {
            return String.Join(" ", VertexAccessors.Keys);
        }

        #endregion

        #region validation

        public override IEnumerable<Exception> Validate()
        {
            var exx = base.Validate().ToList();

            // Number of vertices or indices(1) is not compatible with used drawing mode('TRIANGLES').

            var indexAccessor = IndexAccessor;

            switch (DrawPrimitiveType)
            {
                case PrimitiveType.TRIANGLES:
                    if ((indexAccessor.Count % 3) != 0) exx.Add(new ModelException(this, $"Indices count {indexAccessor.Count} incompatible with Primitive.{DrawPrimitiveType}"));
                    break;

            }

            return exx;
        }

        #endregion
    }

    [System.Diagnostics.DebuggerDisplay("Mesh[{LogicalIndex}] {Name}")]
    public partial class Mesh
    {
        #region lifecycle

        internal Mesh()
        {
            _primitives = new ChildrenCollection<MeshPrimitive, Mesh>(this);
            _weights = new List<double>();
        }

        #endregion

        #region properties        

        public int LogicalIndex => this.LogicalParent.LogicalMeshes.IndexOfReference(this);

        public IEnumerable<Node> VisualParents => Node.GetNodesUsingMesh(this);

        public IReadOnlyList<MeshPrimitive> Primitives => _primitives;

        public IReadOnlyList<float> MorphWeights => _weights.Select(item => (float)item).ToArray();

        internal MeshPrimitive _AddPrimitive(IReadOnlyDictionary<string, Accessor> attributes, Accessor indices, PrimitiveType ptype)
        {
            if (attributes == null) throw new ArgumentNullException(nameof(attributes));
            if (indices == null) throw new ArgumentNullException(nameof(indices));
            if (!this.ShareLogicalParent(attributes.Values.ToArray())) throw new ArgumentException("Root mismatch",nameof(attributes));
            if (!this.ShareLogicalParent(indices)) throw new ArgumentException("Root mismatch", nameof(indices));

            // we can also check for Accessor ByteOffset match, padding, etc

            // check parenting

            var mp = new MeshPrimitive();

            _primitives.Add(mp);

            mp._SetVertexAccessors(attributes);
            mp._SetIndexAccessors(indices, ptype);           

            return mp;
        }

        public BoundingBox3? LocalBounds3 => BoundingBox3.UnionOf(Primitives.Select(item => item.LocalBounds3));

        #endregion

        #region API

        /*
        internal void _AddPrimitive(MeshPrimitiveBuilder primitive, IReadOnlyDictionary<_DataBuffer, BufferView> sharedBuffers)
        {
            Guard.NotNull(primitive, nameof(primitive));
            Guard.NotNullOrEmpty(sharedBuffers, nameof(sharedBuffers));

            // create vertex accessors
            var vAccessors = new Dictionary<string, Accessor>();
            foreach (var vb in primitive._VertexBuffers)
            {
                var vbview = sharedBuffers[vb.Buffer];

                foreach (var adesc in vb.Attributes)
                {
                    vAccessors[adesc.Name] = LogicalParent._CreateVertexAccessor(vbview, adesc, vb.ByteOffset, vb.Count);
                }
            }

            // create index accessor
            var ib = primitive._IndexBuffer;
            var ibview = sharedBuffers[ib.Buffer];
            var iAccessor = LogicalParent._CreateIndexAccessor(ibview, ib.Attributes[0], ib.ByteOffset, ib.Count);

            // create primitive
            this._AddPrimitive(vAccessors, iAccessor, primitive._PrimitiveType);
        }*/

        public override IEnumerable<Exception> Validate()
        {
            var exx = base.Validate().ToList();

            foreach(var p in this.Primitives)
            {
                exx.AddRange(p.Validate());
            }

            return exx;
        }

        #endregion
    }

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

        public int LogicalIndex => this.LogicalParent._LogicalSkins.IndexOfReference(this);

        public IEnumerable<Node> VisualParents => Node.GetNodesUsingSkin(this);

        public int JointsCount => _joints.Count;

        // Skeleton property points to the node that is the root of a joints hierarchy.
        public Node Skeleton
        {
            get => this._skeleton.HasValue ? this.LogicalParent._LogicalNodes[this._skeleton.Value] : null;
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

            return n.LogicalParent._LogicalSkins.Where(s => s._ContainsNode(idx));
        }

        internal bool _ContainsNode(int nodeIdx) { return _joints.Contains(nodeIdx); }        

        public Accessor GetInverseBindMatricesAccessor()
        {
            if (!this._inverseBindMatrices.HasValue) return null;

            return this.LogicalParent._LogicalAccessors[this._inverseBindMatrices.Value];
        }        

        public KeyValuePair<Node, Matrix4x4> GetJoint(int idx)
        {
            var nodeIdx = _joints[idx];

            var node = this.LogicalParent._LogicalNodes[nodeIdx];

            var matrix = (Matrix4x4)GetInverseBindMatricesAccessor().TryGetAttribute<Matrix4x4>()[idx];

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

            for(int i=0; i < this._joints.Count; ++i)
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


    public partial class ModelRoot
    {
        /*
        public void AddMeshes(MeshCollectionBuilder<int> meshes)
        {
            Guard.NotNull(meshes, nameof(meshes));

            var buffers = _CreateSharedMeshBuffers(meshes._VertexBuffers, meshes._IndexBuffers);

            this._meshes.Clear();            

            foreach (var k in meshes.Keys)
            {
                var srcMesh = meshes[k];

                var dstMesh = new Mesh();
                this._meshes.Add(dstMesh);

                foreach (var p in srcMesh._Primitives)
                {
                    dstMesh._AddPrimitive(p, buffers);
                }
            }
        }        

        public Mesh AddMesh(MeshBuilder srcMesh)
        {
            Guard.NotNull(srcMesh, nameof(srcMesh));

            var buffers = _CreateSharedMeshBuffers(srcMesh._VertexBuffers, srcMesh._IndexBuffers);

            var dstMesh = new Mesh();
            this._meshes.Add(dstMesh);

            foreach (var p in srcMesh._Primitives)
            {
                dstMesh._AddPrimitive(p, buffers);
            }

            return dstMesh;
        }

        private IReadOnlyDictionary<_DataBuffer, BufferView> _CreateSharedMeshBuffers(IEnumerable<_DataBuffer> vertexBuffers, IEnumerable<_DataBuffer> indexBuffers)
        {
            var dict = new Dictionary<_DataBuffer, BufferView>(ReferenceComparer<_DataBuffer>.Instance);

            foreach(var vb in vertexBuffers)
            {
                dict[vb] = CreateVertexBufferView(vb._Data, vb._ByteStride);
            }

            foreach (var vb in indexBuffers)
            {
                dict[vb] = CreateIndexBufferView(vb._Data);
            }

            return dict;
        }*/
    }

    
}
