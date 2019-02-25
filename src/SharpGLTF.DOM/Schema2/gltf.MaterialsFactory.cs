using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;

namespace SharpGLTF.Schema2
{
    public partial class Material
    {
        #region lifecycle

        internal static Material CreatePBRMetallicRoughness()
        {
            var m = new Material
            {
                _pbrMetallicRoughness = new MaterialPBRMetallicRoughness()
            };

            return m;
        }

        internal static Material CreatePBRSpecularGlossiness()
        {
            var m = new Material();

            m.SetExtension(new MaterialPBRSpecularGlossiness_KHR());

            return m;
        }

        internal static Material CreateMaterialUnlit()
        {
            var m = new Material();

            m.SetExtension(new MaterialUnlit_KHR());

            return m;
        }

        #endregion

        #region API

        private IEnumerable<MaterialChannelView> _GetChannels()
        {
            if (_pbrMetallicRoughness != null)
            {
                var channels = _pbrMetallicRoughness.GetChannels(this);
                foreach (var c in channels) yield return c;
            }

            var pbrSpecGloss = this.GetExtension<MaterialPBRSpecularGlossiness_KHR>();
            if (pbrSpecGloss != null)
            {
                var channels = pbrSpecGloss.GetChannels(this);
                foreach (var c in channels) yield return c;
            }

            yield return new MaterialChannelView
                (
                this,
                "Normal",
                _GetNormalTexture,
                () => _GetNormalTexture(false) == null ? Vector4.One : new Vector4(1, 1, 1, (float)_GetNormalTexture(false).Scale),
                value => _GetNormalTexture(true).Scale = (double)value.W
                );

            yield return new MaterialChannelView
                (
                this,
                "Occlusion",
                _GetOcclusionTexture,
                () => _GetOcclusionTexture(false) == null ? Vector4.One : new Vector4(1, 1, 1, (float)_GetOcclusionTexture(false).Strength),
                value => _GetOcclusionTexture(true).Strength = (double)value.W
                );

            yield return new MaterialChannelView
                (
                this,
                "Emissive",
                _GetEmissiveTexture,
                () => { var rgb = _emissiveFactor.AsValue(_emissiveFactorDefault); return new Vector4(rgb, 1); },
                value => _emissiveFactor = new Vector3(value.X, value.Y, value.Z).AsNullable(_emissiveFactorDefault)
                );
        }

        #endregion
    }

    public partial class ModelRoot
    {
        public Material AddLogicalMaterial()
        {
            var mat = new Material();

            _materials.Add(mat);

            return mat;
        }

        public Material AddLogicalMaterial(params string[] channelKeys)
        {
            var dict = new Dictionary<string, int>
            {
                ["Unlit"] = MaterialUnlit_KHR.GetScore(channelKeys),
                ["PBRMetallicRoughness"] = MaterialPBRMetallicRoughness.GetScore(channelKeys),
                ["PBRSpecularGlossiness"] = MaterialPBRSpecularGlossiness_KHR.GetScore(channelKeys)
            };

            var materialType = dict.OrderBy(item => item.Value).Last().Key;

            Material m = null;

            if (materialType == "Unlit") m = Material.CreateMaterialUnlit();
            if (materialType == "PBRMetallicRoughness") m = Material.CreatePBRMetallicRoughness();
            if (materialType == "PBRSpecularGlossiness") m = Material.CreatePBRSpecularGlossiness();

            _materials.Add(m);

            return m;
        }
    }

    internal partial class MaterialPBRMetallicRoughness
    {
        internal static int GetScore(IEnumerable<string> keys)
        {
            int score = 0;
            if (keys.Contains("BaseColor")) ++score;
            if (keys.Contains("Metallic")) ++score;
            if (keys.Contains("Roughness")) ++score;
            return score;
        }

        private TextureInfo _GetBaseTexture(bool create)
        {
            if (create && _baseColorTexture == null) _baseColorTexture = new TextureInfo();
            return _baseColorTexture;
        }

        private TextureInfo _GetMetallicTexture(bool create)
        {
            if (create && _metallicRoughnessTexture == null) _metallicRoughnessTexture = new TextureInfo();
            return _metallicRoughnessTexture;
        }

        public IEnumerable<MaterialChannelView> GetChannels(Material material)
        {
            yield return new MaterialChannelView
                (
                material,
                "BaseColor",
                _GetBaseTexture,
                () => _baseColorFactor.AsValue(_baseColorFactorDefault),
                value => _baseColorFactor = value.AsNullable(_baseColorFactorDefault)
                );

            yield return new MaterialChannelView
                (
                material,
                "Metallic",
                _GetMetallicTexture,
                () => new Vector4(1, 1, 1, (float)(_metallicFactor ?? _metallicFactorDefault)),
                value => _metallicFactor = ((double)value.W).AsNullable(_metallicFactorDefault, _metallicFactorMaximum, _metallicFactorMaximum)
                );

            yield return new MaterialChannelView
                (
                material,
                "Roughness",
                null,
                () => new Vector4(1, 1, 1, (float)(_roughnessFactor ?? _roughnessFactorDefault)),
                value => _roughnessFactor = ((double)value.W).AsNullable(_roughnessFactorDefault, _roughnessFactorMinimum, _roughnessFactorMaximum)
                );
        }
    }

    internal partial class MaterialPBRSpecularGlossiness_KHR
    {
        internal static int GetScore(IEnumerable<string> keys)
        {
            int score = 0;
            if (keys.Contains("Diffuse")) ++score;
            if (keys.Contains("Glossiness")) ++score;
            if (keys.Contains("Specular")) ++score;
            return score;
        }

        private TextureInfo _GetDiffuseTexture(bool create)
        {
            if (create && _diffuseTexture == null) _diffuseTexture = new TextureInfo();
            return _diffuseTexture;
        }

        private TextureInfo _GetGlossinessTexture(bool create)
        {
            if (create && _specularGlossinessTexture == null) _specularGlossinessTexture = new TextureInfo();
            return _specularGlossinessTexture;
        }

        public IEnumerable<MaterialChannelView> GetChannels(Material material)
        {
            yield return new MaterialChannelView
                (
                material,
                "Diffuse",
                _GetDiffuseTexture,
                () => _diffuseFactor.AsValue(_diffuseFactorDefault),
                value => _diffuseFactor = value.AsNullable(_diffuseFactorDefault)
                );

            yield return new MaterialChannelView
                (
                material,
                "Glossiness",
                _GetGlossinessTexture,
                () => new Vector4(1, 1, 1, (float)_glossinessFactor.AsValue(_glossinessFactorDefault)),
                value => _glossinessFactor = ((double)value.W).AsNullable(_glossinessFactorDefault, _glossinessFactorMinimum, _glossinessFactorMaximum)
                );

            yield return new MaterialChannelView
                (
                material,
                "Specular",
                null,
                () => { var rgb = _specularFactor.AsValue(_specularFactorDefault); return new Vector4(rgb, 1); },
                value => _specularFactor = new Vector3(value.X, value.Y, value.Z).AsNullable(_specularFactorDefault)
                );
        }
    }

    internal partial class MaterialUnlit_KHR
    {
        internal static int GetScore(IEnumerable<string> keys)
        {
            return 1;
        }
    }
}
