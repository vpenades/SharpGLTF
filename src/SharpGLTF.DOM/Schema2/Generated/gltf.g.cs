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

	public enum IndexType
	{
		UNSIGNED_BYTE = 5121,
		UNSIGNED_SHORT = 5123,
		UNSIGNED_INT = 5125,
	}


	public enum ComponentType
	{
		BYTE = 5120,
		UNSIGNED_BYTE = 5121,
		SHORT = 5122,
		UNSIGNED_SHORT = 5123,
		UNSIGNED_INT = 5125,
		FLOAT = 5126,
	}


	public enum ElementType
	{
		SCALAR,
		VEC2,
		VEC3,
		VEC4,
		MAT2,
		MAT3,
		MAT4,
	}


	public enum PathType
	{
		translation,
		rotation,
		scale,
		weights,
	}


	public enum AnimationInterpolationMode
	{
		LINEAR,
		STEP,
		CUBICSPLINE,
	}


	public enum BufferMode
	{
		ARRAY_BUFFER = 34962,
		ELEMENT_ARRAY_BUFFER = 34963,
	}


	public enum CameraType
	{
		perspective,
		orthographic,
	}


	public enum AlphaMode
	{
		OPAQUE,
		MASK,
		BLEND,
	}


	public enum PrimitiveType
	{
		POINTS = 0,
		LINES = 1,
		LINE_LOOP = 2,
		LINE_STRIP = 3,
		TRIANGLES = 4,
		TRIANGLE_STRIP = 5,
		TRIANGLE_FAN = 6,
	}


	public enum TextureInterpolationMode
	{
		NEAREST = 9728,
		LINEAR = 9729,
	}


	public enum TextureMipMapMode
	{
		NEAREST = 9728,
		LINEAR = 9729,
		NEAREST_MIPMAP_NEAREST = 9984,
		LINEAR_MIPMAP_NEAREST = 9985,
		NEAREST_MIPMAP_LINEAR = 9986,
		LINEAR_MIPMAP_LINEAR = 9987,
	}


	public enum TextureWrapMode
	{
		CLAMP_TO_EDGE = 33071,
		MIRRORED_REPEAT = 33648,
		REPEAT = 10497,
	}


	partial class LogicalChildOfRoot : glTFProperty
	{
	
		private String _name;
		
	
		protected override void SerializeProperties(JsonWriter writer)
		{
			base.SerializeProperties(writer);
			SerializeProperty(writer, "name", _name);
		}
	
		protected override void DeserializeProperty(JsonReader reader, string property)
		{
			switch (property)
			{
				case "name": _name = DeserializeValue<String>(reader); break;
				default: base.DeserializeProperty(reader, property); break;
			}
		}
	
	}

	partial class AccessorSparseIndices : glTFProperty
	{
	
		private Int32 _bufferView;
		
		private const Int32 _byteOffsetDefault = 0;
		private const Int32 _byteOffsetMinimum = 0;
		private Int32? _byteOffset = _byteOffsetDefault;
		
		private IndexType _componentType;
		
	
		protected override void SerializeProperties(JsonWriter writer)
		{
			base.SerializeProperties(writer);
			SerializeProperty(writer, "bufferView", _bufferView);
			SerializeProperty(writer, "byteOffset", _byteOffset, _byteOffsetDefault);
			SerializePropertyEnumValue<IndexType>(writer, "componentType", _componentType);
		}
	
		protected override void DeserializeProperty(JsonReader reader, string property)
		{
			switch (property)
			{
				case "bufferView": _bufferView = DeserializeValue<Int32>(reader); break;
				case "byteOffset": _byteOffset = DeserializeValue<Int32?>(reader); break;
				case "componentType": _componentType = DeserializeValue<IndexType>(reader); break;
				default: base.DeserializeProperty(reader, property); break;
			}
		}
	
	}

	partial class AccessorSparseValues : glTFProperty
	{
	
		private Int32 _bufferView;
		
		private const Int32 _byteOffsetDefault = 0;
		private const Int32 _byteOffsetMinimum = 0;
		private Int32? _byteOffset = _byteOffsetDefault;
		
	
		protected override void SerializeProperties(JsonWriter writer)
		{
			base.SerializeProperties(writer);
			SerializeProperty(writer, "bufferView", _bufferView);
			SerializeProperty(writer, "byteOffset", _byteOffset, _byteOffsetDefault);
		}
	
		protected override void DeserializeProperty(JsonReader reader, string property)
		{
			switch (property)
			{
				case "bufferView": _bufferView = DeserializeValue<Int32>(reader); break;
				case "byteOffset": _byteOffset = DeserializeValue<Int32?>(reader); break;
				default: base.DeserializeProperty(reader, property); break;
			}
		}
	
	}

	partial class AccessorSparse : glTFProperty
	{
	
		private const Int32 _countMinimum = 1;
		private Int32 _count;
		
		private AccessorSparseIndices _indices;
		
		private AccessorSparseValues _values;
		
	
		protected override void SerializeProperties(JsonWriter writer)
		{
			base.SerializeProperties(writer);
			SerializeProperty(writer, "count", _count);
			SerializePropertyObject(writer, "indices", _indices);
			SerializePropertyObject(writer, "values", _values);
		}
	
		protected override void DeserializeProperty(JsonReader reader, string property)
		{
			switch (property)
			{
				case "count": _count = DeserializeValue<Int32>(reader); break;
				case "indices": _indices = DeserializeValue<AccessorSparseIndices>(reader); break;
				case "values": _values = DeserializeValue<AccessorSparseValues>(reader); break;
				default: base.DeserializeProperty(reader, property); break;
			}
		}
	
	}

	partial class Accessor : LogicalChildOfRoot
	{
	
		private Int32? _bufferView;
		
		private const Int32 _byteOffsetDefault = 0;
		private const Int32 _byteOffsetMinimum = 0;
		private Int32? _byteOffset = _byteOffsetDefault;
		
		private ComponentType _componentType;
		
		private static readonly Boolean _normalizedDefault = false;
		private Boolean? _normalized = _normalizedDefault;
		
		private const Int32 _countMinimum = 1;
		private Int32 _count;
		
		private ElementType _type;
		
		private const int _maxMinItems = 1;
		private const int _maxMaxItems = 16;
		private List<Double> _max;
		
		private const int _minMinItems = 1;
		private const int _minMaxItems = 16;
		private List<Double> _min;
		
		private AccessorSparse _sparse;
		
	
		protected override void SerializeProperties(JsonWriter writer)
		{
			base.SerializeProperties(writer);
			SerializeProperty(writer, "bufferView", _bufferView);
			SerializeProperty(writer, "byteOffset", _byteOffset, _byteOffsetDefault);
			SerializePropertyEnumValue<ComponentType>(writer, "componentType", _componentType);
			SerializeProperty(writer, "normalized", _normalized, _normalizedDefault);
			SerializeProperty(writer, "count", _count);
			SerializePropertyEnumSymbol<ElementType>(writer, "type", _type);
			SerializeProperty(writer, "max", _max, _maxMinItems);
			SerializeProperty(writer, "min", _min, _minMinItems);
			SerializePropertyObject(writer, "sparse", _sparse);
		}
	
		protected override void DeserializeProperty(JsonReader reader, string property)
		{
			switch (property)
			{
				case "bufferView": _bufferView = DeserializeValue<Int32?>(reader); break;
				case "byteOffset": _byteOffset = DeserializeValue<Int32?>(reader); break;
				case "componentType": _componentType = DeserializeValue<ComponentType>(reader); break;
				case "normalized": _normalized = DeserializeValue<Boolean?>(reader); break;
				case "count": _count = DeserializeValue<Int32>(reader); break;
				case "type": _type = DeserializeValue<ElementType>(reader); break;
				case "max": DeserializeList<Double>(reader, _max); break;
				case "min": DeserializeList<Double>(reader, _min); break;
				case "sparse": _sparse = DeserializeValue<AccessorSparse>(reader); break;
				default: base.DeserializeProperty(reader, property); break;
			}
		}
	
	}

	partial class AnimationChannelTarget : glTFProperty
	{
	
		private Int32? _node;
		
		private PathType _path;
		
	
		protected override void SerializeProperties(JsonWriter writer)
		{
			base.SerializeProperties(writer);
			SerializeProperty(writer, "node", _node);
			SerializePropertyEnumSymbol<PathType>(writer, "path", _path);
		}
	
		protected override void DeserializeProperty(JsonReader reader, string property)
		{
			switch (property)
			{
				case "node": _node = DeserializeValue<Int32?>(reader); break;
				case "path": _path = DeserializeValue<PathType>(reader); break;
				default: base.DeserializeProperty(reader, property); break;
			}
		}
	
	}

	partial class AnimationChannel : glTFProperty
	{
	
		private Int32 _sampler;
		
		private AnimationChannelTarget _target;
		
	
		protected override void SerializeProperties(JsonWriter writer)
		{
			base.SerializeProperties(writer);
			SerializeProperty(writer, "sampler", _sampler);
			SerializePropertyObject(writer, "target", _target);
		}
	
		protected override void DeserializeProperty(JsonReader reader, string property)
		{
			switch (property)
			{
				case "sampler": _sampler = DeserializeValue<Int32>(reader); break;
				case "target": _target = DeserializeValue<AnimationChannelTarget>(reader); break;
				default: base.DeserializeProperty(reader, property); break;
			}
		}
	
	}

	partial class AnimationSampler : glTFProperty
	{
	
		private Int32 _input;
		
		private const AnimationInterpolationMode _interpolationDefault = AnimationInterpolationMode.LINEAR;
		private AnimationInterpolationMode? _interpolation = _interpolationDefault;
		
		private Int32 _output;
		
	
		protected override void SerializeProperties(JsonWriter writer)
		{
			base.SerializeProperties(writer);
			SerializeProperty(writer, "input", _input);
			SerializePropertyEnumSymbol<AnimationInterpolationMode>(writer, "interpolation", _interpolation, _interpolationDefault);
			SerializeProperty(writer, "output", _output);
		}
	
		protected override void DeserializeProperty(JsonReader reader, string property)
		{
			switch (property)
			{
				case "input": _input = DeserializeValue<Int32>(reader); break;
				case "interpolation": _interpolation = DeserializeValue<AnimationInterpolationMode>(reader); break;
				case "output": _output = DeserializeValue<Int32>(reader); break;
				default: base.DeserializeProperty(reader, property); break;
			}
		}
	
	}

	partial class Animation : LogicalChildOfRoot
	{
	
		private const int _channelsMinItems = 1;
		private ChildrenCollection<AnimationChannel,Animation> _channels;
		
		private const int _samplersMinItems = 1;
		private ChildrenCollection<AnimationSampler,Animation> _samplers;
		
	
		protected override void SerializeProperties(JsonWriter writer)
		{
			base.SerializeProperties(writer);
			SerializeProperty(writer, "channels", _channels, _channelsMinItems);
			SerializeProperty(writer, "samplers", _samplers, _samplersMinItems);
		}
	
		protected override void DeserializeProperty(JsonReader reader, string property)
		{
			switch (property)
			{
				case "channels": DeserializeList<AnimationChannel>(reader, _channels); break;
				case "samplers": DeserializeList<AnimationSampler>(reader, _samplers); break;
				default: base.DeserializeProperty(reader, property); break;
			}
		}
	
	}

	partial class Asset : glTFProperty
	{
	
		private String _copyright;
		
		private String _generator;
		
		private String _version;
		
		private String _minVersion;
		
	
		protected override void SerializeProperties(JsonWriter writer)
		{
			base.SerializeProperties(writer);
			SerializeProperty(writer, "copyright", _copyright);
			SerializeProperty(writer, "generator", _generator);
			SerializeProperty(writer, "version", _version);
			SerializeProperty(writer, "minVersion", _minVersion);
		}
	
		protected override void DeserializeProperty(JsonReader reader, string property)
		{
			switch (property)
			{
				case "copyright": _copyright = DeserializeValue<String>(reader); break;
				case "generator": _generator = DeserializeValue<String>(reader); break;
				case "version": _version = DeserializeValue<String>(reader); break;
				case "minVersion": _minVersion = DeserializeValue<String>(reader); break;
				default: base.DeserializeProperty(reader, property); break;
			}
		}
	
	}

	partial class Buffer : LogicalChildOfRoot
	{
	
		private String _uri;
		
		private const Int32 _byteLengthMinimum = 1;
		private Int32 _byteLength;
		
	
		protected override void SerializeProperties(JsonWriter writer)
		{
			base.SerializeProperties(writer);
			SerializeProperty(writer, "uri", _uri);
			SerializeProperty(writer, "byteLength", _byteLength);
		}
	
		protected override void DeserializeProperty(JsonReader reader, string property)
		{
			switch (property)
			{
				case "uri": _uri = DeserializeValue<String>(reader); break;
				case "byteLength": _byteLength = DeserializeValue<Int32>(reader); break;
				default: base.DeserializeProperty(reader, property); break;
			}
		}
	
	}

	partial class BufferView : LogicalChildOfRoot
	{
	
		private Int32 _buffer;
		
		private const Int32 _byteOffsetDefault = 0;
		private const Int32 _byteOffsetMinimum = 0;
		private Int32? _byteOffset = _byteOffsetDefault;
		
		private const Int32 _byteLengthMinimum = 1;
		private Int32 _byteLength;
		
		private const Int32 _byteStrideMinimum = 4;
		private const Int32 _byteStrideMaximum = 252;
		private Int32? _byteStride;
		
		private BufferMode? _target;
		
	
		protected override void SerializeProperties(JsonWriter writer)
		{
			base.SerializeProperties(writer);
			SerializeProperty(writer, "buffer", _buffer);
			SerializeProperty(writer, "byteOffset", _byteOffset, _byteOffsetDefault);
			SerializeProperty(writer, "byteLength", _byteLength);
			SerializeProperty(writer, "byteStride", _byteStride);
			SerializePropertyEnumValue<BufferMode>(writer, "target", _target);
		}
	
		protected override void DeserializeProperty(JsonReader reader, string property)
		{
			switch (property)
			{
				case "buffer": _buffer = DeserializeValue<Int32>(reader); break;
				case "byteOffset": _byteOffset = DeserializeValue<Int32?>(reader); break;
				case "byteLength": _byteLength = DeserializeValue<Int32>(reader); break;
				case "byteStride": _byteStride = DeserializeValue<Int32?>(reader); break;
				case "target": _target = DeserializeValue<BufferMode>(reader); break;
				default: base.DeserializeProperty(reader, property); break;
			}
		}
	
	}

	partial class CameraOrthographic : glTFProperty
	{
	
		private Double _xmag;
		
		private Double _ymag;
		
		private const Double _zfarMinimum = 0;
		private Double _zfar;
		
		private const Double _znearMinimum = 0;
		private Double _znear;
		
	
		protected override void SerializeProperties(JsonWriter writer)
		{
			base.SerializeProperties(writer);
			SerializeProperty(writer, "xmag", _xmag);
			SerializeProperty(writer, "ymag", _ymag);
			SerializeProperty(writer, "zfar", _zfar);
			SerializeProperty(writer, "znear", _znear);
		}
	
		protected override void DeserializeProperty(JsonReader reader, string property)
		{
			switch (property)
			{
				case "xmag": _xmag = DeserializeValue<Double>(reader); break;
				case "ymag": _ymag = DeserializeValue<Double>(reader); break;
				case "zfar": _zfar = DeserializeValue<Double>(reader); break;
				case "znear": _znear = DeserializeValue<Double>(reader); break;
				default: base.DeserializeProperty(reader, property); break;
			}
		}
	
	}

	partial class CameraPerspective : glTFProperty
	{
	
		private const Double _aspectRatioMinimum = 0;
		private Double? _aspectRatio;
		
		private const Double _yfovMinimum = 0;
		private Double _yfov;
		
		private const Double _zfarMinimum = 0;
		private Double? _zfar;
		
		private const Double _znearMinimum = 0;
		private Double _znear;
		
	
		protected override void SerializeProperties(JsonWriter writer)
		{
			base.SerializeProperties(writer);
			SerializeProperty(writer, "aspectRatio", _aspectRatio);
			SerializeProperty(writer, "yfov", _yfov);
			SerializeProperty(writer, "zfar", _zfar);
			SerializeProperty(writer, "znear", _znear);
		}
	
		protected override void DeserializeProperty(JsonReader reader, string property)
		{
			switch (property)
			{
				case "aspectRatio": _aspectRatio = DeserializeValue<Double?>(reader); break;
				case "yfov": _yfov = DeserializeValue<Double>(reader); break;
				case "zfar": _zfar = DeserializeValue<Double?>(reader); break;
				case "znear": _znear = DeserializeValue<Double>(reader); break;
				default: base.DeserializeProperty(reader, property); break;
			}
		}
	
	}

	partial class Camera : LogicalChildOfRoot
	{
	
		private CameraOrthographic _orthographic;
		
		private CameraPerspective _perspective;
		
		private CameraType _type;
		
	
		protected override void SerializeProperties(JsonWriter writer)
		{
			base.SerializeProperties(writer);
			SerializePropertyObject(writer, "orthographic", _orthographic);
			SerializePropertyObject(writer, "perspective", _perspective);
			SerializePropertyEnumSymbol<CameraType>(writer, "type", _type);
		}
	
		protected override void DeserializeProperty(JsonReader reader, string property)
		{
			switch (property)
			{
				case "orthographic": _orthographic = DeserializeValue<CameraOrthographic>(reader); break;
				case "perspective": _perspective = DeserializeValue<CameraPerspective>(reader); break;
				case "type": _type = DeserializeValue<CameraType>(reader); break;
				default: base.DeserializeProperty(reader, property); break;
			}
		}
	
	}

	partial class TextureInfo : glTFProperty
	{
	
		private Int32 _index;
		
		private const Int32 _texCoordDefault = 0;
		private const Int32 _texCoordMinimum = 0;
		private Int32? _texCoord = _texCoordDefault;
		
	
		protected override void SerializeProperties(JsonWriter writer)
		{
			base.SerializeProperties(writer);
			SerializeProperty(writer, "index", _index);
			SerializeProperty(writer, "texCoord", _texCoord, _texCoordDefault);
		}
	
		protected override void DeserializeProperty(JsonReader reader, string property)
		{
			switch (property)
			{
				case "index": _index = DeserializeValue<Int32>(reader); break;
				case "texCoord": _texCoord = DeserializeValue<Int32?>(reader); break;
				default: base.DeserializeProperty(reader, property); break;
			}
		}
	
	}

	partial class MaterialPBRMetallicRoughness : glTFProperty
	{
	
		private static readonly Vector4 _baseColorFactorDefault = Vector4.One;
		private Vector4? _baseColorFactor = _baseColorFactorDefault;
		
		private TextureInfo _baseColorTexture;
		
		private const Double _metallicFactorDefault = 1;
		private const Double _metallicFactorMinimum = 0;
		private const Double _metallicFactorMaximum = 1;
		private Double? _metallicFactor = _metallicFactorDefault;
		
		private const Double _roughnessFactorDefault = 1;
		private const Double _roughnessFactorMinimum = 0;
		private const Double _roughnessFactorMaximum = 1;
		private Double? _roughnessFactor = _roughnessFactorDefault;
		
		private TextureInfo _metallicRoughnessTexture;
		
	
		protected override void SerializeProperties(JsonWriter writer)
		{
			base.SerializeProperties(writer);
			SerializeProperty(writer, "baseColorFactor", _baseColorFactor, _baseColorFactorDefault);
			SerializePropertyObject(writer, "baseColorTexture", _baseColorTexture);
			SerializeProperty(writer, "metallicFactor", _metallicFactor, _metallicFactorDefault);
			SerializeProperty(writer, "roughnessFactor", _roughnessFactor, _roughnessFactorDefault);
			SerializePropertyObject(writer, "metallicRoughnessTexture", _metallicRoughnessTexture);
		}
	
		protected override void DeserializeProperty(JsonReader reader, string property)
		{
			switch (property)
			{
				case "baseColorFactor": _baseColorFactor = DeserializeValue<Vector4?>(reader); break;
				case "baseColorTexture": _baseColorTexture = DeserializeValue<TextureInfo>(reader); break;
				case "metallicFactor": _metallicFactor = DeserializeValue<Double?>(reader); break;
				case "roughnessFactor": _roughnessFactor = DeserializeValue<Double?>(reader); break;
				case "metallicRoughnessTexture": _metallicRoughnessTexture = DeserializeValue<TextureInfo>(reader); break;
				default: base.DeserializeProperty(reader, property); break;
			}
		}
	
	}

	partial class MaterialNormalTextureInfo : TextureInfo
	{
	
		private const Double _scaleDefault = 1;
		private Double? _scale = _scaleDefault;
		
	
		protected override void SerializeProperties(JsonWriter writer)
		{
			base.SerializeProperties(writer);
			SerializeProperty(writer, "scale", _scale, _scaleDefault);
		}
	
		protected override void DeserializeProperty(JsonReader reader, string property)
		{
			switch (property)
			{
				case "scale": _scale = DeserializeValue<Double?>(reader); break;
				default: base.DeserializeProperty(reader, property); break;
			}
		}
	
	}

	partial class MaterialOcclusionTextureInfo : TextureInfo
	{
	
		private const Double _strengthDefault = 1;
		private const Double _strengthMinimum = 0;
		private const Double _strengthMaximum = 1;
		private Double? _strength = _strengthDefault;
		
	
		protected override void SerializeProperties(JsonWriter writer)
		{
			base.SerializeProperties(writer);
			SerializeProperty(writer, "strength", _strength, _strengthDefault);
		}
	
		protected override void DeserializeProperty(JsonReader reader, string property)
		{
			switch (property)
			{
				case "strength": _strength = DeserializeValue<Double?>(reader); break;
				default: base.DeserializeProperty(reader, property); break;
			}
		}
	
	}

	partial class Material : LogicalChildOfRoot
	{
	
		private MaterialPBRMetallicRoughness _pbrMetallicRoughness;
		
		private MaterialNormalTextureInfo _normalTexture;
		
		private MaterialOcclusionTextureInfo _occlusionTexture;
		
		private TextureInfo _emissiveTexture;
		
		private static readonly Vector3 _emissiveFactorDefault = Vector3.Zero;
		private Vector3? _emissiveFactor = _emissiveFactorDefault;
		
		private const AlphaMode _alphaModeDefault = AlphaMode.OPAQUE;
		private AlphaMode? _alphaMode = _alphaModeDefault;
		
		private const Double _alphaCutoffDefault = 0.5;
		private const Double _alphaCutoffMinimum = 0;
		private Double? _alphaCutoff = _alphaCutoffDefault;
		
		private static readonly Boolean _doubleSidedDefault = false;
		private Boolean? _doubleSided = _doubleSidedDefault;
		
	
		protected override void SerializeProperties(JsonWriter writer)
		{
			base.SerializeProperties(writer);
			SerializePropertyObject(writer, "pbrMetallicRoughness", _pbrMetallicRoughness);
			SerializePropertyObject(writer, "normalTexture", _normalTexture);
			SerializePropertyObject(writer, "occlusionTexture", _occlusionTexture);
			SerializePropertyObject(writer, "emissiveTexture", _emissiveTexture);
			SerializeProperty(writer, "emissiveFactor", _emissiveFactor, _emissiveFactorDefault);
			SerializePropertyEnumSymbol<AlphaMode>(writer, "alphaMode", _alphaMode, _alphaModeDefault);
			SerializeProperty(writer, "alphaCutoff", _alphaCutoff, _alphaCutoffDefault);
			SerializeProperty(writer, "doubleSided", _doubleSided, _doubleSidedDefault);
		}
	
		protected override void DeserializeProperty(JsonReader reader, string property)
		{
			switch (property)
			{
				case "pbrMetallicRoughness": _pbrMetallicRoughness = DeserializeValue<MaterialPBRMetallicRoughness>(reader); break;
				case "normalTexture": _normalTexture = DeserializeValue<MaterialNormalTextureInfo>(reader); break;
				case "occlusionTexture": _occlusionTexture = DeserializeValue<MaterialOcclusionTextureInfo>(reader); break;
				case "emissiveTexture": _emissiveTexture = DeserializeValue<TextureInfo>(reader); break;
				case "emissiveFactor": _emissiveFactor = DeserializeValue<Vector3?>(reader); break;
				case "alphaMode": _alphaMode = DeserializeValue<AlphaMode>(reader); break;
				case "alphaCutoff": _alphaCutoff = DeserializeValue<Double?>(reader); break;
				case "doubleSided": _doubleSided = DeserializeValue<Boolean?>(reader); break;
				default: base.DeserializeProperty(reader, property); break;
			}
		}
	
	}

	partial class MeshPrimitive : glTFProperty
	{
	
		private Dictionary<String,Int32> _attributes;
		
		private Int32? _indices;
		
		private Int32? _material;
		
		private const PrimitiveType _modeDefault = (PrimitiveType)4;
		private PrimitiveType? _mode = _modeDefault;
		
		private const int _targetsMinItems = 1;
		private List<Dictionary<String,Int32>> _targets;
		
	
		protected override void SerializeProperties(JsonWriter writer)
		{
			base.SerializeProperties(writer);
			SerializeProperty(writer, "attributes", _attributes);
			SerializeProperty(writer, "indices", _indices);
			SerializeProperty(writer, "material", _material);
			SerializePropertyEnumValue<PrimitiveType>(writer, "mode", _mode, _modeDefault);
			SerializeProperty(writer, "targets", _targets, _targetsMinItems);
		}
	
		protected override void DeserializeProperty(JsonReader reader, string property)
		{
			switch (property)
			{
				case "attributes": DeserializeDictionary<Int32>(reader, _attributes); break;
				case "indices": _indices = DeserializeValue<Int32?>(reader); break;
				case "material": _material = DeserializeValue<Int32?>(reader); break;
				case "mode": _mode = DeserializeValue<PrimitiveType>(reader); break;
				case "targets": DeserializeList<Dictionary<String,Int32>>(reader, _targets); break;
				default: base.DeserializeProperty(reader, property); break;
			}
		}
	
	}

	partial class Mesh : LogicalChildOfRoot
	{
	
		private const int _primitivesMinItems = 1;
		private ChildrenCollection<MeshPrimitive,Mesh> _primitives;
		
		private const int _weightsMinItems = 1;
		private List<Double> _weights;
		
	
		protected override void SerializeProperties(JsonWriter writer)
		{
			base.SerializeProperties(writer);
			SerializeProperty(writer, "primitives", _primitives, _primitivesMinItems);
			SerializeProperty(writer, "weights", _weights, _weightsMinItems);
		}
	
		protected override void DeserializeProperty(JsonReader reader, string property)
		{
			switch (property)
			{
				case "primitives": DeserializeList<MeshPrimitive>(reader, _primitives); break;
				case "weights": DeserializeList<Double>(reader, _weights); break;
				default: base.DeserializeProperty(reader, property); break;
			}
		}
	
	}

	partial class Node : LogicalChildOfRoot
	{
	
		private Int32? _camera;
		
		private const int _childrenMinItems = 1;
		private List<Int32> _children;
		
		private Int32? _skin;
		
		private Matrix4x4? _matrix;
		
		private Int32? _mesh;
		
		private Quaternion? _rotation;
		
		private Vector3? _scale;
		
		private Vector3? _translation;
		
		private const int _weightsMinItems = 1;
		private List<Double> _weights;
		
	
		protected override void SerializeProperties(JsonWriter writer)
		{
			base.SerializeProperties(writer);
			SerializeProperty(writer, "camera", _camera);
			SerializeProperty(writer, "children", _children, _childrenMinItems);
			SerializeProperty(writer, "skin", _skin);
			SerializeProperty(writer, "matrix", _matrix);
			SerializeProperty(writer, "mesh", _mesh);
			SerializeProperty(writer, "rotation", _rotation);
			SerializeProperty(writer, "scale", _scale);
			SerializeProperty(writer, "translation", _translation);
			SerializeProperty(writer, "weights", _weights, _weightsMinItems);
		}
	
		protected override void DeserializeProperty(JsonReader reader, string property)
		{
			switch (property)
			{
				case "camera": _camera = DeserializeValue<Int32?>(reader); break;
				case "children": DeserializeList<Int32>(reader, _children); break;
				case "skin": _skin = DeserializeValue<Int32?>(reader); break;
				case "matrix": _matrix = DeserializeValue<Matrix4x4?>(reader); break;
				case "mesh": _mesh = DeserializeValue<Int32?>(reader); break;
				case "rotation": _rotation = DeserializeValue<Quaternion?>(reader); break;
				case "scale": _scale = DeserializeValue<Vector3?>(reader); break;
				case "translation": _translation = DeserializeValue<Vector3?>(reader); break;
				case "weights": DeserializeList<Double>(reader, _weights); break;
				default: base.DeserializeProperty(reader, property); break;
			}
		}
	
	}

	partial class Sampler : LogicalChildOfRoot
	{
	
		private TextureInterpolationMode? _magFilter;
		
		private TextureMipMapMode? _minFilter;
		
		private const TextureWrapMode _wrapSDefault = (TextureWrapMode)10497;
		private TextureWrapMode? _wrapS = _wrapSDefault;
		
		private const TextureWrapMode _wrapTDefault = (TextureWrapMode)10497;
		private TextureWrapMode? _wrapT = _wrapTDefault;
		
	
		protected override void SerializeProperties(JsonWriter writer)
		{
			base.SerializeProperties(writer);
			SerializePropertyEnumValue<TextureInterpolationMode>(writer, "magFilter", _magFilter);
			SerializePropertyEnumValue<TextureMipMapMode>(writer, "minFilter", _minFilter);
			SerializePropertyEnumValue<TextureWrapMode>(writer, "wrapS", _wrapS, _wrapSDefault);
			SerializePropertyEnumValue<TextureWrapMode>(writer, "wrapT", _wrapT, _wrapTDefault);
		}
	
		protected override void DeserializeProperty(JsonReader reader, string property)
		{
			switch (property)
			{
				case "magFilter": _magFilter = DeserializeValue<TextureInterpolationMode>(reader); break;
				case "minFilter": _minFilter = DeserializeValue<TextureMipMapMode>(reader); break;
				case "wrapS": _wrapS = DeserializeValue<TextureWrapMode>(reader); break;
				case "wrapT": _wrapT = DeserializeValue<TextureWrapMode>(reader); break;
				default: base.DeserializeProperty(reader, property); break;
			}
		}
	
	}

	partial class Scene : LogicalChildOfRoot
	{
	
		private const int _nodesMinItems = 1;
		private List<Int32> _nodes;
		
	
		protected override void SerializeProperties(JsonWriter writer)
		{
			base.SerializeProperties(writer);
			SerializeProperty(writer, "nodes", _nodes, _nodesMinItems);
		}
	
		protected override void DeserializeProperty(JsonReader reader, string property)
		{
			switch (property)
			{
				case "nodes": DeserializeList<Int32>(reader, _nodes); break;
				default: base.DeserializeProperty(reader, property); break;
			}
		}
	
	}

	partial class Skin : LogicalChildOfRoot
	{
	
		private Int32? _inverseBindMatrices;
		
		private Int32? _skeleton;
		
		private const int _jointsMinItems = 1;
		private List<Int32> _joints;
		
	
		protected override void SerializeProperties(JsonWriter writer)
		{
			base.SerializeProperties(writer);
			SerializeProperty(writer, "inverseBindMatrices", _inverseBindMatrices);
			SerializeProperty(writer, "skeleton", _skeleton);
			SerializeProperty(writer, "joints", _joints, _jointsMinItems);
		}
	
		protected override void DeserializeProperty(JsonReader reader, string property)
		{
			switch (property)
			{
				case "inverseBindMatrices": _inverseBindMatrices = DeserializeValue<Int32?>(reader); break;
				case "skeleton": _skeleton = DeserializeValue<Int32?>(reader); break;
				case "joints": DeserializeList<Int32>(reader, _joints); break;
				default: base.DeserializeProperty(reader, property); break;
			}
		}
	
	}

	partial class Texture : LogicalChildOfRoot
	{
	
		private Int32? _sampler;
		
		private Int32? _source;
		
	
		protected override void SerializeProperties(JsonWriter writer)
		{
			base.SerializeProperties(writer);
			SerializeProperty(writer, "sampler", _sampler);
			SerializeProperty(writer, "source", _source);
		}
	
		protected override void DeserializeProperty(JsonReader reader, string property)
		{
			switch (property)
			{
				case "sampler": _sampler = DeserializeValue<Int32?>(reader); break;
				case "source": _source = DeserializeValue<Int32?>(reader); break;
				default: base.DeserializeProperty(reader, property); break;
			}
		}
	
	}

	partial class ModelRoot : glTFProperty
	{
	
		private const int _extensionsUsedMinItems = 1;
		private List<String> _extensionsUsed;
		
		private const int _extensionsRequiredMinItems = 1;
		private List<String> _extensionsRequired;
		
		private const int _accessorsMinItems = 1;
		private ChildrenCollection<Accessor,ModelRoot> _accessors;
		
		private const int _animationsMinItems = 1;
		private ChildrenCollection<Animation,ModelRoot> _animations;
		
		private Asset _asset;
		
		private const int _buffersMinItems = 1;
		private ChildrenCollection<Buffer,ModelRoot> _buffers;
		
		private const int _bufferViewsMinItems = 1;
		private ChildrenCollection<BufferView,ModelRoot> _bufferViews;
		
		private const int _camerasMinItems = 1;
		private ChildrenCollection<Camera,ModelRoot> _cameras;
		
		private const int _imagesMinItems = 1;
		private ChildrenCollection<Image,ModelRoot> _images;
		
		private const int _materialsMinItems = 1;
		private ChildrenCollection<Material,ModelRoot> _materials;
		
		private const int _meshesMinItems = 1;
		private ChildrenCollection<Mesh,ModelRoot> _meshes;
		
		private const int _nodesMinItems = 1;
		private ChildrenCollection<Node,ModelRoot> _nodes;
		
		private const int _samplersMinItems = 1;
		private ChildrenCollection<Sampler,ModelRoot> _samplers;
		
		private Int32? _scene;
		
		private const int _scenesMinItems = 1;
		private ChildrenCollection<Scene,ModelRoot> _scenes;
		
		private const int _skinsMinItems = 1;
		private ChildrenCollection<Skin,ModelRoot> _skins;
		
		private const int _texturesMinItems = 1;
		private ChildrenCollection<Texture,ModelRoot> _textures;
		
	
		protected override void SerializeProperties(JsonWriter writer)
		{
			base.SerializeProperties(writer);
			SerializeProperty(writer, "extensionsUsed", _extensionsUsed, _extensionsUsedMinItems);
			SerializeProperty(writer, "extensionsRequired", _extensionsRequired, _extensionsRequiredMinItems);
			SerializeProperty(writer, "accessors", _accessors, _accessorsMinItems);
			SerializeProperty(writer, "animations", _animations, _animationsMinItems);
			SerializePropertyObject(writer, "asset", _asset);
			SerializeProperty(writer, "buffers", _buffers, _buffersMinItems);
			SerializeProperty(writer, "bufferViews", _bufferViews, _bufferViewsMinItems);
			SerializeProperty(writer, "cameras", _cameras, _camerasMinItems);
			SerializeProperty(writer, "images", _images, _imagesMinItems);
			SerializeProperty(writer, "materials", _materials, _materialsMinItems);
			SerializeProperty(writer, "meshes", _meshes, _meshesMinItems);
			SerializeProperty(writer, "nodes", _nodes, _nodesMinItems);
			SerializeProperty(writer, "samplers", _samplers, _samplersMinItems);
			SerializeProperty(writer, "scene", _scene);
			SerializeProperty(writer, "scenes", _scenes, _scenesMinItems);
			SerializeProperty(writer, "skins", _skins, _skinsMinItems);
			SerializeProperty(writer, "textures", _textures, _texturesMinItems);
		}
	
		protected override void DeserializeProperty(JsonReader reader, string property)
		{
			switch (property)
			{
				case "extensionsUsed": DeserializeList<String>(reader, _extensionsUsed); break;
				case "extensionsRequired": DeserializeList<String>(reader, _extensionsRequired); break;
				case "accessors": DeserializeList<Accessor>(reader, _accessors); break;
				case "animations": DeserializeList<Animation>(reader, _animations); break;
				case "asset": _asset = DeserializeValue<Asset>(reader); break;
				case "buffers": DeserializeList<Buffer>(reader, _buffers); break;
				case "bufferViews": DeserializeList<BufferView>(reader, _bufferViews); break;
				case "cameras": DeserializeList<Camera>(reader, _cameras); break;
				case "images": DeserializeList<Image>(reader, _images); break;
				case "materials": DeserializeList<Material>(reader, _materials); break;
				case "meshes": DeserializeList<Mesh>(reader, _meshes); break;
				case "nodes": DeserializeList<Node>(reader, _nodes); break;
				case "samplers": DeserializeList<Sampler>(reader, _samplers); break;
				case "scene": _scene = DeserializeValue<Int32?>(reader); break;
				case "scenes": DeserializeList<Scene>(reader, _scenes); break;
				case "skins": DeserializeList<Skin>(reader, _skins); break;
				case "textures": DeserializeList<Texture>(reader, _textures); break;
				default: base.DeserializeProperty(reader, property); break;
			}
		}
	
	}

	partial class Image : LogicalChildOfRoot
	{
	
		private String _uri;
		
		private String _mimeType;
		
		private Int32? _bufferView;
		
	
		protected override void SerializeProperties(JsonWriter writer)
		{
			base.SerializeProperties(writer);
			SerializeProperty(writer, "uri", _uri);
			SerializeProperty(writer, "mimeType", _mimeType);
			SerializeProperty(writer, "bufferView", _bufferView);
		}
	
		protected override void DeserializeProperty(JsonReader reader, string property)
		{
			switch (property)
			{
				case "uri": _uri = DeserializeValue<String>(reader); break;
				case "mimeType": _mimeType = DeserializeValue<String>(reader); break;
				case "bufferView": _bufferView = DeserializeValue<Int32?>(reader); break;
				default: base.DeserializeProperty(reader, property); break;
			}
		}
	
	}

}
