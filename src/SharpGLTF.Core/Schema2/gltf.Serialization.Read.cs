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
    public delegate BYTES FileReaderCallback(String assetName);

    public delegate Boolean ImageReaderCallback(Image image);

    /// <summary>
    /// Configuration settings for reading model files.
    /// </summary>
    public class ReadSettings
    {
        #region lifecycle

        public ReadSettings() { }

        public ReadSettings(ReadSettings other)
        {
            this.Validation = other.Validation;
        }

        #endregion

        #region properties

        /// <summary>
        /// Gets or sets a value indicating the level of validation applied when loading a file.
        /// </summary>
        public VALIDATIONMODE Validation { get; set; } = VALIDATIONMODE.Strict;

        #endregion
    }

    public class ReadContext : ReadSettings
    {
        #region lifecycle

        public static ReadContext Create(FileReaderCallback callback)
        {
            Guard.NotNull(callback, nameof(callback));

            return new ReadContext(callback);
        }

        public static ReadContext CreateFromFile(string filePath)
        {
            Guard.FilePathMustExist(filePath, nameof(filePath));

            var dir = Path.GetDirectoryName(filePath);

            return CreateFromDirectory(dir);
        }

        public static ReadContext CreateFromDirectory(string directoryPath)
        {
            return new ReadContext(assetFileName => new BYTES(File.ReadAllBytes(Path.Combine(directoryPath, assetFileName))));
        }

        public static ReadContext CreateFromDictionary(IReadOnlyDictionary<string, BYTES> dictionary)
        {
            return new ReadContext(fn => dictionary[fn]);
        }

        private ReadContext(FileReaderCallback reader)
        {
            _FileReader = reader;
        }

        internal ReadContext(ReadContext other)
            : base(other)
        {
            this._FileReader = other._FileReader;
            this.ImageReader = other.ImageReader;
        }

        #endregion

        #region data

        private FileReaderCallback _FileReader;

        /// <summary>
        /// When loading GLB, this represents the internal binary data chunk.
        /// </summary>
        private Byte[] _BinaryChunk;

        #endregion

        #region callbacks

        public ImageReaderCallback ImageReader { get; set; }

        #endregion

        #region API

        public BYTES ReadBytes(string fileName)
        {
            if (_BinaryChunk != null)
            {
                if (string.IsNullOrEmpty(fileName)) return new BYTES(_BinaryChunk);
            }

            return _FileReader(fileName);
        }

        public Stream OpenFile(string fileName)
        {
            var content = _FileReader(fileName);

            return new MemoryStream(content.Array, content.Offset, content.Count);
        }

        /// <summary>
        /// Reads a <see cref="MODEL"/> instance from a <see cref="Stream"/> containing a GLB or a GLTF file.
        /// </summary>
        /// <param name="stream">A <see cref="Stream"/> to read from.</param>
        /// <returns>A <see cref="MODEL"/> instance.</returns>
        public MODEL Read(Stream stream)
        {
            Guard.NotNull(stream, nameof(stream));

            bool binaryFile = glb._Identify(stream);

            return binaryFile ? ReadGLB(stream) : ReadGLTF(stream);
        }

        /// <summary>
        /// Reads a <see cref="MODEL"/> instance from a <see cref="Stream"/> containing a GLTF file.
        /// </summary>
        /// <param name="stream">A <see cref="Stream"/> to read from.</param>
        /// <returns>A <see cref="MODEL"/> instance.</returns>
        public MODEL ReadGLTF(Stream stream)
        {
            Guard.NotNull(stream, nameof(stream));

            string content = null;

            using (var streamReader = new StreamReader(stream))
            {
                content = streamReader.ReadToEnd();
            }

            return ParseGLTF(content);
        }

        /// <summary>
        /// Reads a <see cref="MODEL"/> instance from a <see cref="Stream"/> containing a GLB file.
        /// </summary>
        /// <param name="stream">A <see cref="Stream"/> to read from.</param>
        /// <returns>A <see cref="MODEL"/> instance.</returns>
        public MODEL ReadGLB(Stream stream)
        {
            Guard.NotNull(stream, nameof(stream));

            var mv = _ReadGLB(stream);

            if (mv.Validation.HasErrors) throw mv.Validation.Errors.FirstOrDefault();

            return mv.Model;
        }

        /// <summary>
        /// Parses a <see cref="MODEL"/> instance from a <see cref="String"/> JSON content representing a GLTF file.
        /// </summary>
        /// <param name="jsonContent">A <see cref="String"/> JSON content representing a GLTF file.</param>
        /// <returns>A <see cref="MODEL"/> instance.</returns>
        public MODEL ParseGLTF(String jsonContent)
        {
            var mv = _ParseGLTF(jsonContent);

            if (mv.Validation.HasErrors) throw mv.Validation.Errors.FirstOrDefault();

            return mv.Model;
        }

        public Validation.ValidationResult Validate(string filePath)
        {
            using (var stream = File.OpenRead(filePath))
            {
                bool binaryFile = glb._Identify(stream);

                if (binaryFile) return _ReadGLB(stream).Validation;

                string content = null;

                using (var streamReader = new StreamReader(stream))
                {
                    content = streamReader.ReadToEnd();
                }

                return _ParseGLTF(content).Validation;
            }
        }

        internal MODEL _ReadFromDictionary(string fileName)
        {
            using (var s = this.OpenFile(fileName))
            {
                using (var tr = new StreamReader(s))
                {
                    var mv = this._Read(tr);

                    if (mv.Validation.HasErrors) throw mv.Validation.Errors.FirstOrDefault();

                    return mv.Model;
                }
            }
        }

        #endregion

        #region core

        private (MODEL Model, Validation.ValidationResult Validation) _ReadGLB(Stream stream)
        {
            Guard.NotNull(stream, nameof(stream));

            var chunks = glb.ReadBinaryFile(stream);

            var dom = Encoding.UTF8.GetString(chunks[glb.CHUNKJSON]);

            var context = this;

            if (chunks.ContainsKey(glb.CHUNKBIN))
            {
                context = new ReadContext(context); // clone instance
                context._BinaryChunk = chunks[glb.CHUNKBIN];
            }

            return context._ParseGLTF(dom);
        }

        private (MODEL Model, Validation.ValidationResult Validation) _ParseGLTF(String jsonContent)
        {
            Guard.NotNullOrEmpty(jsonContent, nameof(jsonContent));
            using (var tr = new StringReader(jsonContent))
            {
                return _Read(tr);
            }
        }

        private (MODEL Model, Validation.ValidationResult Validation) _Read(TextReader textReader)
        {
            Guard.NotNull(textReader, nameof(textReader));

            var root = new MODEL();
            var vcontext = new Validation.ValidationResult(root, this.Validation);

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

            root.ValidateReferences(vcontext.GetContext());
            var ex = vcontext.Errors.FirstOrDefault();
            if (ex != null) return (null, vcontext);

            // resolve external dependencies

            root._ResolveSatelliteDependencies(this);

            // full validation

            if (this.Validation != VALIDATIONMODE.Skip)
            {
                root.Validate(vcontext.GetContext());
                ex = vcontext.Errors.FirstOrDefault();
                if (ex != null) return (null, vcontext);
            }

            return (root, vcontext);
        }

        #endregion
    }

    partial class ModelRoot
    {
        #region validate

        public static Validation.ValidationResult Validate(string filePath)
        {
            Guard.FilePathMustExist(filePath, nameof(filePath));

            var context = ReadContext.CreateFromFile(filePath);

            return context.Validate(filePath);
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

            var context = ReadContext.CreateFromFile(filePath);

            context.Validation = vmode;

            using (var s = File.OpenRead(filePath))
            {
                return context.Read(s);
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

            var context = ReadContext.Create(f => throw new NotSupportedException());

            using (var m = new MemoryStream(glb.Array, glb.Offset, glb.Count, false))
            {
                return context.ReadGLB(m);
            }
        }

        #endregion

        #region externals resolver

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
