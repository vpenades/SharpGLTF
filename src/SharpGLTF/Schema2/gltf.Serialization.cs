using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

using Newtonsoft.Json;

namespace SharpGLTF.Schema2
{
    using BYTES = ArraySegment<Byte>;
    using MODEL = ModelRoot;

    /// <summary>
    /// Callback used for loading associated files of current model.
    /// </summary>
    /// <param name="assetName">the asset relative path.</param>
    /// <returns>The file contents as a <see cref="byte"/> array.</returns>
    public delegate BYTES AssetReader(String assetName);

    /// <summary>
    /// Callback used for saving associated files of the current model.
    /// </summary>
    /// <param name="assetName">The asset relative path.</param>
    /// <param name="assetData">The file contents as a <see cref="byte"/> array.</param>
    public delegate void AssetWriter(String assetName, BYTES assetData);

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
    }

    public enum ImageWriteMode
    {
        SatelliteFile,
        BufferView,
        Embedded
    }

    /// <summary>
    /// Configuration settings for writing model files.
    /// </summary>
    public class WriteSettings
    {
        #region lifecycle

        internal static WriteSettings ForText(string filePath)
        {
            Guard.FilePathMustBeValid(filePath, nameof(filePath));

            var dir = Path.GetDirectoryName(filePath);

            var settings = new WriteSettings
            {
                BinaryMode = false,
                ImageWriting = ImageWriteMode.SatelliteFile,
                MergeBuffers = true,
                JsonFormatting = Formatting.Indented,

                FileWriter = (fn, d) => File.WriteAllBytes(Path.Combine(dir, fn), d.ToArray())
            };

            return settings;
        }

        internal static WriteSettings ForText(Dictionary<string, BYTES> dict)
        {
            var settings = new WriteSettings()
            {
                BinaryMode = false,
                ImageWriting = ImageWriteMode.SatelliteFile,
                MergeBuffers = false,
                JsonFormatting = Formatting.None,

                FileWriter = (fn, buff) => dict[fn] = buff
            };

            return settings;
        }

        internal static WriteSettings ForBinary(string filePath)
        {
            Guard.FilePathMustBeValid(filePath, nameof(filePath));

            var dir = Path.GetDirectoryName(filePath);

            var settings = new WriteSettings
            {
                BinaryMode = true,
                ImageWriting = ImageWriteMode.BufferView,
                MergeBuffers = true,
                JsonFormatting = Formatting.None,

                FileWriter = (fn, d) => File.WriteAllBytes(Path.Combine(dir, fn), d.ToArray())
            };

            return settings;
        }

        internal static WriteSettings ForBinary(Stream stream)
        {
            Guard.NotNull(stream, nameof(stream));
            Guard.IsTrue(stream.CanWrite, nameof(stream));

            var settings = new WriteSettings
            {
                BinaryMode = true,
                ImageWriting = ImageWriteMode.BufferView,
                MergeBuffers = true,
                JsonFormatting = Formatting.None,

                FileWriter = (fn, d) => stream.Write(d.Array, d.Offset, d.Count)
            };

            return settings;
        }

        private WriteSettings() { }

        #endregion

        #region data

        /// <summary>
        /// Gets or sets a value indicating whether to write a GLTF or a GLB file
        /// </summary>
        public Boolean BinaryMode { get; set; }

        /// <summary>
        /// Gets or sets a value indicating how to write the images of the model
        /// </summary>
        public ImageWriteMode ImageWriting { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to merge all the buffers in <see cref="MODEL.LogicalBuffers"/> into a single buffer.
        /// </summary>
        public Boolean MergeBuffers { get; set; }

        /// <summary>
        /// Gets or sets a value indicating how to format the JSON document of the glTF
        /// </summary>
        public Formatting JsonFormatting { get; set; }

        /// <summary>
        /// Gets or sets the <see cref="AssetWriter"/> delegate used to write satellite files.
        /// </summary>
        public AssetWriter FileWriter { get; set; }

        internal bool _NoCloneWatchdog = false;

        #endregion

        #region API

        /// <summary>
        /// Prepares the model for writing with the appropiate settings, cloning it if neccesary.
        /// </summary>
        /// <param name="model">The source <see cref="MODEL"/> instance.</param>
        /// <returns>The source <see cref="MODEL"/> instance, or a cloned and modified instance if current settings required it.</returns>
        internal MODEL FilterModel(MODEL model)
        {
            Guard.NotNull(model,nameof(model));

            var needsMergeBuffers = (this.MergeBuffers | this.BinaryMode) && model.LogicalBuffers.Count > 1;

            var imagesAsBufferViews = model.LogicalImages.Count > 0 && this.ImageWriting == ImageWriteMode.BufferView;

            if (needsMergeBuffers | imagesAsBufferViews)
            {
                if (_NoCloneWatchdog) throw new InvalidOperationException($"Current settings require a model rewrite, but {nameof(MODEL.DeepClone)} is not allowed in the current context");
                model = model.DeepClone();
            }

            if (ImageWriting == ImageWriteMode.BufferView)
            {
                model.MergeImages();
                needsMergeBuffers |= this.MergeBuffers | this.BinaryMode;
            }

            if (needsMergeBuffers) model.MergeBuffers();

            return model;
        }

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
                settings.FileReader = key => string.IsNullOrEmpty(key) ? new BYTES(chunks[glb.CHUNKBIN]) : settings.FileReader.Invoke(key);
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

        private static MODEL _Read(TextReader textReader, ReadSettings settings)
        {
            Guard.NotNull(textReader, nameof(textReader));
            Guard.NotNull(settings, nameof(settings));

            using (var reader = new JsonTextReader(textReader))
            {
                var root = new MODEL();
                root.Deserialize(reader);

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
            var settings = WriteSettings.ForBinary(filePath);

            var name = Path.GetFileNameWithoutExtension(filePath);

            _Write(settings, name, this);
        }

        /// <summary>
        /// Writes this <see cref="MODEL"/> to a file in GLTF format.
        /// </summary>
        /// <param name="filePath">A valid file path to write to.</param>
        /// <param name="fmt">The formatting of the JSON document.</param>
        /// <remarks>
        /// Satellite files like buffers and images are also saved with the file name formatted as "FILE_{Index}.EXT".
        /// </remarks>
        public void SaveGLTF(string filePath, Formatting fmt = Formatting.None)
        {
            var settings = WriteSettings.ForText(filePath);

            settings.JsonFormatting = fmt;

            var name = Path.GetFileNameWithoutExtension(filePath);

            _Write(settings, name, this);
        }

        /// <summary>
        /// Writes this <see cref="MODEL"/> to a dictionary where every key is an individual file
        /// </summary>
        /// <param name="fileName">the base name to use for the dictionary keys</param>
        /// <returns>a dictionary instance.</returns>
        public Dictionary<String, BYTES> WriteToDictionary(string fileName)
        {
            var dict = new Dictionary<string, BYTES>();

            var settings = WriteSettings.ForText(dict);

            // WriteToDictionary is usually called by DeepClone, so allowing a setting that
            // would imply cloning the model would cause an infine loop and a StackOverflow
            settings._NoCloneWatchdog = true;

            _Write(settings, fileName, this);

            return dict;
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
                _WriteJSON(ss, fmt);
                return ss.ToString();
            }
        }

        /// <summary>
        /// Writes this <see cref="MODEL"/> to a <see cref="byte"/> array in GLB format.
        /// </summary>
        /// <returns>A <see cref="byte"/> array containing a GLB file.</returns>
        public BYTES WriteGLB()
        {
            using (var m = new MemoryStream())
            {
                WriteGLB(m);

                return m.ToArraySegment();
            }
        }

        /// <summary>
        /// Writes this <see cref="MODEL"/> to a <see cref="Stream"/> in GLB format.
        /// </summary>
        /// <param name="stream">A <see cref="Stream"/> open for writing.</param>
        public void WriteGLB(Stream stream)
        {
            Guard.NotNull(stream, nameof(stream));
            Guard.IsTrue(stream.CanWrite, nameof(stream));

            var settings = WriteSettings.ForBinary(stream);

            _Write(settings, "model", this);
        }

        /// <summary>
        /// Writes this <see cref="MODEL"/> to the asset writer in <see cref="WriteSettings"/> configuration.
        /// </summary>
        /// <param name="settings">A <see cref="WriteSettings"/> to use to write the files.</param>
        /// <param name="baseName">The base name to use for asset files.</param>
        /// <remarks>
        /// Satellite files like buffers and images are also written with the file name formatted as "FILE_{Index}.EXT".
        /// </remarks>
        public void Write(WriteSettings settings, string baseName)
        {
            _Write(settings, baseName, this);
        }

        private void _WriteJSON(TextWriter sw, Formatting fmt)
        {
            using (var writer = new JsonTextWriter(sw))
            {
                writer.Formatting = fmt;

                this.Serialize(writer);
            }
        }

        private static void _Write(WriteSettings settings, string baseName, MODEL model)
        {
            Guard.NotNull(settings, nameof(settings));
            Guard.NotNullOrEmpty(baseName, nameof(baseName));
            Guard.NotNull(model,nameof(model));

            model = settings.FilterModel(model);

            if (settings.BinaryMode)
            {
                var ex = glb.IsBinaryCompatible(model);
                if (ex != null) throw ex;

                // setup all buffers to be written internally
                for (int i = 0; i < model._buffers.Count; ++i)
                {
                    var buffer = model._buffers[i];
                    buffer._WriteToInternal();
                }
            }
            else
            {
                // setup all buffers to be written as satellite files
                for (int i = 0; i < model._buffers.Count; ++i)
                {
                    var buffer = model._buffers[i];
                    var bname = model._buffers.Count != 1 ? $"{baseName}_{i}.bin" : $"{baseName}.bin";
                    buffer._WriteToSatellite(settings.FileWriter, bname);
                }
            }

            // setup all images to be written to the appropiate location.
            for (int i = 0; i < model._images.Count; ++i)
            {
                var image = model._images[i];
                var iname = model._images.Count != 1 ? $"{baseName}_{i}" : $"{baseName}";
                if (settings.ImageWriting != ImageWriteMode.SatelliteFile) image._WriteToInternal();
                else image._WriteToSatellite(settings.FileWriter, iname);
            }

            using (var m = new MemoryStream())
            {
                if (settings.BinaryMode)
                {
                    using (var w = new BinaryWriter(m))
                    {
                        glb.WriteBinaryModel(w, model);
                    }

                    settings.FileWriter($"{baseName}.glb", m.ToArraySegment());
                }
                else
                {
                    using (var w = new StreamWriter(m))
                    {
                        model._WriteJSON(w, settings.JsonFormatting);
                    }

                    settings.FileWriter($"{baseName}.gltf", m.ToArraySegment());
                }
            }

            foreach (var b in model._buffers) b._ClearAfterWrite();
            foreach (var i in model._images) i._ClearAfterWrite();
        }

        #endregion
    }
}
