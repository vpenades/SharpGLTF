using System;
using System.IO;

using BYTES = System.ArraySegment<byte>;
using MODEL = SharpGLTF.Schema2.ModelRoot;
using VALIDATIONMODE = SharpGLTF.Validation.ValidationMode;

namespace SharpGLTF.Schema2
{
    /// <summary>
    /// Determines how resources are written.
    /// </summary>
    public enum ResourceWriteMode
    {
        /// <summary>
        /// Use the most appropiate mode.
        /// </summary>
        Default,

        /// <summary>
        /// Resources will be stored as external satellite files.
        /// </summary>
        SatelliteFile,

        /// <summary>
        /// Resources will be embedded into the JSON encoded in MIME64.
        /// </summary>
        Embedded,

        /// <summary>
        /// Resources will be stored as internal binary buffers. Valid only for <see cref="Image"/>
        /// </summary>
        BufferView
    }

    /// <summary>
    /// Write settings and base class of <see cref="WriteContext"/>
    /// </summary>
    public class WriteSettings
    {
        #region lifecycle

        public static implicit operator WriteSettings(VALIDATIONMODE vmode)
        {
            return new WriteSettings
            {
                Validation = vmode
            };
        }

        public WriteSettings() { }

        public WriteSettings(WriteSettings other)
        {
            Guard.NotNull(other, nameof(other));
            other.CopyTo(this);
        }

        #endregion

        #region data

        private System.Text.Json.JsonWriterOptions _JsonOptions;

        #endregion

        #region properties

        /// <summary>
        /// Gets or sets a value indicating how to write the images of the model.
        /// </summary>
        public ResourceWriteMode ImageWriting { get; set; } = ResourceWriteMode.Default;

        /// <summary>
        /// Gets or sets a callback hook that controls the image writing behavior.
        /// </summary>
        public ImageWriterCallback ImageWriteCallback { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to merge all the buffers in <see cref="MODEL.LogicalBuffers"/> into a single buffer.
        /// </summary>
        public Boolean MergeBuffers { get; set; } = true;

        /// <summary>
        /// Gets or sets the size used to split all the resources into individual buffers.
        /// </summary>
        /// <remarks>
        /// It only has an effect when these conditions are met:
        /// <list type="table">
        /// <item><see cref="MergeBuffers"/> must be true.</item>
        /// <item>Output format must be glTF, not GLB</item>
        /// </list>
        /// </remarks>
        public int BuffersMaxSize { get; set; } = int.MaxValue;

        /// <summary>
        /// Gets or sets a value indicating whether the JSON formatting will include indentation.
        /// </summary>
        public Boolean JsonIndented
        {
            get => _JsonOptions.Indented;
            set => _JsonOptions.Indented = value;
        }

        /// <summary>
        /// Gets or sets a value indicating the Json options to be used for writing.
        /// </summary>
        public System.Text.Json.JsonWriterOptions JsonOptions
        {
            get => _JsonOptions;
            set => _JsonOptions = value;
        }

        /// <summary>
        /// Gets or sets a value indicating the level of validation applied when loading a file.
        /// </summary>
        public VALIDATIONMODE Validation { get; set; } = VALIDATIONMODE.Strict;

        /// <summary>
        /// Gets or sets the callback used to postprocess the json text before parsing it.
        /// </summary>
        public JsonFilterCallback JsonPostprocessor { get; set; }

        #endregion

        #region API

        public void CopyTo(WriteSettings other)
        {
            Guard.NotNull(other, nameof(other));

            other.ImageWriting = this.ImageWriting;
            other.ImageWriteCallback = this.ImageWriteCallback;
            other.MergeBuffers = this.MergeBuffers;
            other.BuffersMaxSize = this.BuffersMaxSize;
            other._JsonOptions = this._JsonOptions;
            other.Validation = this.Validation;
            other.JsonPostprocessor = this.JsonPostprocessor;
        }

        #endregion
    }

    partial class ModelRoot
    {
        #region save / write methods

        /// <summary>
        /// Writes this <see cref="MODEL"/> to a file in GLTF or GLB based on the extension of <paramref name="filePath"/>.
        /// </summary>
        /// <param name="filePath">A valid file path to write to.</param>
        /// <param name="settings">Optional settings.</param>
        public void Save(string filePath, WriteSettings settings = null)
        {
            Guard.NotNull(filePath, nameof(filePath));

            bool isGltfExtension = filePath
                .EndsWith(".gltf", StringComparison.OrdinalIgnoreCase);

            if (isGltfExtension) SaveGLTF(filePath, settings);
            else SaveGLB(filePath, settings);
        }

        /// <summary>
        /// Writes this <see cref="MODEL"/> to a file in GLB format.
        /// </summary>
        /// <param name="filePath">A valid file path to write to.</param>
        /// <param name="settings">Optional settings.</param>
        public void SaveGLB(string filePath, WriteSettings settings = null)
        {
            if (!(settings is WriteContext context))
            {
                context = WriteContext
                    .CreateFromFile(filePath)
                    .WithSettingsFrom(settings);
            }

            context.WithBinarySettings();

            var name = Path.GetFileNameWithoutExtension(filePath);

            context.WriteBinarySchema2(name, this);
        }

        /// <summary>
        /// Writes this <see cref="MODEL"/> to a file in GLTF format.
        /// </summary>
        /// <param name="filePath">A valid file path to write to.</param>
        /// <param name="settings">Optional settings.</param>
        /// <remarks>
        /// Satellite files like buffers and images are also saved with the file name formatted as "FILE_{Index}.EXT".
        /// </remarks>
        public void SaveGLTF(string filePath, WriteSettings settings = null)
        {
            if (!(settings is WriteContext context))
            {
                context = WriteContext
                    .CreateFromFile(filePath)
                    .WithSettingsFrom(settings);
            }

            context.WithTextSettings();

            var name = Path.GetFileNameWithoutExtension(filePath);

            context.WriteTextSchema2(name, this);
        }

        [Obsolete("Use GetJsonPreview", true)]
        public string GetJSON(bool indented) { return GetJsonPreview(); }

        /// <summary>
        /// Gets the JSON document of this <see cref="MODEL"/>.
        /// </summary>
        /// <returns>A JSON content.</returns>
        /// <remarks>
        /// ⚠ Beware: this method serializes the current model into a json, without taking care of the binary buffers,
        /// so the produced json might not be usable!
        /// </remarks>
        public string GetJsonPreview()
        {
            return _GetJSON(true);
        }

        /// <summary>
        /// Gets the JSON document of this <see cref="MODEL"/>.
        /// </summary>
        /// <param name="indented">The formatting of the JSON document.</param>
        /// <returns>A JSON content.</returns>
        internal string _GetJSON(bool indented)
        {
            var options = new System.Text.Json.JsonWriterOptions
            {
                Indented = indented
            };

            using (var mm = new System.IO.MemoryStream())
            {
                _WriteJSON(mm, options, null);

                mm.Position = 0;

                using (var ss = new System.IO.StreamReader(mm))
                {
                    return ss.ReadToEnd();
                }
            }
        }

        /// <summary>
        /// Writes this <see cref="MODEL"/> to a <see cref="byte"/> array in GLB format.
        /// </summary>
        /// <param name="settings">Optional settings.</param>
        /// <returns>A <see cref="byte"/> array containing a GLB file.</returns>
        public BYTES WriteGLB(WriteSettings settings = null)
        {
            using (var m = new MemoryStream())
            {
                WriteGLB(m, settings);

                return m.ToArraySegment();
            }
        }

        /// <summary>
        /// Writes this <see cref="MODEL"/> to a <see cref="Stream"/> in GLB format.
        /// </summary>
        /// <param name="stream">A <see cref="Stream"/> open for writing.</param>
        /// <param name="settings">Optional settings.</param>
        public void WriteGLB(Stream stream, WriteSettings settings = null)
        {
            Guard.NotNull(stream, nameof(stream));
            Guard.IsTrue(stream.CanWrite, nameof(stream));

            var context = WriteContext
                .CreateFromStream(stream)
                .WithSettingsFrom(settings);

            context.WithBinarySettings();

            context.WriteBinarySchema2("model", this);
        }

        #endregion

        #region core

        internal void _WriteJSON(System.IO.Stream sw, System.Text.Json.JsonWriterOptions options, JsonFilterCallback filter)
        {
            if (filter == null)
            {
                using (var writer = new System.Text.Json.Utf8JsonWriter(sw, options))
                {
                    this.Serialize(writer);
                }

                return;
            }

            string text = null;

            using (var mm = new System.IO.MemoryStream())
            {
                _WriteJSON(mm, options, null);

                mm.Position = 0;

                using (var ss = new System.IO.StreamReader(mm))
                {
                    text = ss.ReadToEnd();
                }
            }

            text = filter.Invoke(text);

            var bytes = System.Text.Encoding.UTF8.GetBytes(text);

            using (var mm = new System.IO.MemoryStream(bytes, false))
            {
                mm.CopyTo(sw);
            }
        }

        internal void _PrepareBuffersForSatelliteWriting(WriteContext context, string baseName)
        {
            // setup all buffers to be written as satellite files
            for (int i = 0; i < this._buffers.Count; ++i)
            {
                var buffer = this._buffers[i];
                var bname = this._buffers.Count != 1 ? $"{baseName}_{i}.bin" : $"{baseName}.bin";
                buffer._WriteToSatellite(context, bname);
            }
        }

        internal void _PrepareBuffersForInternalWriting()
        {
            // setup all buffers to be written internally
            for (int i = 0; i < this._buffers.Count; ++i)
            {
                var buffer = this._buffers[i];
                buffer._WriteToInternal();
            }
        }

        internal void _PrepareImagesForWriting(WriteContext context, string baseName, ResourceWriteMode rmode)
        {
            if (context.ImageWriting != ResourceWriteMode.Default) rmode = context.ImageWriting;

            // setup all images to be written to the appropiate location.
            for (int i = 0; i < this._images.Count; ++i)
            {
                var image = this._images[i];

                if (rmode != ResourceWriteMode.SatelliteFile)
                {
                    image._WriteToInternal();
                }
                else
                {
                    var iname = this._images.Count != 1 ? $"{baseName}_{i}" : $"{baseName}";
                    image._WriteToSatellite(context, iname);
                }
            }
        }

        internal void _AfterWriting()
        {
            foreach (var b in this._buffers) b._ClearAfterWrite();
            foreach (var i in this._images) i._ClearAfterWrite();
        }

        #endregion
    }
}
