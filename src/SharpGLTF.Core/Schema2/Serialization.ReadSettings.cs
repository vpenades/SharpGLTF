using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using BYTES = System.ArraySegment<byte>;
using MODEL = SharpGLTF.Schema2.ModelRoot;
using VALIDATIONMODE = SharpGLTF.Validation.ValidationMode;

namespace SharpGLTF.Schema2
{
    /// <summary>
    /// Callback used to intercept the loading of textures so they can be
    /// decoded by the client engine and uploaded to the GPU if neccesary.
    /// </summary>
    /// <param name="image">The Image containing the texture</param>
    /// <returns>
    /// True if we want to keep the image memory data inside <see cref="Image"/>.<br/>
    /// Otherwise the memory will be cleared and <see cref="Image"/> will be empty.
    /// </returns>
    public delegate Boolean ImageDecodeCallback(Image image);

    /// <summary>
    /// Callback used to preprocess and postprocess json before reading and after writing.
    /// </summary>
    /// <param name="json">The source json text.</param>
    /// <returns>The processed json text.</returns>
    public delegate string JsonFilterCallback(string json);

    /// <summary>
    /// Read settings and base class of <see cref="ReadContext"/>
    /// </summary>
    public class ReadSettings
    {
        #region lifecycle

        public static implicit operator ReadSettings(VALIDATIONMODE vmode)
        {
            return new ReadSettings
            {
                Validation = vmode
            };
        }

        public ReadSettings() { }

        public ReadSettings(ReadSettings other)
        {
            Guard.NotNull(other, nameof(other));
            other.CopyTo(this);
        }

        #endregion

        #region properties

        /// <summary>
        /// Gets or sets a value indicating the level of validation applied when loading a file.
        /// </summary>
        public VALIDATIONMODE Validation { get; set; } = VALIDATIONMODE.Strict;

        /// <summary>
        /// Gets or sets the callback used to decode the textures as they're loaded.
        /// </summary>
        public ImageDecodeCallback ImageDecoder { get; set; }

        /// <summary>
        /// Gets or sets the callback used to preprocess the json text before parsing it.
        /// </summary>
        public JsonFilterCallback JsonPreprocessor { get; set; }

        #endregion

        #region API

        public void CopyTo(ReadSettings other)
        {
            Guard.NotNull(other, nameof(other));
            other.Validation = this.Validation;
            other.ImageDecoder = this.ImageDecoder;
            other.JsonPreprocessor = this.JsonPreprocessor;
        }

        #endregion
    }

    partial class ModelRoot
    {
        #region validate

        public static Validation.ValidationResult Validate(string filePath)
        {
            Guard.NotNull(filePath, nameof(filePath));
            var finfo = new System.IO.FileInfo(filePath);
            Guard.MustExist(finfo, nameof(filePath));

            return ReadContext
                .CreateFromDirectory(finfo.Directory)
                .Validate(finfo.Name);
        }

        #endregion

        #region read / load methods

        /// <summary>
        /// Reads a <see cref="MODEL"/> instance from a path pointing to a GLB or a GLTF file
        /// </summary>
        /// <param name="filePath">A valid file path.</param>
        /// <param name="settings">Optional settings.</param>
        /// <returns>A <see cref="MODEL"/> instance.</returns>
        /// <remarks>
        /// <paramref name="settings"/> can be either a plain <see cref="ReadSettings"/> instance,
        /// or a <see cref="ReadContext"/>, in which case, the context will be used to read the
        /// files from it.
        /// </remarks>
        public static MODEL Load(string filePath, ReadSettings settings = null)
        {
            Guard.NotNull(filePath, nameof(filePath));

            if (!(settings is ReadContext context))
            {
                var finfo = new System.IO.FileInfo(filePath);

                Guard.MustExist(finfo, nameof(filePath));

                context = ReadContext
                    .CreateFromDirectory(finfo.Directory)
                    .WithSettingsFrom(settings);

                filePath = finfo.Name;
            }

            // at this point, filePath must be a path "relative to context"            

            return context.ReadSchema2(filePath);
        }

        /// <summary>
        /// Parses a <see cref="MODEL"/> instance from a <see cref="byte"/> array representing a GLB file
        /// </summary>
        /// <param name="glb">A <see cref="byte"/> array representing a GLB file</param>
        /// <param name="settings">Optional settings.</param>
        /// <returns>A <see cref="MODEL"/> instance.</returns>
        public static MODEL ParseGLB(BYTES glb, ReadSettings settings = null)
        {
            System.Diagnostics.Debug.Assert(!(settings is ReadContext), "Use Load method.");

            Guard.NotNull(glb, nameof(glb));

            using (var m = new MemoryStream(glb.Array, glb.Offset, glb.Count, false))
            {
                return ReadGLB(m, settings);
            }
        }

        /// <summary>
        /// Reads a <see cref="MODEL"/> instance from a <see cref="Stream"/> representing a GLB file
        /// </summary>
        /// <param name="stream">The source <see cref="Stream"/>.</param>
        /// <param name="settings">Optional settings.</param>
        /// <returns>A <see cref="MODEL"/> instance.</returns>
        public static MODEL ReadGLB(Stream stream, ReadSettings settings = null)
        {
            System.Diagnostics.Debug.Assert(!(settings is ReadContext), "Use Load method.");

            Guard.NotNull(stream, nameof(stream));
            Guard.IsTrue(stream.CanRead, nameof(stream));

            var context = ReadContext
                .Create(f => throw new NotSupportedException())
                .WithSettingsFrom(settings);

            return context.ReadBinarySchema2(stream);
        }

        /// <summary>
        /// Gets the list of satellite / dependency files for a given glTF file.
        /// This includes binary blobs and texture images.
        /// </summary>
        /// <param name="filePath">A valid file path.</param>
        /// <returns>A list of relative file paths, as found in the file.</returns>
        /// <remarks>
        /// This method is designed to be as fast as possible, and it avoids performing much
        /// of the validation and parsing of a glTf file, it just blindly looks for URI fields.
        /// </remarks>
        public static string[] GetSatellitePaths(string filePath)
        {
            Guard.FilePathMustExist(filePath, nameof(filePath));

            var json = ReadOnlyMemory<byte>.Empty;

            using (var s = File.OpenRead(filePath))
            {
                json = ReadContext.ReadJsonBytes(s);
            }

            return ParseSatellitePaths(json);
        }

        private static string[] ParseSatellitePaths(ReadOnlyMemory<Byte> json)
        {
            var uris = new HashSet<string>();

            void _addUri(System.Text.Json.JsonElement property)
            {
                var newUri = property.GetString();

                if (string.IsNullOrWhiteSpace(newUri)) return;
                if (newUri.StartsWith("data:", StringComparison.OrdinalIgnoreCase)) return;

                uris.Add(newUri);
            }

            using (var dom = System.Text.Json.JsonDocument.Parse(json))
            {
                if (dom.RootElement.TryGetProperty("buffers", out System.Text.Json.JsonElement buffers))
                {
                    foreach (var buffer in buffers.EnumerateArray())
                    {
                        if (buffer.TryGetProperty("uri", out System.Text.Json.JsonElement uri)) _addUri(uri);
                    }
                }

                if (dom.RootElement.TryGetProperty("images", out System.Text.Json.JsonElement images))
                {
                    foreach (var image in images.EnumerateArray())
                    {
                        if (image.TryGetProperty("uri", out System.Text.Json.JsonElement uri)) _addUri(uri);
                    }
                }
            }

            return uris.ToArray();
        }

        #endregion

        #region externals dependencies resolver

        internal void OnDeserializationCompleted()
        {
        }

        internal void _ResolveSatelliteDependencies(ReadContext context)
        {
            // resolve satellite buffers

            foreach (var buffer in this._buffers)
            {
                buffer._ResolveUri(context);
            }

            // resolve satellite images

            foreach (var image in this._images)
            {
                // reads the image file into the current object.
                image._ResolveUri(context);

                // if we have a decoder hook, call the decoder, and free the memory.
                if (context.ImageDecoder != null)
                {
                    if (!context.ImageDecoder(image))
                    {
                        image._DiscardContent();
                    }
                }
            }
        }

        #endregion
    }
}
