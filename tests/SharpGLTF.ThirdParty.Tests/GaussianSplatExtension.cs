using System;
using System.Collections.Generic;

using JSONREADER = System.Text.Json.Utf8JsonReader;
using JSONWRITER = System.Text.Json.Utf8JsonWriter;
using FIELDINFO = SharpGLTF.Reflection.FieldInfo;
using SharpGLTF.Schema2;

namespace SharpGLTF.ThirdParty
{
    partial class SpzGaussianSplatsCompression
    {
        private MeshPrimitive meshPrimitive;

        internal SpzGaussianSplatsCompression(MeshPrimitive meshPrimitive)
        {
            this.meshPrimitive = meshPrimitive;
        }

        public int BufferViewIndex
        {
            get => _bufferView;
            set
            {
                _bufferView = value;
            }
        }
    }




    /// <summary>
    /// Compressed data for SPZ primitive.
    /// </summary>
#if NET6_0_OR_GREATER
	[System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembers(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.NonPublicConstructors | System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicConstructors)]
#endif
    [global::System.CodeDom.Compiler.GeneratedCodeAttribute("SharpGLTF.CodeGen", "1.0.0.0")]
    partial class SpzGaussianSplatsCompression : ExtraProperties
    {

        #region reflection

        public new const string SCHEMANAME = "KHR_spz_gaussian_splats_compression";
        protected override string GetSchemaName() => SCHEMANAME;

        protected override IEnumerable<string> ReflectFieldsNames()
        {
            yield return "bufferView";
            foreach (var f in base.ReflectFieldsNames()) yield return f;
        }
        protected override bool TryReflectField(string name, out FIELDINFO value)
        {
            switch (name)
            {
                case "bufferView": value = FIELDINFO.From("bufferView", this, instance => instance._bufferView); return true;
                default: return base.TryReflectField(name, out value);
            }
        }

        #endregion

        #region data

        private Int32 _bufferView;

        #endregion

        #region serialization

        protected override void SerializeProperties(JSONWRITER writer)
        {
            base.SerializeProperties(writer);
            SerializeProperty(writer, "bufferView", _bufferView);
        }

        protected override void DeserializeProperty(string jsonPropertyName, ref JSONREADER reader)
        {
            switch (jsonPropertyName)
            {
                case "bufferView": DeserializePropertyValue<SpzGaussianSplatsCompression, Int32>(ref reader, this, out _bufferView); break;
                default: base.DeserializeProperty(jsonPropertyName, ref reader); break;
            }
        }

        #endregion

    }
}
