using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

using Newtonsoft.Json;

namespace SharpGLTF.Schema2
{
    using MODEL = ModelRoot;

    /// <summary>
    /// Callback used for loading associated files of current model.
    /// </summary>
    /// <param name="assetName">the asset relative path.</param>
    /// <returns>The file contents as a <see cref="byte"/> array.</returns>
    public delegate Byte[] AssetReader(String assetName);

    /// <summary>
    /// Callback used for saving associated files of the current model.
    /// </summary>
    /// <param name="assetName">The asset relative path.</param>
    /// <param name="assetData">The file contents as a <see cref="byte"/> array.</param>
    public delegate void AssetWriter(String assetName, Byte[] assetData);

    /// <summary>
    /// Configuration settings for reading model files.
    /// </summary>
    public class ReadSettings
    {
        public ReadSettings()
        {
        }

        internal ReadSettings(string filePath)
        {
            Guard.FilePathMustExist(filePath, nameof(filePath));

            var dir = Path.GetDirectoryName(filePath);

            FileReader = asset => File.ReadAllBytes(Path.Combine(dir, asset));
        }

        public AssetReader FileReader { get; set; }
    }

    /// <summary>
    /// Configuration settings for writing model files.
    /// </summary>
    public class WriteSettings
    {
        #region lifecycle

        internal WriteSettings(string filePath)
        {
            Guard.FilePathMustBeValid(filePath, nameof(filePath));

            var dir = Path.GetDirectoryName(filePath);

            this.FileWriter = (fn, d) => File.WriteAllBytes(Path.Combine(dir, fn), d);
        }

        internal WriteSettings(Func<string, Stream> fileWriter)
        {
            void assetWriter(string n, byte[] d)
            {
                using (var s = fileWriter(n))
                {
                    s.Write(d, 0, d.Length);
                }
            }

            this.FileWriter = assetWriter;
        }

        internal WriteSettings(MemoryStream memory)
        {
            JSonFormatting = Formatting.None;
            BinaryMode = true;
            EmbedImages = true;

            this.FileWriter = (fn, d) => memory.Write(d, 0, d.Length);
        }

        #endregion

        #region data

        public Boolean BinaryMode { get; set; }

        public Boolean EmbedImages { get; set; }

        public Formatting JSonFormatting { get; set; }

        public AssetWriter FileWriter { get; set; }

        #endregion
    }

    partial class ModelRoot
    {
        #region Read API

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
        public static MODEL ParseGLB(Byte[] glb)
        {
            Guard.NotNull(glb, nameof(glb));

            using (var m = new MemoryStream(glb))
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
                settings.FileReader = key => string.IsNullOrEmpty(key) ? chunks[glb.CHUNKBIN] : settings.FileReader?.Invoke(key);
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

        private static MODEL _Read(StringReader textReader, ReadSettings settings)
        {
            Guard.NotNull(textReader, nameof(textReader));
            Guard.NotNull(settings, nameof(settings));

            using (var reader = new JsonTextReader(textReader))
            {
                var root = new MODEL();
                root.DeserializeObject(reader);

                var ex = root.Validate().FirstOrDefault();
                if (ex != null) throw ex;

                foreach (var buffer in root._buffers)
                {
                    buffer._ResolveUri(settings.FileReader);
                }

                foreach (var image in root._images)
                {
                    image._ResolveUri(settings.FileReader);
                }

                return root;
            }
        }

        #endregion

        #region Write API

        // TODO: usually when we save the gltf file, we need to amend/fix several features,
        // which goes against good practices of not modifying any file when it is being saved.
        // a possible solution would be to do a shallow copy of RootObject and update Buffers, BufferViews, etc
        // an issue that complicates things is that it requires to copy the extensions of root, buffers, etc

        /// <summary>
        /// Writes this <see cref="MODEL"/> to a file in GLB format.
        /// </summary>
        /// <param name="filePath">A valid file path to write to.</param>
        public void SaveGLB(string filePath)
        {
            Guard.FilePathMustBeValid(filePath, nameof(filePath));

            var settings = new WriteSettings(filePath)
            {
                JSonFormatting = Formatting.None,
                BinaryMode = true,
                EmbedImages = true
            };

            var name = Path.GetFileNameWithoutExtension(filePath);

            Write(settings, name);
        }

        /// <summary>
        /// Writes this <see cref="MODEL"/> to a file in GLTF format.
        /// </summary>
        /// <param name="filePath">A valid file path to write to.</param>
        /// <param name="fmt">The formatting of the JSON document.</param>
        public void SaveGLTF(string filePath, Formatting fmt = Formatting.None)
        {
            Guard.FilePathMustBeValid(filePath, nameof(filePath));

            var settings = new WriteSettings(filePath)
            {
                JSonFormatting = fmt,
                BinaryMode = false
            };

            var name = Path.GetFileNameWithoutExtension(filePath);

            Write(settings, name);
        }

        /// <summary>
        /// Writes this <see cref="MODEL"/> to the asset writer in <see cref="WriteSettings"/> configuration.
        /// </summary>
        /// <param name="settings">A <see cref="WriteSettings"/> to use to write the files.</param>
        /// <param name="baseName">The base name to use for asset files.</param>
        public void Write(WriteSettings settings, string baseName)
        {
            if (settings.BinaryMode)
            {
                var ex = glb.IsBinaryCompatible(this); if (ex != null) throw ex;

                for (int i = 0; i < this._buffers.Count; ++i)
                {
                    var buffer = this._buffers[i];
                    buffer._WriteToInternal();
                }
            }
            else
            {
                for (int i = 0; i < this._buffers.Count; ++i)
                {
                    var buffer = this._buffers[i];
                    var bname = this._buffers.Count != 1 ? $"{baseName}_{i}.bin" : $"{baseName}.bin";
                    buffer._WriteToExternal(bname, settings.FileWriter);
                }
            }

            for (int i = 0; i < this._images.Count; ++i)
            {
                var image = this._images[i];
                var iname = this._images.Count != 1 ? $"{baseName}_{i}" : $"{baseName}";
                if (settings.EmbedImages) image._EmbedAssets();
                else image._WriteExternalAssets(iname, settings.FileWriter);
            }

            using (var m = new MemoryStream())
            {
                if (settings.BinaryMode)
                {
                    using (var w = new BinaryWriter(m))
                    {
                        glb.WriteBinaryModel(w, this);
                    }

                    settings.FileWriter($"{baseName}.glb", m.ToArray());
                }
                else
                {
                    using (var w = new StreamWriter(m))
                    {
                        WriteJSON(w, settings.JSonFormatting);
                    }

                    settings.FileWriter($"{baseName}.gltf", m.ToArray());
                }
            }

            foreach (var b in this._buffers) b._ClearAfterWrite();
            foreach (var i in this._images) i._ClearAfterWrite();
        }

        /// <summary>
        /// Writes this <see cref="MODEL"/> JSON document to a <see cref="TextWriter"/>.
        /// </summary>
        /// <param name="sw">The target <see cref="TextWriter"/>.</param>
        /// <param name="fmt">The formatting of the JSON document.</param>
        public void WriteJSON(TextWriter sw, Formatting fmt)
        {
            using (var writer = new JsonTextWriter(sw))
            {
                writer.Formatting = fmt;

                this.Serialize(writer);
            }
        }

        /// <summary>
        /// Gets the JSON document of this <see cref="MODEL"/>.
        /// </summary>
        /// <param name="fmt">The formatting of the JSON document.</param>
        /// <returns>A JSON content.</returns>
        public string GetJSON(Formatting fmt)
        {
            using (var ss = new StringWriter())
            {
                WriteJSON(ss, fmt);
                return ss.ToString();
            }
        }

        /// <summary>
        /// Writes this <see cref="MODEL"/> to a <see cref="byte"/> array in GLB format.
        /// </summary>
        /// <returns>A <see cref="byte"/> array containing a GLB file.</returns>
        public Byte[] GetGLB()
        {
            using (var m = new MemoryStream())
            {
                var settings = new WriteSettings(m);

                Write(settings, "model");

                return m.ToArray();
            }
        }

        #endregion
    }
}
