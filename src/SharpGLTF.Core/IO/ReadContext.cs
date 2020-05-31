using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

// using Newtonsoft.Json;
using System.Text.Json;
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

    public delegate String UriResolver(String relativeUri);

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
            directoryPath = System.IO.Path.GetFullPath(directoryPath);

            string _uriSolver(string rawUri)
            {
                var path = Uri.UnescapeDataString(rawUri);
                return Path.Combine(directoryPath, path);
            }

            BYTES _loadFile(string rawUri)
            {
                var path = _uriSolver(rawUri);
                var content = File.ReadAllBytes(path);
                return new BYTES(content);
            }

            return new ReadContext(_loadFile, _uriSolver);
        }

        public static ReadContext CreateFromDictionary(IReadOnlyDictionary<string, BYTES> dictionary)
        {
            return new ReadContext(rawUri => dictionary[rawUri]);
        }

        private ReadContext(FileReaderCallback reader, UriResolver uriResolver = null)
        {
            _FileReader = reader;
            _UriResolver = uriResolver;
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
        private UriResolver _UriResolver;

        /// <summary>
        /// When loading a GLB, this represents the internal binary data chunk.
        /// </summary>
        private Byte[] _BinaryChunk;

        #endregion

        #region API - File System

        public bool TryGetFullPath(string relativeUri, out string fullPath)
        {
            if (_UriResolver == null) { fullPath = null; return false; }
            fullPath = _UriResolver(relativeUri);
            return true;
        }

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

        #endregion

        #region API

        public Validation.ValidationResult Validate(string filePath)
        {
            using (var stream = File.OpenRead(filePath))
            {
                bool isBinary = BinarySerialization._Identify(stream);

                if (isBinary) return _ReadGLB(stream).Validation;

                var json = stream.ReadBytesToEnd();

                return _Read(json).Validation;
            }
        }

        /// <summary>
        /// Reads a <see cref="SCHEMA2"/> instance from a <see cref="Stream"/> containing a GLB or a GLTF file.
        /// </summary>
        /// <param name="stream">A <see cref="Stream"/> to read from.</param>
        /// <returns>A <see cref="SCHEMA2"/> instance.</returns>
        public SCHEMA2 ReadSchema2(Stream stream)
        {
            Guard.NotNull(stream, nameof(stream));
            Guard.IsTrue(stream.CanRead, nameof(stream));

            bool binaryFile = BinarySerialization._Identify(stream);

            return binaryFile ? ReadBinarySchema2(stream) : ReadTextSchema2(stream);
        }

        internal SCHEMA2 _ReadFromDictionary(string fileName)
        {
            var json = this.ReadAllBytesToEnd(fileName);

            var mv = this._Read(json);

            if (mv.Validation.HasErrors) throw mv.Validation.Errors.FirstOrDefault();

            return mv.Model;
        }

        /// <summary>
        /// Reads a <see cref="SCHEMA2"/> instance from a <see cref="Stream"/> containing a GLTF file.
        /// </summary>
        /// <param name="stream">A <see cref="Stream"/> to read from.</param>
        /// <returns>A <see cref="SCHEMA2"/> instance.</returns>
        public SCHEMA2 ReadTextSchema2(Stream stream)
        {
            Guard.NotNull(stream, nameof(stream));
            Guard.IsTrue(stream.CanRead, nameof(stream));

            var json = stream.ReadBytesToEnd();

            var mv = this._Read(json);

            if (mv.Validation.HasErrors) throw mv.Validation.Errors.FirstOrDefault();

            return mv.Model;
        }

        /// <summary>
        /// Reads a <see cref="SCHEMA2"/> instance from a <see cref="Stream"/> containing a GLB file.
        /// </summary>
        /// <param name="stream">A <see cref="Stream"/> to read from.</param>
        /// <returns>A <see cref="SCHEMA2"/> instance.</returns>
        public SCHEMA2 ReadBinarySchema2(Stream stream)
        {
            Guard.NotNull(stream, nameof(stream));
            Guard.IsTrue(stream.CanRead, nameof(stream));

            var mv = this._ReadGLB(stream);

            if (mv.Validation.HasErrors) throw mv.Validation.Errors.FirstOrDefault();

            return mv.Model;
        }

        #endregion

        #region core

        private (SCHEMA2 Model, Validation.ValidationResult Validation) _ReadGLB(Stream stream)
        {
            Guard.NotNull(stream, nameof(stream));

            IReadOnlyDictionary<uint, byte[]> chunks;

            try
            {
                chunks = BinarySerialization.ReadBinaryFile(stream);
            }
            catch (System.IO.EndOfStreamException ex)
            {
                var vr = new Validation.ValidationResult(null, this.Validation);
                vr.SetError(new Validation.SchemaException(null, ex.Message));
                return (null, vr);
            }
            catch (Validation.SchemaException ex)
            {
                var vr = new Validation.ValidationResult(null, this.Validation);
                vr.SetError(ex);
                return (null, vr);
            }

            var context = this;

            if (chunks.ContainsKey(BinarySerialization.CHUNKBIN))
            {
                // clone self
                var binChunk = chunks[BinarySerialization.CHUNKBIN];
                context = new ReadContext(context);
                context._BinaryChunk = binChunk;
            }

            var jsonChunk = chunks[BinarySerialization.CHUNKJSON];

            return context._Read(jsonChunk);
        }

        private (SCHEMA2 Model, Validation.ValidationResult Validation) _Read(ReadOnlyMemory<Byte> jsonUtf8Bytes)
        {
            var root = new SCHEMA2();
            var vcontext = new Validation.ValidationResult(root, this.Validation);

            try
            {
                if (jsonUtf8Bytes.IsEmpty) throw new System.Text.Json.JsonException("JSon is empty.");

                var reader = new Utf8JsonReader(jsonUtf8Bytes.Span);

                if (!reader.Read())
                {
                    vcontext.SetError(new Validation.SchemaException(root, "Json is empty"));
                    return (null, vcontext);
                }

                root.Deserialize(ref reader);
                root.OnDeserializationCompleted();

                // binary chunk check

                foreach (var b in root.LogicalBuffers) b.OnValidateBinaryChunk(vcontext.GetContext(), this._BinaryChunk);

                // schema validation

                root.ValidateReferences(vcontext.GetContext());
                var ex = vcontext.Errors.FirstOrDefault();
                if (ex != null) return (null, vcontext);

                // resolve external dependencies

                root._ResolveSatelliteDependencies(this);

                // full validation

                if (this.Validation != VALIDATIONMODE.Skip)
                {
                    root.ValidateContent(vcontext.GetContext());
                    ex = vcontext.Errors.FirstOrDefault();
                    if (ex != null) return (null, vcontext);
                }
            }
            catch (JsonException rex)
            {
                vcontext.SetError(new Validation.SchemaException(root, rex));
                return (null, vcontext);
            }
            catch (System.FormatException fex)
            {
                vcontext.SetError(new Validation.ModelException(null, fex));
                return (null, vcontext);
            }
            catch (ArgumentException aex)
            {
                vcontext.SetError(new Validation.ModelException(root, aex));
                return (null, vcontext);
            }
            catch (Validation.ModelException mex)
            {
                vcontext.SetError(mex);
                return (null, vcontext);
            }

            return (root, vcontext);
        }

        #endregion

        #region extras

        public static String ReadJson(Stream stream)
        {
            Guard.NotNull(stream, nameof(stream));

            bool binaryFile = BinarySerialization._Identify(stream);

            if (binaryFile)
            {
                var chunks = BinarySerialization.ReadBinaryFile(stream);

                return Encoding.UTF8.GetString(chunks[BinarySerialization.CHUNKJSON]);
            }

            using (var streamReader = new StreamReader(stream))
            {
                return streamReader.ReadToEnd();
            }
        }

        public static ReadOnlyMemory<Byte> ReadJsonBytes(Stream stream)
        {
            Guard.NotNull(stream, nameof(stream));

            bool binaryFile = BinarySerialization._Identify(stream);

            if (binaryFile)
            {
                var chunks = BinarySerialization.ReadBinaryFile(stream);

                return chunks[BinarySerialization.CHUNKJSON];
            }

            return stream.ReadBytesToEnd();
        }

        #endregion
    }
}
