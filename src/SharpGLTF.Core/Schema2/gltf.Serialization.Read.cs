using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

using Newtonsoft.Json;

using BYTES = System.ArraySegment<byte>;

namespace SharpGLTF.Schema2
{
    using MODEL = ModelRoot;
    using VALIDATIONMODE = Validation.ValidationMode;

    public delegate Boolean ImageReaderCallback(Image image);

    /// <summary>
    /// Settings to customize how <see cref="MODEL"/> files are read.
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

        public ImageReaderCallback ImageReader { get; set; }

        #endregion

        #region API

        public void CopyTo(ReadSettings other)
        {
            Guard.NotNull(other, nameof(other));
            other.Validation = this.Validation;
            other.ImageReader = this.ImageReader;
        }

        #endregion
    }

    partial class ModelRoot
    {
        #region validate

        public static Validation.ValidationResult Validate(string filePath)
        {
            Guard.FilePathMustExist(filePath, nameof(filePath));

            var context = IO.ReadContext.CreateFromFile(filePath);

            return context.Validate(filePath);
        }

        #endregion

        #region read / load methods

        /// <summary>
        /// Reads a <see cref="MODEL"/> instance from a path pointing to a GLB or a GLTF file
        /// </summary>
        /// <param name="filePath">A valid file path.</param>
        /// <param name="settings">Optional settings.</param>
        /// <returns>A <see cref="MODEL"/> instance.</returns>
        public static MODEL Load(string filePath, ReadSettings settings = null)
        {
            Guard.FilePathMustExist(filePath, nameof(filePath));

            var context = IO.ReadContext.CreateFromFile(filePath);

            if (settings != null) settings.CopyTo(context);

            using (var s = File.OpenRead(filePath))
            {
                return context.ReadSchema2(s);
            }
        }

        /// <summary>
        /// Parses a <see cref="MODEL"/> instance from a <see cref="byte"/> array representing a GLB file
        /// </summary>
        /// <param name="glb">A <see cref="byte"/> array representing a GLB file</param>
        /// <param name="settings">Optional settings.</param>
        /// <returns>A <see cref="MODEL"/> instance.</returns>
        public static MODEL ParseGLB(BYTES glb, ReadSettings settings = null)
        {
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
            Guard.NotNull(stream, nameof(stream));
            Guard.IsTrue(stream.CanRead, nameof(stream));

            var context = IO.ReadContext
                .Create(f => throw new NotSupportedException());

            if (settings != null) settings.CopyTo(context);

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

            var context = IO.ReadContext.CreateFromFile(filePath);

            string json = null;

            using (var s = File.OpenRead(filePath))
            {
                json = context.ReadJson(s);
            }

            return ParseSatellitePaths(json);
        }

        private static string[] ParseSatellitePaths(string json)
        {
            var rss = Newtonsoft.Json.Linq.JObject.Parse(json);

            var buffers = rss["buffers"]?.Select(item => (String)item["uri"]);
            var images = rss["images"]?.Select(item => (String)item["uri"]);

            IEnumerable<string> uris = buffers;

            if (images != null)
            {
                uris = uris == null ? images : uris.Concat(images);
            }

            uris = uris
                .Where(uri => !string.IsNullOrWhiteSpace(uri))
                .Where(uri => !uri.StartsWith("data:", StringComparison.Ordinal))
                .Distinct();

            return uris.ToArray();
        }

        #endregion

        #region externals dependencies resolver

        internal void _ResolveSatelliteDependencies(IO.ReadContext context)
        {
            // resolve satellite buffers

            foreach (var buffer in this._buffers)
            {
                buffer._ResolveUri(context);
            }

            // resolve satellite images

            foreach (var image in this._images)
            {
                image._ResolveUri(context);

                if (context.ImageReader != null)
                {
                    if (!context.ImageReader(image))
                    {
                        image._DiscardContent();
                    }
                }
            }
        }

        #endregion
    }
}
