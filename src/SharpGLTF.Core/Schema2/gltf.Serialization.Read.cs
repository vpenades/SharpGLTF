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

    /// <summary>
    /// Callback used for loading associated files of current model.
    /// </summary>
    /// <param name="assetName">the asset relative path.</param>
    /// <returns>The file contents as a <see cref="byte"/> array.</returns>
    public delegate BYTES AssetReader(String assetName);

    /// <summary>
    /// Configuration settings for reading model files.
    /// </summary>
    public class ReadSettings
    {
        public ReadSettings()
        {
        }

        internal ReadSettings(AssetReader reader)
        {
            FileReader = reader;
        }

        internal ReadSettings(string filePath)
        {
            Guard.FilePathMustExist(filePath, nameof(filePath));

            var dir = Path.GetDirectoryName(filePath);

            FileReader = assetFileName => new BYTES(File.ReadAllBytes(Path.Combine(dir, assetFileName)));
        }

        /// <summary>
        /// Gets or sets the <see cref="AssetReader"/> delegate used to read satellite files.
        /// </summary>
        public AssetReader FileReader { get; set; }

        public Boolean SkipValidation { get; set; }
    }

    partial class ModelRoot
    {
        #region read / load methods

        /// <summary>
        /// Reads a <see cref="MODEL"/> instance from a path pointing to a GLB or a GLTF file
        /// </summary>
        /// <param name="filePath">A valid file path.</param>
        /// <returns>A <see cref="MODEL"/> instance.</returns>
        public static MODEL Load(string filePath)
        {
            Guard.FilePathMustExist(filePath, nameof(filePath));

            var settings = new ReadSettings(filePath);

            using (var s = File.OpenRead(filePath))
            {
                return Read(s, settings);
            }
        }

        /// <summary>
        /// Parses a <see cref="MODEL"/> instance from a <see cref="byte"/> array representing a GLB file
        /// </summary>
        /// <param name="glb">A <see cref="byte"/> array representing a GLB file</param>
        /// <returns>A <see cref="MODEL"/> instance.</returns>
        public static MODEL ParseGLB(BYTES glb)
        {
            Guard.NotNull(glb, nameof(glb));

            using (var m = new MemoryStream(glb.Array, glb.Offset, glb.Count, false))
            {
                return ReadGLB(m, new ReadSettings());
            }
        }

        /// <summary>
        /// Reads a <see cref="MODEL"/> instance from a <see cref="Stream"/> containing a GLB or a GLTF file.
        /// </summary>
        /// <param name="stream">A <see cref="Stream"/> to read from.</param>
        /// <param name="settings">A <see cref="ReadSettings"/> instance defining the reading options.</param>
        /// <returns>A <see cref="MODEL"/> instance.</returns>
        public static MODEL Read(Stream stream, ReadSettings settings)
        {
            Guard.NotNull(stream, nameof(stream));

            bool binaryFile = glb._Identify(stream);

            if (binaryFile) return ReadGLB(stream, settings);
            else            return ReadGLTF(stream, settings);
        }

        /// <summary>
        /// Reads a <see cref="MODEL"/> instance from a <see cref="Stream"/> containing a GLTF file.
        /// </summary>
        /// <param name="stream">A <see cref="Stream"/> to read from.</param>
        /// <param name="settings">A <see cref="ReadSettings"/> instance defining the reading options.</param>
        /// <returns>A <see cref="MODEL"/> instance.</returns>
        public static MODEL ReadGLTF(Stream stream, ReadSettings settings)
        {
            Guard.NotNull(stream, nameof(stream));
            Guard.NotNull(settings, nameof(settings));

            string content = null;

            using (var streamReader = new StreamReader(stream))
            {
                content = streamReader.ReadToEnd();
            }

            return ParseGLTF(content, settings);
        }

        /// <summary>
        /// Reads a <see cref="MODEL"/> instance from a <see cref="Stream"/> containing a GLB file.
        /// </summary>
        /// <param name="stream">A <see cref="Stream"/> to read from.</param>
        /// <param name="settings">A <see cref="ReadSettings"/> instance defining the reading options.</param>
        /// <returns>A <see cref="MODEL"/> instance.</returns>
        public static MODEL ReadGLB(Stream stream, ReadSettings settings)
        {
            Guard.NotNull(stream, nameof(stream));
            Guard.NotNull(settings, nameof(settings));

            var chunks = glb.ReadBinaryFile(stream);

            var dom = Encoding.UTF8.GetString(chunks[glb.CHUNKJSON]);

            if (chunks.ContainsKey(glb.CHUNKBIN))
            {
                var sourceReader = settings.FileReader;

                settings.FileReader =
                    key =>
                    string.IsNullOrEmpty(key)
                    ?
                    new BYTES(chunks[glb.CHUNKBIN])
                    :
                    sourceReader.Invoke(key);
            }

            return ParseGLTF(dom, settings);
        }

        /// <summary>
        /// Parses a <see cref="MODEL"/> instance from a <see cref="String"/> JSON content representing a GLTF file.
        /// </summary>
        /// <param name="jsonContent">A <see cref="String"/> JSON content representing a GLTF file.</param>
        /// <param name="settings">A <see cref="ReadSettings"/> instance defining the reading options.</param>
        /// <returns>A <see cref="MODEL"/> instance.</returns>
        public static MODEL ParseGLTF(String jsonContent, ReadSettings settings)
        {
            Guard.NotNullOrEmpty(jsonContent, nameof(jsonContent));
            Guard.NotNull(settings, nameof(settings));

            using (var tr = new StringReader(jsonContent))
            {
                return _Read(tr, settings);
            }
        }

        public static MODEL ReadFromDictionary(Dictionary<string, BYTES> files, string fileName)
        {
            Guard.NotNull(files, nameof(files));

            var jsonBytes = files[fileName];

            var settings = new ReadSettings(fn => files[fn]);

            using (var m = new MemoryStream(jsonBytes.Array, jsonBytes.Offset, jsonBytes.Count))
            {
                using (var s = new StreamReader(m))
                {
                    return _Read(s, settings);
                }
            }
        }

        #endregion

        #region reading core

        private static MODEL _Read(TextReader textReader, ReadSettings settings)
        {
            Guard.NotNull(textReader, nameof(textReader));
            Guard.NotNull(settings, nameof(settings));

            using (var reader = new JsonTextReader(textReader))
            {
                var root = new MODEL();

                try
                {
                    reader.Read();
                    root.Deserialize(reader);
                }
                catch (JsonReaderException rex)
                {
                    throw new Validation.SchemaException(root, rex);
                }

                // reference checking is mandatory and cannot be skipped
                var result = new Validation.ValidationResult();
                root.ValidateReferences(result.GetContext(root));
                var ex = result.Errors.FirstOrDefault();
                if (ex != null) throw ex;

                foreach (var buffer in root._buffers)
                {
                    buffer._ResolveUri(settings.FileReader);
                }

                foreach (var image in root._images)
                {
                    image._ResolveUri(settings.FileReader);
                }

                if (!settings.SkipValidation)
                {
                    result = new Validation.ValidationResult();
                    root.Validate(result.GetContext(root));
                    ex = result.Errors.FirstOrDefault();
                    if (ex != null) throw ex;
                }

                return root;
            }
        }

        #endregion
    }
}
