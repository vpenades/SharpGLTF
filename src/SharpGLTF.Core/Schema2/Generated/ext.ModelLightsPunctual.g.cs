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

	partial class PunctualLightSpot : ExtraProperties
	{
	
		private const Double _innerConeAngleDefault = 0;
		private const Double _innerConeAngleMinimum = 0;
		private const Double _innerConeAngleMaximum = 1.5707963267949;
		private Double? _innerConeAngle = _innerConeAngleDefault;
		
		private const Double _outerConeAngleDefault = 0.785398163397448;
		private const Double _outerConeAngleMinimum = 0;
		private const Double _outerConeAngleMaximum = 1.5707963267949;
		private Double? _outerConeAngle = _outerConeAngleDefault;
		
	
		/// <inheritdoc />
		protected override void SerializeProperties(JsonWriter writer)
		{
			base.SerializeProperties(writer);
			SerializeProperty(writer, "innerConeAngle", _innerConeAngle, _innerConeAngleDefault);
			SerializeProperty(writer, "outerConeAngle", _outerConeAngle, _outerConeAngleDefault);
		}
	
		/// <inheritdoc />
		protected override void DeserializeProperty(string jsonPropertyName, JsonReader reader)
		{
			switch (jsonPropertyName)
			{
				case "innerConeAngle": _innerConeAngle = DeserializePropertyValue<Double?>(reader); break;
				case "outerConeAngle": _outerConeAngle = DeserializePropertyValue<Double?>(reader); break;
				default: base.DeserializeProperty(jsonPropertyName, reader); break;
			}
		}
	
	}

	/// <summary>
	/// A directional, point, or spot light.
	/// </summary>
	partial class PunctualLight : LogicalChildOfRoot
	{
	
		private static readonly Vector3 _colorDefault = Vector3.One;
		private Vector3? _color = _colorDefault;
		
		private const Double _intensityDefault = 1;
		private const Double _intensityMinimum = 0;
		private Double? _intensity = _intensityDefault;
		
		private const Double _rangeMinimum = 0;
		private Double? _range;
		
		private PunctualLightSpot _spot;
		
		private String _type;
		
	
		/// <inheritdoc />
		protected override void SerializeProperties(JsonWriter writer)
		{
			base.SerializeProperties(writer);
			SerializeProperty(writer, "color", _color, _colorDefault);
			SerializeProperty(writer, "intensity", _intensity, _intensityDefault);
			SerializeProperty(writer, "range", _range);
			SerializePropertyObject(writer, "spot", _spot);
			SerializeProperty(writer, "type", _type);
		}
	
		/// <inheritdoc />
		protected override void DeserializeProperty(string jsonPropertyName, JsonReader reader)
		{
			switch (jsonPropertyName)
			{
				case "color": _color = DeserializePropertyValue<Vector3?>(reader); break;
				case "intensity": _intensity = DeserializePropertyValue<Double?>(reader); break;
				case "range": _range = DeserializePropertyValue<Double?>(reader); break;
				case "spot": _spot = DeserializePropertyValue<PunctualLightSpot>(reader); break;
				case "type": _type = DeserializePropertyValue<String>(reader); break;
				default: base.DeserializeProperty(jsonPropertyName, reader); break;
			}
		}
	
	}

	partial class KHR_lights_punctualglTFextension : ExtraProperties
	{
	
		private const int _lightsMinItems = 1;
		private ChildrenCollection<PunctualLight,ModelRoot> _lights;
		
	
		/// <inheritdoc />
		protected override void SerializeProperties(JsonWriter writer)
		{
			base.SerializeProperties(writer);
			SerializeProperty(writer, "lights", _lights, _lightsMinItems);
		}
	
		/// <inheritdoc />
		protected override void DeserializeProperty(string jsonPropertyName, JsonReader reader)
		{
			switch (jsonPropertyName)
			{
				case "lights": DeserializePropertyList<PunctualLight>(reader, _lights); break;
				default: base.DeserializeProperty(jsonPropertyName, reader); break;
			}
		}
	
	}

}
