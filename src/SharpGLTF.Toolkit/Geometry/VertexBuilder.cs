using System;
using System.Collections.Generic;
using System.Text;
using System.Numerics;

using SharpGLTF.Geometry.VertexTypes;

namespace SharpGLTF.Geometry
{
    public interface IVertexBuilder
    {
        IVertexGeometry GetGeometry();
        IVertexMaterial GetMaterial();
        IVertexSkinning GetSkinning();

        /// <summary>
        /// Applies a <see cref="IVertexGeometry"/> set to this <see cref="VertexBuilder{TvG, TvM, TvS}"/>
        /// Remember we're working with struct types; <see href="https://blogs.msdn.microsoft.com/abhinaba/2005/10/05/c-structs-and-interface/"/>
        /// </summary>
        /// <param name="geometry">A <see cref="IVertexGeometry"/> set.</param>
        void SetGeometry(IVertexGeometry geometry);

        /// <summary>
        /// Applies a <see cref="IVertexMaterial"/> set to this <see cref="VertexBuilder{TvG, TvM, TvS}"/>
        /// Remember we're working with struct types; <see href="https://blogs.msdn.microsoft.com/abhinaba/2005/10/05/c-structs-and-interface/"/>
        /// </summary>
        /// <param name="material">A <see cref="IVertexMaterial"/> set.</param>
        void SetMaterial(IVertexMaterial material);

        /// <summary>
        /// Applies a <see cref="IVertexSkinning"/> set to this <see cref="VertexBuilder{TvG, TvM, TvS}"/>
        /// Remember we're working with struct types; <see href="https://blogs.msdn.microsoft.com/abhinaba/2005/10/05/c-structs-and-interface/"/>
        /// </summary>
        /// <param name="skinning">A <see cref="IVertexSkinning"/> set.</param>
        void SetSkinning(IVertexSkinning skinning);
    }

    /// <summary>
    /// Represents an individual vertex object.
    /// </summary>
    /// <typeparam name="TvG">
    /// The vertex fragment type with Position, Normal and Tangent.
    /// Valid types are:
    /// <see cref="VertexPosition"/>,
    /// <see cref="VertexPositionNormal"/>,
    /// <see cref="VertexPositionNormalTangent"/>.
    /// </typeparam>
    /// <typeparam name="TvM">
    /// The vertex fragment type with Colors and Texture Coordinates.
    /// Valid types are:
    /// <see cref="VertexEmpty"/>,
    /// <see cref="VertexColor1"/>,
    /// <see cref="VertexTexture1"/>,
    /// <see cref="VertexColor1Texture1"/>.
    /// <see cref="VertexColor1Texture2"/>.
    /// <see cref="VertexColor2Texture2"/>.
    /// </typeparam>
    /// <typeparam name="TvS">
    /// The vertex fragment type with Skin Joint Weights.
    /// Valid types are:
    /// <see cref="VertexEmpty"/>,
    /// <see cref="VertexJoints4"/>,
    /// <see cref="VertexJoints8"/>.
    /// </typeparam>
    [System.Diagnostics.DebuggerDisplay("{_GetDebuggerDisplay(),nq}")]
    public partial struct VertexBuilder<TvG, TvM, TvS> : IVertexBuilder
        where TvG : struct, IVertexGeometry
        where TvM : struct, IVertexMaterial
        where TvS : struct, IVertexSkinning
    {
        #region debug

        internal string _GetDebuggerDisplay()
        {
            var txt = "Vertex";

            txt += " " + _GetDebuggerDisplayTextFrom(Geometry);
            txt += " " + _GetDebuggerDisplayTextFrom(Material);
            txt += " " + _GetDebuggerDisplayTextFrom(Skinning);

            return txt;
        }

        private static string _GetDebuggerDisplayTextFrom(Object o)
        {
            if (o is VertexEmpty) return string.Empty;

            var bindings = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance;

            var method = o.GetType().GetMethod("_GetDebuggerDisplay", bindings);

            if (method == null) return string.Empty;

            return method.Invoke(o, Array.Empty<Object>()) as string;
        }

        private String _GetDebugWarnings()
        {
            var sb = new StringBuilder();

            if (Geometry.TryGetNormal(out Vector3 n))
            {
                if (!n.IsNormalized()) sb.Append($" ❌𝚴:{n}");
            }

            if (Geometry.TryGetTangent(out Vector4 t))
            {
                if (!t.IsValidTangent()) sb.Append($" ❌𝚻:{t}");
            }

            for (int i = 0; i < Material.MaxColors; ++i)
            {
                var c = Material.GetColor(i);
                if (!c._IsFinite() | !c.IsInRange(Vector4.Zero, Vector4.One)) sb.Append($" ❌𝐂{i}:{c}");
            }

            for (int i = 0; i < Material.MaxTextCoords; ++i)
            {
                var uv = Material.GetTexCoord(i);
                if (!uv._IsFinite()) sb.Append($" ❌𝐔𝐕{i}:{uv}");
            }

            for (int i = 0; i < Skinning.MaxBindings; ++i)
            {
                var jw = Skinning.GetJointBinding(i);
                if (!jw.Weight._IsFinite() || jw.Weight < 0 || jw.Index < 0) sb.Append($" ❌𝐉𝐖{i} {jw.Index}:{jw.Weight}");
            }

            return sb.ToString();
        }

        #endregion

        #region constructors

        public VertexBuilder(in TvG g, in TvM m, in TvS s)
        {
            Geometry = g;
            Material = m;
            Skinning = s;
        }

        public VertexBuilder(in TvG g, in TvM m, params (int, float)[] bindings)
        {
            Geometry = g;
            Material = m;

            var sparse = Transforms.SparseWeight8.Create(bindings);

            Skinning = default;
            Skinning.SetWeights(sparse);
        }

        public VertexBuilder(in TvG g, in TvM m, in Transforms.SparseWeight8 bindings)
        {
            Geometry = g;
            Material = m;
            Skinning = default;
            Skinning.SetWeights(bindings);
        }

        public VertexBuilder(in TvG g, in TvM m)
        {
            Geometry = g;
            Material = m;
            Skinning = default;
        }

        public VertexBuilder(in TvG g, in TvS s)
        {
            Geometry = g;
            Material = default;
            Skinning = s;
        }

        public VertexBuilder(in TvG g)
        {
            Geometry = g;
            Material = default;
            Skinning = default;
        }

        public VertexBuilder(in TvG g, params (int Index, float Weight)[] bindings)
        {
            Geometry = g;
            Material = default;

            var sparse = Transforms.SparseWeight8.Create(bindings);

            Skinning = default;
            Skinning.SetWeights(sparse);
        }

        public VertexBuilder(TvG g, Transforms.SparseWeight8 bindings)
        {
            Geometry = g;
            Material = default;
            Skinning = default;
            Skinning.SetWeights(bindings);
        }

        public static implicit operator VertexBuilder<TvG, TvM, TvS>(in (TvG Geo, TvM Mat, TvS Skin) tuple)
        {
            return new VertexBuilder<TvG, TvM, TvS>(tuple.Geo, tuple.Mat, tuple.Skin);
        }

        public static implicit operator VertexBuilder<TvG, TvM, TvS>(in (TvG Geo, TvM Mat) tuple)
        {
            return new VertexBuilder<TvG, TvM, TvS>(tuple.Geo, tuple.Mat);
        }

        public static implicit operator VertexBuilder<TvG, TvM, TvS>(in (TvG Geo, TvS Skin) tuple)
        {
            return new VertexBuilder<TvG, TvM, TvS>(tuple.Geo, tuple.Skin);
        }

        public static implicit operator VertexBuilder<TvG, TvM, TvS>(in TvG g)
        {
            return new VertexBuilder<TvG, TvM, TvS>(g);
        }

        #pragma warning disable CA1000 // Do not declare static members on generic types

        public static VertexBuilder<TvG, TvM, TvS> Create(in Vector3 position)
        {
            var v = default(VertexBuilder<TvG, TvM, TvS>);
            v.Geometry.SetPosition(position);
            return v;
        }

        public static VertexBuilder<TvG, TvM, TvS> Create(in Vector3 position, in Vector3 normal)
        {
            var v = default(VertexBuilder<TvG, TvM, TvS>);
            v.Geometry.SetPosition(position);
            v.Geometry.SetNormal(normal);
            return v;
        }

        public static VertexBuilder<TvG, TvM, TvS> Create(in Vector3 position, in Vector3 normal, in Vector4 tangent)
        {
            var v = default(VertexBuilder<TvG, TvM, TvS>);
            v.Geometry.SetPosition(position);
            v.Geometry.SetNormal(normal);
            v.Geometry.SetTangent(tangent);
            return v;
        }

        public static VertexBuilder<TvG, TvM, TvS> CreateFrom(IVertexBuilder src)
        {
            if (src is VertexBuilder<TvG, TvM, TvS> srcTyped) return srcTyped;

            Guard.NotNull(src, nameof(src));

            var dst = default(VertexBuilder<TvG, TvM, TvS>);

            dst.Geometry = src.GetGeometry().ConvertToGeometry<TvG>();
            dst.Material = src.GetMaterial().ConvertToMaterial<TvM>();
            dst.Skinning = src.GetSkinning().ConvertToSkinning<TvS>();

            return dst;
        }

        #pragma warning restore CA1000 // Do not declare static members on generic types

        #endregion

        #region data

        public TvG Geometry;
        public TvM Material;
        public TvS Skinning;

        public override int GetHashCode() { return Geometry.GetHashCode(); }

        #endregion

        #region properties

        [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
        public Vector3 Position
        {
            get => Geometry.GetPosition();
            set => Geometry.SetPosition(value);
        }

        #endregion

        #region API

        public VertexBuilder<TvG, TvM, TvS> WithGeometry(in Vector3 position)
        {
            var v = this;
            v.Geometry.SetPosition(position);
            return v;
        }

        public VertexBuilder<TvG, TvM, TvS> WithGeometry(in Vector3 position, in Vector3 normal)
        {
            var v = this;
            v.Geometry.SetPosition(position);
            v.Geometry.SetNormal(normal);
            return v;
        }

        public VertexBuilder<TvG, TvM, TvS> WithGeometry(in Vector3 position, in Vector3 normal, in Vector4 tangent)
        {
            var v = this;
            v.Geometry.SetPosition(position);
            v.Geometry.SetNormal(normal);
            v.Geometry.SetTangent(tangent);
            return v;
        }

        public VertexBuilder<TvG, TvM, TvS> WithMaterial(params Vector2[] uvs)
        {
            var v = this;
            for (int i = 0; i < uvs.Length; ++i) v.Material.SetTexCoord(i, uvs[i]);
            return v;
        }

        public VertexBuilder<TvG, TvM, TvS> WithMaterial(in Vector4 color0, params Vector2[] uvs)
        {
            var v = this;
            v.Material.SetColor(0, color0);
            for (int i = 0; i < uvs.Length; ++i) v.Material.SetTexCoord(i, uvs[i]);
            return v;
        }

        public VertexBuilder<TvG, TvM, TvS> WithMaterial(in Vector4 color0, Vector4 color1, params Vector2[] uvs)
        {
            var v = this;
            v.Material.SetColor(0, color0);
            v.Material.SetColor(1, color1);
            for (int i = 0; i < uvs.Length; ++i) v.Material.SetTexCoord(i, uvs[i]);
            return v;
        }

        public VertexBuilder<TvG, TvM, TvS> WithSkinning(params (int Index, float Weight)[] bindings)
        {
            var v = this;

            int i = 0;

            while (i < bindings.Length)
            {
                v.Skinning.SetJointBinding(i, bindings[i].Index, bindings[i].Weight);
                ++i;
            }

            while (i < bindings.Length)
            {
                v.Skinning.SetJointBinding(i, 0, 0);
                ++i;
            }

            return v;
        }

        public VertexBuilder<TvG, TvM, TvS> TransformedBy(in Matrix4x4 transform)
        {
            var clone = this;
            clone.Geometry.ApplyTransform(transform);
            return clone;
        }

        public void Validate()
        {
            Geometry.Validate();
            Material.Validate();
            Skinning.Validate();
        }

        #pragma warning disable CA1000 // Do not declare static members on generic types

        public static MeshBuilder<TMaterial, TvG, TvM, TvS> CreateCompatibleMesh<TMaterial>(string name = null)
        {
            return new MeshBuilder<TMaterial, TvG, TvM, TvS>(name);
        }

        public static MeshBuilder<TvG, TvM, TvS> CreateCompatibleMesh(string name = null)
        {
            return new MeshBuilder<TvG, TvM, TvS>(name);
        }

        #pragma warning restore CA1000 // Do not declare static members on generic types

        IVertexGeometry IVertexBuilder.GetGeometry() { return this.Geometry; }

        IVertexMaterial IVertexBuilder.GetMaterial() { return this.Material; }

        IVertexSkinning IVertexBuilder.GetSkinning() { return this.Skinning; }

        void IVertexBuilder.SetGeometry(IVertexGeometry geometry)
        {
            Guard.NotNull(geometry, nameof(geometry));
            this.Geometry = geometry.ConvertToGeometry<TvG>();
        }

        void IVertexBuilder.SetMaterial(IVertexMaterial material)
        {
            Guard.NotNull(material, nameof(material));
            this.Material = material.ConvertToMaterial<TvM>();
        }

        void IVertexBuilder.SetSkinning(IVertexSkinning skinning)
        {
            Guard.NotNull(skinning, nameof(skinning));
            this.Skinning = skinning.ConvertToSkinning<TvS>();
        }

        #endregion
    }

    struct VertexBuilder : IVertexBuilder
    {
        #region constructor

        public VertexBuilder(IVertexGeometry g)
        {
            this.Geometry = g;
            this.Material = null;
            this.Skinning = null;
        }

        public VertexBuilder(IVertexGeometry g, IVertexMaterial m, IVertexSkinning s)
        {
            this.Geometry = g;
            this.Material = m;
            this.Skinning = s;
        }

        #endregion

        #region data

        public IVertexGeometry Geometry;
        public IVertexMaterial Material;
        public IVertexSkinning Skinning;

        public override int GetHashCode() { return Geometry.GetHashCode(); }

        #endregion

        #region API

        public IVertexGeometry GetGeometry() { return Geometry; }

        public IVertexMaterial GetMaterial() { return Material; }

        public IVertexSkinning GetSkinning() { return Skinning; }

        public void SetGeometry(IVertexGeometry geometry) { this.Geometry = geometry; }

        public void SetMaterial(IVertexMaterial material) { this.Material = material; }

        public void SetSkinning(IVertexSkinning skinning) { this.Skinning = skinning; }

        public IVertexBuilder ConvertToType(Type vertexType)
        {
            var v = (IVertexBuilder)Activator.CreateInstance(vertexType);

            v.SetGeometry(Geometry);
            v.SetMaterial(Material);
            v.SetSkinning(Skinning);

            return v;
        }

        #endregion
    }
}
