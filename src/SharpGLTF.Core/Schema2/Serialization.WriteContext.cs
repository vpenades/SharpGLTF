using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using SharpGLTF.Memory;

using BYTES = System.ArraySegment<byte>;
using MODEL = SharpGLTF.Schema2.ModelRoot;

namespace SharpGLTF.Schema2
{
    /// <summary>
    /// Callback used for saving associated files of the current model.
    /// </summary>
    /// <param name="assetName">The asset relative path.</param>
    /// <param name="assetData">The file contents as a <see cref="byte"/> array.</param>
    public delegate void FileWriterCallback(String assetName, BYTES assetData);

    /// <summary>
    /// Callback to control the image writing behavior.
    /// </summary>
    /// <param name="context">The current model writing context.</param>
    /// <param name="assetName">The default gltf URI used to reference the image.</param>
    /// <param name="image">The image to write.</param>
    /// <returns>The final glTF URI. If it didn't change, return the value of <para name="assetName"/>.</returns>
    public delegate string ImageWriterCallback(WriteContext context, String assetName, MemoryImage image);

    /// <summary>
    /// Configuration settings for writing model files.
    /// </summary>
    public class WriteContext : WriteSettings
    {
        #region lifecycle

        public static WriteContext Create(FileWriterCallback fileCallback)
        {
            Guard.NotNull(fileCallback, nameof(fileCallback));

            var context = new WriteContext(fileCallback)
            {
                _UpdateSupportedExtensions = true
            };

            return context;
        }

        [Obsolete("Use CreateFromDirectory", true)]
        public static WriteContext CreateFromFile(string filePath)
        {
            Guard.FilePathMustBeValid(filePath, nameof(filePath));

            var finfo = new System.IO.FileInfo(filePath);

            return CreateFromDirectory(finfo.Directory);
        }

        public static WriteContext CreateFromDirectory(DirectoryInfo dinfo)
        {
            Guard.NotNull(dinfo, nameof(dinfo));
            Guard.MustExist(dinfo, nameof(dinfo));            

            void _saveFile(string rawUri, BYTES data)
            {
                var path = Uri.UnescapeDataString(rawUri);
                path = Path.Combine(dinfo.FullName, path);

                File.WriteAllBytes(path, data.ToUnderlayingArray());
            }

            var context = Create(_saveFile);
            context.ImageWriting = ResourceWriteMode.Default;
            context.JsonIndented = true;
            context.CurrentDirectory = dinfo;
            return context;
        }

        public static WriteContext CreateFromDictionary(IDictionary<string, BYTES> dict)
        {
            Guard.NotNull(dict, nameof(dict));

            var context = Create((rawUri, data) => dict[rawUri] = data);
            context.ImageWriting = ResourceWriteMode.SatelliteFile;
            context.MergeBuffers = false;
            context.JsonIndented = false;

            return context;
        }

        public static WriteContext CreateFromStream(Stream stream)
        {
            Guard.NotNull(stream, nameof(stream));
            Guard.IsTrue(stream.CanWrite, nameof(stream));

            var context = Create((fn, d) => stream.Write(d.Array, d.Offset, d.Count));
            context.ImageWriting = ResourceWriteMode.Default;
            context.MergeBuffers = true;
            context.JsonIndented = false;

            return context.WithBinarySettings();
        }

        public WriteContext WithTextSettings()
        {
            if (ImageWriting == ResourceWriteMode.Default) ImageWriting = ResourceWriteMode.SatelliteFile;
            if (ImageWriting == ResourceWriteMode.BufferView) ImageWriting = ResourceWriteMode.SatelliteFile;            

            return this;
        }

        public WriteContext WithBinarySettings()
        {
            // Binary settings should allow BufferView and SatelliteFile ImageWriting modes:

            if (ImageWriting == ResourceWriteMode.Default) ImageWriting = ResourceWriteMode.BufferView;
            if (ImageWriting == ResourceWriteMode.EmbeddedAsBase64) ImageWriting = ResourceWriteMode.BufferView;

            // merging buffers is mandatory for GLB since the format only supports a single buffer.
            MergeBuffers = true;

            // there's no point in writing an indented json that's going to be written into a binary file.
            JsonIndented = false;

            return this;
        }

        public WriteContext WithSettingsFrom(WriteSettings settings)
        {
            settings?.CopyTo(this);
            return this;
        }

        /// <summary>
        /// These settings are used exclusively by <see cref="MODEL.DeepClone"/>.
        /// </summary>
        /// <returns>A <see cref="WriteContext"/> instance to be used by <see cref="MODEL.DeepClone()"/></returns>
        internal WriteContext WithDeepCloneSettings()
        {
            _UpdateSupportedExtensions = false;
            _NoCloneWatchdog = true;
            MergeBuffers = false;

            return this;
        }

        private WriteContext(FileWriterCallback fileCallback)
        {
            _FileWriter = fileCallback;
        }

        #endregion

        #region data

        private readonly FileWriterCallback _FileWriter;

        #endregion

        #region properties

        public System.IO.DirectoryInfo CurrentDirectory { get; private set; }

        /// <summary>
        /// Gets a value indicating whether to scan the whole model for used extensions.
        /// </summary>
        internal Boolean _UpdateSupportedExtensions { get; private set; } = true;

        /// <summary>
        /// Gets a value indicating whether creating a defensive copy before serialization is not allowed.
        /// </summary>
        internal bool _NoCloneWatchdog { get; private set; }

        #endregion

        #region API

        public void WriteAllBytesToEnd(string fileName, BYTES data)
        {
            this._FileWriter(fileName, data);
        }

        public string WriteImage(string assetName, MemoryImage image)
        {
            var callback = this.ImageWriteCallback;
            if (callback == null) callback = (ctx, apath, img) => { ctx.WriteAllBytesToEnd(apath, img._GetBuffer()); return apath; };

            return callback(this, assetName, image);
        }

        /// <summary>
        /// Writes <paramref name="model"/> to this context using the glTF json container.
        /// </summary>
        /// <param name="baseName">The base name to use for asset files, without extension.</param>
        /// <param name="model">The <see cref="MODEL"/> to write.</param>
        /// <remarks>
        /// If the model has associated resources like binary assets and textures,<br/>
        /// these additional resources will be also written as associated files using the pattern:<br/>
        /// <br/>
        /// "<paramref name="baseName"/>.{Number}.bin|png|jpg|dds"
        /// </remarks>
        public void WriteTextSchema2(string baseName, MODEL model)
        {
            Guard.NotNullOrEmpty(baseName, nameof(baseName));
            Guard.FilePathMustBeValid(baseName, nameof(baseName));
            if (System.IO.Path.IsPathRooted(baseName)) throw new ArgumentException("path must be relative", nameof(baseName));

            Guard.NotNull(model, nameof(model));

            // merge images when explicitly requested.
            var mergeImages = this.ImageWriting == ResourceWriteMode.BufferView;

            model = this._PreprocessSchema2(model, mergeImages, this.MergeBuffers, this.BuffersMaxSize);
            Guard.NotNull(model, nameof(model));

            model._PrepareBuffersForSatelliteWriting(this, baseName);

            model._PrepareImagesForWriting(this, baseName, false, ResourceWriteMode.SatelliteFile);

            _ValidateBeforeWriting(model);

            using (var m = new MemoryStream())
            {
                model._WriteJSON(m, this.JsonOptions, this.JsonPostprocessor);

                WriteAllBytesToEnd($"{baseName}.gltf", m.ToArraySegment());
            }

            model._AfterWriting();
        }

        /// <summary>
        /// Writes <paramref name="model"/> to this context using the GLB binary container.
        /// </summary>
        /// <param name="baseName">The base name to use for asset files, without extension.</param>
        /// <param name="model">The <see cref="MODEL"/> to write.</param>
        public void WriteBinarySchema2(string baseName, MODEL model)
        {
            Guard.NotNullOrEmpty(baseName, nameof(baseName));
            Guard.FilePathMustBeValid(baseName, nameof(baseName));
            if (System.IO.Path.IsPathRooted(baseName)) throw new ArgumentException("path must be relative", nameof(baseName));

            Guard.NotNull(model, nameof(model));

            

            // merge images for all cases except for satellite files
            var mergeImages = this.ImageWriting != ResourceWriteMode.SatelliteFile;

            model = this._PreprocessSchema2(model, mergeImages, true, int.MaxValue);
            Guard.NotNull(model, nameof(model));

            var ex = _BinarySerialization.IsBinaryCompatible(model);
            if (ex != null) throw ex;

            model._PrepareBuffersForInternalWriting();

            model._PrepareImagesForWriting(this, baseName, true, ResourceWriteMode.BufferView);

            _ValidateBeforeWriting(model);

            using (var m = new MemoryStream())
            {
                using (var w = new BinaryWriter(m))
                {
                    _BinarySerialization.WriteBinaryModel(w, model);
                }

                WriteAllBytesToEnd($"{baseName}.glb", m.ToArraySegment());
            }

            model._AfterWriting();
        }

        #endregion

        #region core

        /// <summary>
        /// This needs to be called immediately before writing to json,
        /// but immediately after preprocessing and buffer setup, so the model can be correctly validated.
        /// </summary>
        /// <param name="model">The model to validate.</param>
        private void _ValidateBeforeWriting(MODEL model)
        {
            if (_NoCloneWatchdog) return;

            if (this.Validation == SharpGLTF.Validation.ValidationMode.Skip) return;

            var vcontext = new Validation.ValidationResult(model, this.Validation);

            model.ValidateReferences(vcontext.GetContext());
            var ex = vcontext.Errors.FirstOrDefault();
            if (ex != null) throw ex;

            model.ValidateContent(vcontext.GetContext());
            ex = vcontext.Errors.FirstOrDefault();
            if (ex != null) throw ex;
        }

        /// <summary>
        /// Prepares the model for writing with the appropiate settings, creating a defensive copy if neccesary.
        /// </summary>
        /// <param name="model">The source <see cref="MODEL"/> instance.</param>
        /// <param name="imagesAsBufferViews">true if images should be stored as buffer views.</param>
        /// <param name="mergeBuffers">true if it's required the model must have a single buffer.</param>
        /// <param name="buffersMaxSize">When merging buffers, the max buffer size</param>
        /// <returns>The source <see cref="MODEL"/> instance, or a cloned and modified instance if current settings required it.</returns>
        private MODEL _PreprocessSchema2(MODEL model, bool imagesAsBufferViews, bool mergeBuffers, int buffersMaxSize)
        {
            Guard.NotNull(model, nameof(model));

            foreach (var img in model.LogicalImages) if (!img._HasContent) throw new Validation.DataException(img, "Image Content is missing.");

            // check if we need to modify the model before saving it,
            // in order to create a defensive copy.

            if (model.LogicalImages.Count == 0) imagesAsBufferViews = false;
            if (model.LogicalBuffers.Count <= 1 && !imagesAsBufferViews) mergeBuffers = false;

            if (mergeBuffers | imagesAsBufferViews)
            {
                // cloning check is done to prevent cloning from entering in an infinite loop where each clone attempt triggers another clone request.
                if (_NoCloneWatchdog) throw new InvalidOperationException($"Current settings require creating a densive copy before model modification, but calling {nameof(MODEL.DeepClone)} is not allowed with the current settings.");

                model = model.DeepClone();
            }

            if (imagesAsBufferViews) model.MergeImages();
            if (mergeBuffers)
            {
                if (buffersMaxSize == int.MaxValue) model.MergeBuffers();
                else model.MergeBuffers(buffersMaxSize);
            }

            if (this._UpdateSupportedExtensions) model.UpdateExtensionsSupport();

            return model;
        }

        #endregion
    }
}
