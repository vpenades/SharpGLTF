using System;
using System.Collections.Generic;
using System.Text;
using System.Numerics;

using SharpGLTF.Geometry.VertexTypes;

namespace SharpGLTF.Geometry
{
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
    /// <see cref="VertexJoints8x4"/>,
    /// <see cref="VertexJoints8x8"/>,
    /// <see cref="VertexJoints16x4"/>,
    /// <see cref="VertexJoints16x8"/>.
    /// </typeparam>
    [System.Diagnostics.DebuggerDisplay("Vertex 𝐏:{Position} {_GetDebugWarnings()}")]
    public partial struct VertexBuilder<TvG, TvM, TvS>
        where TvG : struct, IVertexGeometry
        where TvM : struct, IVertexMaterial
        where TvS : struct, IVertexSkinning
    {
        #region constructors

        public VertexBuilder(TvG g, TvM m, TvS s)
        {
            Geometry = g;
            Material = m;
            Skinning = s;
        }

        public VertexBuilder(TvG g, TvM m, params (int, float)[] bindings)
        {
            Geometry = g;
            Material = m;
            Skinning = default;

            for (int i = 0; i < bindings.Length; ++i)
            {
                Skinning.SetJointBinding(i, bindings[i].Item1, bindings[i].Item2);
            }
        }

        public VertexBuilder(TvG g, TvM m)
        {
            Geometry = g;
            Material = m;
            Skinning = default;
        }

        public VertexBuilder(TvG g, TvS s)
        {
            Geometry = g;
            Material = default;
            Skinning = s;
        }

        public VertexBuilder(TvG g)
        {
            Geometry = g;
            Material = default;
            Skinning = default;
        }

        public VertexBuilder(TvG g, params (int, float)[] bindings)
        {
            Geometry = g;
            Material = default;
            Skinning = default;

            for (int i = 0; i < bindings.Length; ++i)
            {
                Skinning.SetJointBinding(i, bindings[i].Item1, bindings[i].Item2);
            }
        }

        public static implicit operator VertexBuilder<TvG, TvM, TvS>((TvG, TvM, TvS) tuple)
        {
            return new VertexBuilder<TvG, TvM, TvS>(tuple.Item1, tuple.Item2, tuple.Item3);
        }

        public static implicit operator VertexBuilder<TvG, TvM, TvS>((TvG, TvM) tuple)
        {
            return new VertexBuilder<TvG, TvM, TvS>(tuple.Item1, tuple.Item2);
        }

        public static implicit operator VertexBuilder<TvG, TvM, TvS>((TvG, TvS) tuple)
        {
            return new VertexBuilder<TvG, TvM, TvS>(tuple.Item1, tuple.Item2);
        }

        public static implicit operator VertexBuilder<TvG, TvM, TvS>(TvG g)
        {
            return new VertexBuilder<TvG, TvM, TvS>(g);
        }

        public static VertexBuilder<TvG, TvM, TvS> Create(Vector3 position)
        {
            var v = default(VertexBuilder<TvG, TvM, TvS>);
            v.Geometry.SetPosition(position);
            return v;
        }

        public static VertexBuilder<TvG, TvM, TvS> Create(Vector3 position,Vector3 normal)
        {
            var v = default(VertexBuilder<TvG, TvM, TvS>);
            v.Geometry.SetPosition(position);
            v.Geometry.SetNormal(normal);
            return v;
        }

        public static VertexBuilder<TvG, TvM, TvS> Create(Vector3 position, Vector3 normal, Vector4 tangent)
        {
            var v = default(VertexBuilder<TvG, TvM, TvS>);
            v.Geometry.SetPosition(position);
            v.Geometry.SetNormal(normal);
            v.Geometry.SetTangent(tangent);
            return v;
        }

        #endregion

        #region data

        public TvG Geometry;
        public TvM Material;
        public TvS Skinning;

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

        public VertexBuilder<TvG, TvM, TvS> WithGeometry(Vector3 position)
        {
            var v = this;
            v.Geometry.SetPosition(position);
            return v;
        }

        public VertexBuilder<TvG, TvM, TvS> WithGeometry(Vector3 position, Vector3 normal)
        {
            var v = this;
            v.Geometry.SetPosition(position);
            v.Geometry.SetNormal(normal);
            return v;
        }

        public VertexBuilder<TvG, TvM, TvS> WithGeometry(Vector3 position, Vector3 normal, Vector4 tangent)
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

        public VertexBuilder<TvG, TvM, TvS> WithMaterial(Vector4 color0, params Vector2[] uvs)
        {
            var v = this;
            v.Material.SetColor(0, color0);
            for (int i = 0; i < uvs.Length; ++i) v.Material.SetTexCoord(i, uvs[i]);
            return v;
        }

        public VertexBuilder<TvG, TvM, TvS> WithMaterial(Vector4 color0, Vector4 color1, params Vector2[] uvs)
        {
            var v = this;
            v.Material.SetColor(0, color0);
            v.Material.SetColor(1, color1);
            for (int i = 0; i < uvs.Length; ++i) v.Material.SetTexCoord(i, uvs[i]);
            return v;
        }

        public VertexBuilder<TvG, TvM, TvS> WithSkinning(params (int, float)[] bindings)
        {
            var v = this;

            int i = 0;

            while (i < bindings.Length)
            {
                v.Skinning.SetJointBinding(i, bindings[i].Item1, bindings[i].Item2);
                ++i;
            }

            while (i < bindings.Length)
            {
                v.Skinning.SetJointBinding(i, 0, 0);
                ++i;
            }

            return v;
        }

        public void Validate()
        {
            Geometry.Validate();
            Material.Validate();
            Skinning.Validate();
        }

        public VertexBuilder<TvPP, TvMM, TvSS> ConvertTo<TvPP, TvMM, TvSS>()
            where TvPP : struct, IVertexGeometry
            where TvMM : struct, IVertexMaterial
            where TvSS : struct, IVertexSkinning
        {
            var p = Geometry.ConvertTo<TvPP>();
            var m = Material.ConvertTo<TvMM>();
            var s = Skinning.ConvertTo<TvSS>();

            return new VertexBuilder<TvPP, TvMM, TvSS>(p, m, s);
        }

        public static MeshBuilder<TMaterial, TvG, TvM, TvS> CreateCompatibleMesh<TMaterial>(string name = null)
        {
            return new MeshBuilder<TMaterial, TvG, TvM, TvS>(name);
        }

        public static MeshBuilder<TvG, TvM, TvS> CreateCompatibleMesh(string name = null)
        {
            return new MeshBuilder<TvG, TvM, TvS>(name);
        }

        private String _GetDebugWarnings()
        {
            var sb = new StringBuilder();

            if (Geometry.TryGetNormal(out Vector3 n))
            {
                if (!n.IsValidNormal()) sb.Append($" ❌𝚴:{n}");
            }

            if (Geometry.TryGetTangent(out Vector4 t))
            {
                if (!t.IsValidTangent()) sb.Append($" ❌𝚻:{t}");
            }

            for (int i = 0; i < Material.MaxColors; ++i)
            {
                var c = Material.GetColor(i);
                if (!c._IsReal() | !c.IsInRange(Vector4.Zero, Vector4.One)) sb.Append($" ❌𝐂{i}:{c}");
            }

            for (int i = 0; i < Material.MaxTextures; ++i)
            {
                var uv = Material.GetTexCoord(i);
                if (!uv._IsReal()) sb.Append($" ❌𝐔𝐕{i}:{uv}");
            }

            for (int i = 0; i < Skinning.MaxBindings; ++i)
            {
                var jw = Skinning.GetJointBinding(i);
                if (!jw.Weight._IsReal() || jw.Weight < 0 || jw.Joint < 0) sb.Append($" ❌𝐉𝐖{i} {jw.Joint}:{jw.Weight}");
            }

            return sb.ToString();
        }

        #endregion
    }
}
