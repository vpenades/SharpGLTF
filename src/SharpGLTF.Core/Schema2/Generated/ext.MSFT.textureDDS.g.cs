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
	/// glTF extension to specify textures using the DirectDraw Surface file format (DDS).
	/// </summary>
	partial class MSFTTextureDDS : ExtraProperties
	{
	
		private Int32? _source;
		
	
		/// <inheritdoc />
		protected override void SerializeProperties(JsonWriter writer)
		{
			base.SerializeProperties(writer);
			SerializeProperty(writer, "source", _source);
		}
	
		/// <inheritdoc />
		protected override void DeserializeProperty(string property, JsonReader reader)
		{
			switch (property)
			{
				case "source": _source = DeserializePropertyValue<Int32?>(reader); break;
				default: base.DeserializeProperty(property, reader); break;
			}
		}
	
	}

}
