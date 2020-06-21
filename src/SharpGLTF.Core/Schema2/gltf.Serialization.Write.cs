using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

using BYTES = System.ArraySegment<byte>;
using VALIDATIONMODE = SharpGLTF.Validation.ValidationMode;

namespace SharpGLTF.Schema2
{
    using MODEL = ModelRoot;

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

        #region properties

        /// <summary>
        /// Gets or sets a value indicating how to write the images of the model.
        /// </summary>
        public ResourceWriteMode ImageWriting { get; set; } = ResourceWriteMode.Default;

        /// <summary>
        /// Gets or sets a callback hook that controls the image writing behavior.
        /// </summary>
        public IO.ImageWriterCallback ImageWriteCallback { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to merge all the buffers in <see cref="MODEL.LogicalBuffers"/> into a single buffer.
        /// </summary>
        public Boolean MergeBuffers { get; set; } = true;

        /// <summary>
        /// Gets or sets a value indicating how to format the JSON document of the glTF.
        /// </summary>
        public Boolean JsonIndented { get; set; } = false;

        /// <summary>
        /// Gets or sets a value indicating the level of validation applied when loading a file.
        /// </summary>
        public VALIDATIONMODE Validation { get; set; } = VALIDATIONMODE.Strict;

        #endregion

        #region API

        public void CopyTo(WriteSettings other)
        {
            Guard.NotNull(other, nameof(other));

            other.ImageWriting = this.ImageWriting;
            other.ImageWriteCallback = this.ImageWriteCallback;
            other.MergeBuffers = this.MergeBuffers;
            other.JsonIndented = this.JsonIndented;
            other.Validation = this.Validation;
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
            Guard.FilePathMustBeValid(filePath, nameof(filePath));

            bool isGltfExtension = filePath
                .ToLower(System.Globalization.CultureInfo.InvariantCulture)
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
            Guard.FilePathMustBeValid(filePath, nameof(filePath));

            var context = IO.WriteContext
                .CreateFromFile(filePath)
                .WithBinarySettings();

            settings?.CopyTo(context);

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
            Guard.FilePathMustBeValid(filePath, nameof(filePath));

            var context = IO.WriteContext
                .CreateFromFile(filePath);

            settings?.CopyTo(context);

            var name = Path.GetFileNameWithoutExtension(filePath);

            context.WriteTextSchema2(name, this);
        }

        /// <summary>
        /// Gets the JSON document of this <see cref="MODEL"/>.
        /// </summary>
        /// <param name="indented">The formatting of the JSON document.</param>
        /// <returns>A JSON content.</returns>
        public string GetJSON(bool indented)
        {
            using (var mm = new System.IO.MemoryStream())
            {
                _WriteJSON(mm, indented);

                mm.Position = 0;

                using (var ss = new System.IO.StreamReader(mm))
                {
                    return ss.ReadToEnd();
                }
            }

            /*
            using (var ss = new StringWriter())
            {
                _WriteJSON(ss, fmt);
                return ss.ToString();
            }*/
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

            var context = IO.WriteContext.CreateFromStream(stream);

            if (settings != null)
            {
                settings.CopyTo(context);
                context.MergeBuffers = true;
                context.ImageWriting = ResourceWriteMode.Default;
            }

            context.WriteBinarySchema2("model", this);
        }

        #endregion

        #region core

        internal void _WriteJSON(System.IO.Stream sw, bool indented)
        {
            System.Text.Json.JsonWriterOptions options = default;

            options.Indented = indented;

            using (var writer = new System.Text.Json.Utf8JsonWriter(sw, options))
            {
                this.Serialize(writer);
            }
        }

        internal void _PrepareBuffersForSatelliteWriting(IO.WriteContext context, string baseName)
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

        internal void _PrepareImagesForWriting(IO.WriteContext context, string baseName, ResourceWriteMode rmode)
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
