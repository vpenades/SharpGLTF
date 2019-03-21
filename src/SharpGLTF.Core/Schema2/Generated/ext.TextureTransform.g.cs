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

	/// <summary>
	/// glTF extension that enables shifting and scaling UV coordinates on a per-texture basis
	/// </summary>
	partial class TextureTransform : ExtraProperties
	{
	
		private static readonly Vector2 _offsetDefault = Vector2.One;
		private Vector2? _offset = _offsetDefault;
		
		private const Double _rotationDefault = 0;
		private Double? _rotation = _rotationDefault;
		
		private static readonly Vector2 _scaleDefault = Vector2.One;
		private Vector2? _scale = _scaleDefault;
		
		private const Int32 _texCoordMinimum = 0;
		private Int32? _texCoord;
		
	
		/// <inheritdoc />
		protected override void SerializeProperties(JsonWriter writer)
		{
			base.SerializeProperties(writer);
			SerializeProperty(writer, "offset", _offset, _offsetDefault);
			SerializeProperty(writer, "rotation", _rotation, _rotationDefault);
			SerializeProperty(writer, "scale", _scale, _scaleDefault);
			SerializeProperty(writer, "texCoord", _texCoord);
		}
	
		/// <inheritdoc />
		protected override void DeserializeProperty(string property, JsonReader reader)
		{
			switch (property)
			{
				case "offset": _offset = DeserializePropertyValue<Vector2?>(reader); break;
				case "rotation": _rotation = DeserializePropertyValue<Double?>(reader); break;
				case "scale": _scale = DeserializePropertyValue<Vector2?>(reader); break;
				case "texCoord": _texCoord = DeserializePropertyValue<Int32?>(reader); break;
				default: base.DeserializeProperty(property, reader); break;
			}
		}
	
	}

}
