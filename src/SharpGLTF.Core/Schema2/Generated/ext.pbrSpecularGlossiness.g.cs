// <auto-generated/>

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
using System.Text.Json;

namespace SharpGLTF.Schema2
{
	using Collections;

	/// <summary>
	/// glTF extension that defines the specular-glossiness material model from Physically-Based Rendering (PBR) methodology.
	/// </summary>
	#if NET6_0_OR_GREATER
	[System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembers(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.NonPublicConstructors | System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicConstructors)]
	#endif
	[global::System.CodeDom.Compiler.GeneratedCodeAttribute("SharpGLTF.CodeGen", "1.0.0.0")]
	partial class MaterialPBRSpecularGlossiness : ExtraProperties
	{
	
		private static readonly Vector4 _diffuseFactorDefault = Vector4.One;
		private Vector4? _diffuseFactor = _diffuseFactorDefault;
		
		private TextureInfo _diffuseTexture;
		
		private const Double _glossinessFactorDefault = 1;
		private const Double _glossinessFactorMinimum = 0;
		private const Double _glossinessFactorMaximum = 1;
		private Double? _glossinessFactor = _glossinessFactorDefault;
		
		private static readonly Vector3 _specularFactorDefault = Vector3.One;
		private Vector3? _specularFactor = _specularFactorDefault;
		
		private TextureInfo _specularGlossinessTexture;
		
	
		protected override void SerializeProperties(Utf8JsonWriter writer)
		{
			base.SerializeProperties(writer);
			SerializeProperty(writer, "diffuseFactor", _diffuseFactor, _diffuseFactorDefault);
			SerializePropertyObject(writer, "diffuseTexture", _diffuseTexture);
			SerializeProperty(writer, "glossinessFactor", _glossinessFactor, _glossinessFactorDefault);
			SerializeProperty(writer, "specularFactor", _specularFactor, _specularFactorDefault);
			SerializePropertyObject(writer, "specularGlossinessTexture", _specularGlossinessTexture);
		}
	
		protected override void DeserializeProperty(string jsonPropertyName, ref Utf8JsonReader reader)
		{
			switch (jsonPropertyName)
			{
				case "diffuseFactor": DeserializePropertyValue<MaterialPBRSpecularGlossiness, Vector4?>(ref reader, this, out _diffuseFactor); break;
				case "diffuseTexture": DeserializePropertyValue<MaterialPBRSpecularGlossiness, TextureInfo>(ref reader, this, out _diffuseTexture); break;
				case "glossinessFactor": DeserializePropertyValue<MaterialPBRSpecularGlossiness, Double?>(ref reader, this, out _glossinessFactor); break;
				case "specularFactor": DeserializePropertyValue<MaterialPBRSpecularGlossiness, Vector3?>(ref reader, this, out _specularFactor); break;
				case "specularGlossinessTexture": DeserializePropertyValue<MaterialPBRSpecularGlossiness, TextureInfo>(ref reader, this, out _specularGlossinessTexture); break;
				default: base.DeserializeProperty(jsonPropertyName,ref reader); break;
			}
		}
	
	}

}
