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

        /// <summary>
        /// Gets or sets a value indicating the level of validation applied when loading a file.
        /// </summary>
        public VALIDATIONMODE Validation { get; set; } = VALIDATIONMODE.Strict;
    }

    partial class ModelRoot
    {
        #region validate

        public static Validation.ValidationResult Validate(string filePath)
        {
            Guard.FilePathMustExist(filePath, nameof(filePath));

            var settings = new ReadSettings(filePath);

            using (var stream = File.OpenRead(filePath))
            {
                bool binaryFile = glb._Identify(stream);

                if (binaryFile) return _ReadGLB(stream, settings).Validation;

                string content = null;

                using (var streamReader = new StreamReader(stream))
                {
                    content = streamReader.ReadToEnd();
                }

                return _ParseGLTF(content, settings).Validation;
            }
        }

        #endregion

        #region read / load methods

        /// <summary>
        /// Reads a <see cref="MODEL"/> instance from a path pointing to a GLB or a GLTF file
        /// </summary>
        /// <param name="filePath">A valid file path.</param>
        /// <param name="vmode">Defines the file validation level.</param>
        /// <returns>A <see cref="MODEL"/> instance.</returns>
        public static MODEL Load(string filePath, VALIDATIONMODE vmode = VALIDATIONMODE.Strict)
        {
            Guard.FilePathMustExist(filePath, nameof(filePath));

            var settings = new ReadSettings(filePath);

            settings.Validation = vmode;

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

            var mv = _ReadGLB(stream, settings);

            if (mv.Validation.HasErrors) throw mv.Validation.Errors.FirstOrDefault();

            return mv.Model;
        }

        /// <summary>
        /// Parses a <see cref="MODEL"/> instance from a <see cref="String"/> JSON content representing a GLTF file.
        /// </summary>
        /// <param name="jsonContent">A <see cref="String"/> JSON content representing a GLTF file.</param>
        /// <param name="settings">A <see cref="ReadSettings"/> instance defining the reading options.</param>
        /// <returns>A <see cref="MODEL"/> instance.</returns>
        public static MODEL ParseGLTF(String jsonContent, ReadSettings settings)
        {
            var mv = _ParseGLTF(jsonContent, settings);

            if (mv.Validation.HasErrors) throw mv.Validation.Errors.FirstOrDefault();

            return mv.Model;
        }

        public static MODEL ReadFromDictionary(Dictionary<string, BYTES> files, string fileName, VALIDATIONMODE vmode = VALIDATIONMODE.Strict)
        {
            Guard.NotNull(files, nameof(files));

            var jsonBytes = files[fileName];

            var settings = new ReadSettings(fn => files[fn]);

            settings.Validation = vmode;

            using (var m = new MemoryStream(jsonBytes.Array, jsonBytes.Offset, jsonBytes.Count))
            {
                using (var tr = new StreamReader(m))
                {
                    var mv = _Read(tr, settings);

                    if (mv.Validation.HasErrors) throw mv.Validation.Errors.FirstOrDefault();

                    return mv.Model;
                }
            }
        }

        #endregion

        #region reading core

        private static (MODEL Model, Validation.ValidationResult Validation) _ReadGLB(Stream stream, ReadSettings settings)
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

            return _ParseGLTF(dom, settings);
        }

        private static (MODEL Model, Validation.ValidationResult Validation) _ParseGLTF(String jsonContent, ReadSettings settings)
        {
            Guard.NotNullOrEmpty(jsonContent, nameof(jsonContent));
            Guard.NotNull(settings, nameof(settings));

            using (var tr = new StringReader(jsonContent))
            {
                return _Read(tr, settings);
            }
        }

        private static (MODEL Model, Validation.ValidationResult Validation) _Read(TextReader textReader, ReadSettings settings)
        {
            Guard.NotNull(textReader, nameof(textReader));
            Guard.NotNull(settings, nameof(settings));

            var root = new MODEL();
            var vcontext = new Validation.ValidationResult(root, settings.Validation);

            using (var reader = new JsonTextReader(textReader))
            {
                if (!reader.Read())
                {
                    vcontext.AddError(new Validation.ModelException(root, "Json is empty"));
                    return (null, vcontext);
                }

                try
                {
                    root.Deserialize(reader);
                }
                catch (JsonReaderException rex)
                {
                    vcontext.AddError(new Validation.SchemaException(root, rex));
                    return (null, vcontext);
                }
            }

            // schema validation

            root.ValidateReferences(vcontext.GetContext(root));
            var ex = vcontext.Errors.FirstOrDefault();
            if (ex != null) return (null, vcontext);

            // resolve external references

            foreach (var buffer in root._buffers)
            {
                buffer._ResolveUri(settings.FileReader);
            }

            foreach (var image in root._images)
            {
                image._ResolveUri(settings.FileReader);
            }

            // full validation

            if (settings.Validation != VALIDATIONMODE.Skip)
            {
                root.Validate(vcontext.GetContext(root));
                ex = vcontext.Errors.FirstOrDefault();
                if (ex != null) return (null, vcontext);
            }

            return (root, vcontext);
        }

        #endregion
    }
}
