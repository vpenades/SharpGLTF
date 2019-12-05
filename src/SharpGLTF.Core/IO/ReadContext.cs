using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

using Newtonsoft.Json;
using SharpGLTF.Schema2;

using BYTES = System.ArraySegment<byte>;
using SCHEMA2 = SharpGLTF.Schema2.ModelRoot;
using VALIDATIONMODE = SharpGLTF.Validation.ValidationMode;

namespace SharpGLTF.IO
{
    /// <summary>
    /// Callback used for loading associated files of current model.
    /// </summary>
    /// <param name="assetName">the asset relative path.</param>
    /// <returns>The file contents as a <see cref="byte"/> array.</returns>
    public delegate BYTES FileReaderCallback(String assetName);

    /// <summary>
    /// Context for reading a <see cref="SCHEMA2"/>.
    /// </summary>
    public class ReadContext : Schema2.ReadSettings
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
        /// When loading a GLB, this represents the internal binary data chunk.
        /// </summary>
        private Byte[] _BinaryChunk;

        #endregion

        #region API

        public BYTES ReadAllBytesToEnd(string fileName)
        {
            if (_BinaryChunk != null)
            {
                if (string.IsNullOrEmpty(fileName)) return new BYTES(_BinaryChunk);
            }

            return _FileReader(fileName);
        }

        /// <summary>
        /// Opens a file relative to this <see cref="ReadContext"/>.
        /// </summary>
        /// <param name="fileName">A relative file Name path.</param>
        /// <returns>A <see cref="Stream"/>.</returns>
        public Stream OpenFile(string fileName)
        {
            var content = _FileReader(fileName);

            return new MemoryStream(content.Array, content.Offset, content.Count);
        }

        /// <summary>
        /// Reads a <see cref="SCHEMA2"/> instance from a <see cref="Stream"/> containing a GLB or a GLTF file.
        /// </summary>
        /// <param name="stream">A <see cref="Stream"/> to read from.</param>
        /// <returns>A <see cref="SCHEMA2"/> instance.</returns>
        public SCHEMA2 ReadSchema2(Stream stream)
        {
            Guard.NotNull(stream, nameof(stream));

            bool binaryFile = glb._Identify(stream);

            return binaryFile ? ReadBinarySchema2(stream) : ReadTextSchema2(stream);
        }

        /// <summary>
        /// Reads a <see cref="SCHEMA2"/> instance from a <see cref="Stream"/> containing a GLTF file.
        /// </summary>
        /// <param name="stream">A <see cref="Stream"/> to read from.</param>
        /// <returns>A <see cref="SCHEMA2"/> instance.</returns>
        public SCHEMA2 ReadTextSchema2(Stream stream)
        {
            Guard.NotNull(stream, nameof(stream));

            string content = null;

            using (var streamReader = new StreamReader(stream))
            {
                content = streamReader.ReadToEnd();
            }

            return ParseJson(content);
        }

        /// <summary>
        /// Reads a <see cref="SCHEMA2"/> instance from a <see cref="Stream"/> containing a GLB file.
        /// </summary>
        /// <param name="stream">A <see cref="Stream"/> to read from.</param>
        /// <returns>A <see cref="SCHEMA2"/> instance.</returns>
        public SCHEMA2 ReadBinarySchema2(Stream stream)
        {
            Guard.NotNull(stream, nameof(stream));

            var mv = _ReadGLB(stream);

            if (mv.Validation.HasErrors) throw mv.Validation.Errors.FirstOrDefault();

            return mv.Model;
        }

        /// <summary>
        /// Parses a <see cref="SCHEMA2"/> instance from a <see cref="String"/> JSON content representing a GLTF file.
        /// </summary>
        /// <param name="jsonContent">A <see cref="String"/> JSON content representing a GLTF file.</param>
        /// <returns>A <see cref="SCHEMA2"/> instance.</returns>
        public SCHEMA2 ParseJson(String jsonContent)
        {
            var mv = _ParseGLTF(jsonContent);

            if (mv.Validation.HasErrors) throw mv.Validation.Errors.FirstOrDefault();

            return mv.Model;
        }

        public Validation.ValidationResult Validate(string filePath)
        {
            using (var stream = File.OpenRead(filePath))
            {
                bool isBinary = glb._Identify(stream);

                if (isBinary) return _ReadGLB(stream).Validation;

                string content = null;

                using (var streamReader = new StreamReader(stream))
                {
                    content = streamReader.ReadToEnd();
                }

                return _ParseGLTF(content).Validation;
            }
        }

        internal SCHEMA2 _ReadFromDictionary(string fileName)
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

        private (SCHEMA2 Model, Validation.ValidationResult Validation) _ReadGLB(Stream stream)
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

        private (SCHEMA2 Model, Validation.ValidationResult Validation) _ParseGLTF(String jsonContent)
        {
            Guard.NotNullOrEmpty(jsonContent, nameof(jsonContent));
            using (var tr = new StringReader(jsonContent))
            {
                return _Read(tr);
            }
        }

        private (SCHEMA2 Model, Validation.ValidationResult Validation) _Read(TextReader textReader)
        {
            Guard.NotNull(textReader, nameof(textReader));

            var root = new SCHEMA2();
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
}
