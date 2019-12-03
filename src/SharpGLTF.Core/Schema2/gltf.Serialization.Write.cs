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
    /// Determines how resources are written.
    /// </summary>
    public enum ResourceWriteMode
    {
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

    /// <summary>
    /// Callback used for saving associated files of the current model.
    /// </summary>
    /// <param name="assetName">The asset relative path.</param>
    /// <param name="assetData">The file contents as a <see cref="byte"/> array.</param>
    public delegate void AssetWriter(String assetName, BYTES assetData);

    /// <summary>
    /// Configuration settings for writing model files.
    /// </summary>
    public class WriteContext
    {
        #region lifecycle

        /// <summary>
        /// These settings are used exclusively by <see cref="MODEL.DeepClone"/>.
        /// </summary>
        /// <param name="dict">The dictionary where the model will be stored</param>
        /// <returns>The settings to use with <see cref="MODEL.Write(WriteContext, string)"/></returns>
        internal static WriteContext ForDeepClone(Dictionary<string, BYTES> dict)
        {
            var settings = new WriteContext()
            {
                BinaryMode = false,
                ImageWriting = ResourceWriteMode.SatelliteFile,
                MergeBuffers = false,
                JsonFormatting = Formatting.None,
                _UpdateSupportedExtensions = false,
                _NoCloneWatchdog = true,

                FileWriter = (fn, buff) => dict[fn] = buff
            };

            return settings;
        }

        internal static WriteContext ForText(string filePath)
        {
            Guard.FilePathMustBeValid(filePath, nameof(filePath));

            var dir = Path.GetDirectoryName(filePath);

            var settings = new WriteContext
            {
                BinaryMode = false,
                ImageWriting = ResourceWriteMode.SatelliteFile,
                MergeBuffers = true,
                JsonFormatting = Formatting.Indented,
                _UpdateSupportedExtensions = true,

                FileWriter = (fn, d) => File.WriteAllBytes(Path.Combine(dir, fn), d.ToArray())
            };

            return settings;
        }

        internal static WriteContext ForText(Dictionary<string, BYTES> dict)
        {
            var settings = new WriteContext()
            {
                BinaryMode = false,
                ImageWriting = ResourceWriteMode.SatelliteFile,
                MergeBuffers = false,
                JsonFormatting = Formatting.None,
                _UpdateSupportedExtensions = true,

                FileWriter = (fn, buff) => dict[fn] = buff
            };

            return settings;
        }

        internal static WriteContext ForBinary(string filePath)
        {
            Guard.FilePathMustBeValid(filePath, nameof(filePath));

            var dir = Path.GetDirectoryName(filePath);

            var settings = new WriteContext
            {
                BinaryMode = true,
                ImageWriting = ResourceWriteMode.BufferView,
                MergeBuffers = true,
                JsonFormatting = Formatting.None,
                _UpdateSupportedExtensions = true,

                FileWriter = (fn, d) => File.WriteAllBytes(Path.Combine(dir, fn), d.ToArray())
            };

            return settings;
        }

        internal static WriteContext ForBinary(Stream stream)
        {
            Guard.NotNull(stream, nameof(stream));
            Guard.IsTrue(stream.CanWrite, nameof(stream));

            var settings = new WriteContext
            {
                BinaryMode = true,
                ImageWriting = ResourceWriteMode.BufferView,
                MergeBuffers = true,
                JsonFormatting = Formatting.None,
                _UpdateSupportedExtensions = true,

                FileWriter = (fn, d) => stream.Write(d.Array, d.Offset, d.Count)
            };

            return settings;
        }
        
        #endregion

        #region data

        /// <summary>
        /// Gets or sets a value indicating whether to write a GLTF or a GLB file.
        /// </summary>
        public Boolean BinaryMode { get; set; }

        /// <summary>
        /// Gets or sets a value indicating how to write the images of the model.
        /// </summary>
        public ResourceWriteMode ImageWriting { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to merge all the buffers in <see cref="MODEL.LogicalBuffers"/> into a single buffer.
        /// </summary>
        public Boolean MergeBuffers { get; set; }

        /// <summary>
        /// Gets or sets a value indicating how to format the JSON document of the glTF.
        /// </summary>
        public Formatting JsonFormatting { get; set; }

        /// <summary>
        /// Gets or sets the <see cref="AssetWriter"/> delegate used to write satellite files.
        /// </summary>
        public AssetWriter FileWriter { get; set; }

        /// <summary>
        /// Gets a value indicating whether to scan the whole model for used extensions.
        /// </summary>
        internal Boolean _UpdateSupportedExtensions { get; private set; } = true;

        /// <summary>
        /// Gets a value indicating whether creating a defensive copy before serialization is not allowed.
        /// </summary>
        internal bool _NoCloneWatchdog { get; private set; } = false;

        #endregion

        #region API

        /// <summary>
        /// Prepares the model for writing with the appropiate settings, creating a defensive copy if neccesary.
        /// </summary>
        /// <param name="model">The source <see cref="MODEL"/> instance.</param>
        /// <returns>The source <see cref="MODEL"/> instance, or a cloned and modified instance if current settings required it.</returns>
        internal MODEL FilterModel(MODEL model)
        {
            Guard.NotNull(model, nameof(model));

            // check if we need to modify the model before saving it,
            // in order to create a defensive copy.

            var needsMergeBuffers = (this.MergeBuffers | this.BinaryMode) && model.LogicalBuffers.Count > 1;

            var imagesAsBufferViews = model.LogicalImages.Count > 0 && this.ImageWriting == ResourceWriteMode.BufferView;

            if (needsMergeBuffers | imagesAsBufferViews)
            {
                if (_NoCloneWatchdog) throw new InvalidOperationException($"Current settings require creating a densive copy before model modification, but calling {nameof(MODEL.DeepClone)} is not allowed with the current settings.");
                model = model.DeepClone();
            }

            if (ImageWriting == ResourceWriteMode.BufferView)
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
        /// <summary>
        /// Writes this <see cref="MODEL"/> to a file in GLTF or GLB based on the extension of <paramref name="filePath"/>.
        /// </summary>
        /// <param name="filePath">A valid file path to write to.</param>
        public void Save(string filePath)
        {
            Guard.FilePathMustBeValid(filePath, nameof(filePath));

            bool isGltfExtension = filePath
                .ToLower(System.Globalization.CultureInfo.InvariantCulture)
                .EndsWith(".gltf", StringComparison.OrdinalIgnoreCase);

            if (isGltfExtension) SaveGLTF(filePath);
            else SaveGLB(filePath);
        }

        /// <summary>
        /// Writes this <see cref="MODEL"/> to a file in GLB format.
        /// </summary>
        /// <param name="filePath">A valid file path to write to.</param>
        public void SaveGLB(string filePath)
        {
            Guard.FilePathMustBeValid(filePath, nameof(filePath));

            var settings = WriteContext.ForBinary(filePath);

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
            Guard.FilePathMustBeValid(filePath, nameof(filePath));

            var settings = WriteContext.ForText(filePath);

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

            var settings = WriteContext.ForText(dict);

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

            var settings = WriteContext.ForBinary(stream);

            _Write(settings, "model", this);
        }

        /// <summary>
        /// Writes this <see cref="MODEL"/> to the asset writer in <see cref="WriteContext"/> configuration.
        /// </summary>
        /// <param name="settings">A <see cref="WriteContext"/> to use to write the files.</param>
        /// <param name="baseName">The base name to use for asset files.</param>
        /// <remarks>
        /// Satellite files like buffers and images are also written with the file name formatted as "FILE_{Index}.EXT".
        /// </remarks>
        public void Write(WriteContext settings, string baseName)
        {
            Guard.NotNull(settings, nameof(settings));
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

        private static void _Write(WriteContext settings, string baseName, MODEL model)
        {
            Guard.NotNull(settings, nameof(settings));
            Guard.NotNullOrEmpty(baseName, nameof(baseName));
            Guard.NotNull(model, nameof(model));

            model = settings.FilterModel(model);

            foreach (var img in model._images) if (!img._HasContent) throw new Validation.DataException(img, "Content is missing.");

            if (settings._UpdateSupportedExtensions) model.UpdateExtensionsSupport();

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

                if (settings.ImageWriting != ResourceWriteMode.SatelliteFile)
                {
                    image._WriteToInternal();
                }
                else
                {
                    var iname = model._images.Count != 1 ? $"{baseName}_{i}" : $"{baseName}";
                    image._WriteToSatellite(settings.FileWriter, iname);
                }
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
    }
}
