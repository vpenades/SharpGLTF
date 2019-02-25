//------------------------------------------------------------------------------------------------
//      This file has been programatically generated; DON´T EDIT!
//------------------------------------------------------------------------------------------------

#pragma warning disable SA1001
#pragma warning disable SA1027
#pragma warning disable SA1028
#pragma warning disable SA1121
#pragma warning disable SA1205
#pragma warning disable SA1309
#pragma warning disable SA1402
#pragma warning disable SA1505
#pragma warning disable SA1507
#pragma warning disable SA1508
#pragma warning disable SA1652

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Numerics;
using Newtonsoft.Json;

namespace SharpGLTF.Schema2
{
	using Collections;

	partial class MaterialPBRSpecularGlossiness_KHR : glTFProperty
	{
	
		private static readonly Vector4 _diffuseFactorDefault = Vector4.One;
		private Vector4? _diffuseFactor = _diffuseFactorDefault;
		
		private TextureInfo _diffuseTexture;
		
		private static readonly Vector3 _specularFactorDefault = Vector3.One;
		private Vector3? _specularFactor = _specularFactorDefault;
		
		private const Double _glossinessFactorDefault = 1;
		private const Double _glossinessFactorMinimum = 0;
		private const Double _glossinessFactorMaximum = 1;
		private Double? _glossinessFactor = _glossinessFactorDefault;
		
		private TextureInfo _specularGlossinessTexture;
		
	
		protected override void SerializeProperties(JsonWriter writer)
		{
			base.SerializeProperties(writer);
			SerializeProperty(writer, "diffuseFactor", _diffuseFactor, _diffuseFactorDefault);
			SerializePropertyObject(writer, "diffuseTexture", _diffuseTexture);
			SerializeProperty(writer, "specularFactor", _specularFactor, _specularFactorDefault);
			SerializeProperty(writer, "glossinessFactor", _glossinessFactor, _glossinessFactorDefault);
			SerializePropertyObject(writer, "specularGlossinessTexture", _specularGlossinessTexture);
		}
	
		protected override void DeserializeProperty(JsonReader reader, string property)
		{
			switch (property)
			{
				case "diffuseFactor": _diffuseFactor = DeserializeValue<Vector4?>(reader); break;
				case "diffuseTexture": _diffuseTexture = DeserializeValue<TextureInfo>(reader); break;
				case "specularFactor": _specularFactor = DeserializeValue<Vector3?>(reader); break;
				case "glossinessFactor": _glossinessFactor = DeserializeValue<Double?>(reader); break;
				case "specularGlossinessTexture": _specularGlossinessTexture = DeserializeValue<TextureInfo>(reader); break;
				default: base.DeserializeProperty(reader, property); break;
			}
		}
	
	}

}
