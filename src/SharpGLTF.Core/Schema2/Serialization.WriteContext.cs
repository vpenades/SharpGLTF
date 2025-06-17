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

        public static WriteContext Create(FileWriterCallback fileCallback, Func<string, System.IO.Stream> streamWriteCallback = null)
        {
            Guard.NotNull(fileCallback, nameof(fileCallback));

            var context = new WriteContext(fileCallback, streamWriteCallback)
            {
                _UpdateSupportedExtensions = true
            };

            return context;
        }        

        public static WriteContext CreateFromDirectory(DirectoryInfo dinfo)
        {
            Guard.NotNull(dinfo, nameof(dinfo));
            Guard.MustExist(dinfo, nameof(dinfo));            

            void _writeBytes(string rawUri, BYTES data)
            {
                var path = Uri.UnescapeDataString(rawUri);
                path = Path.Combine(dinfo.FullName, path);

                using (var s = System.IO.File.Create(path))
                {
                    s.Write(data.Array, data.Offset, data.Count);
                }
            }

            System.IO.Stream _OpenStream(string rawUri)
            {
                var path = Uri.UnescapeDataString(rawUri);
                path = Path.Combine(dinfo.FullName, path);

                return System.IO.File.Create(path);
            }

            var context = Create(_writeBytes, _OpenStream);
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

        private WriteContext(FileWriterCallback byteWriteCallback, Func<string, System.IO.Stream> streamWriteCallback)
        {
            _ByteWriter = byteWriteCallback;
            _StreamWriter = streamWriteCallback;
        }

        #endregion

        #region data

        /// <summary>
        /// callback used to write named binary blogs to the current context
        /// </summary>
        private readonly FileWriterCallback _ByteWriter;

        /// <summary>
        /// alternate callback used to write directly to a file.
        /// </summary>
        /// <remarks>
        /// If this callback is null, <see cref="_ByteWriter"/> must be used as fallback.
        /// </remarks>
        private readonly Func<string, System.IO.Stream> _StreamWriter;        

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
            this._ByteWriter(fileName, data);
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
		/// <param name="name">The name to use for asset files. It is possible to specify a custom extension for the main file</param>
		/// <param name="model">The <see cref="MODEL"/> to write.</param>
		/// <remarks>
		/// The main file extension will be .gltf if none is provided.<br/>
		/// <br/>
		/// If the model has associated resources like binary assets and textures,<br/>
		/// these additional resources will be also written as associated files using the pattern:<br/>
		/// <br/>
		/// "<paramref name="name"/>.{Number}.bin|png|jpg|dds"<br/>
		/// where <paramref name="name"/> is used without extension.
		/// </remarks>
		public void WriteTextSchema2(string name, MODEL model)
        {
            Guard.NotNullOrEmpty(name, nameof(name));
            Guard.FilePathMustBeValid(name, nameof(name));
            if (System.IO.Path.IsPathRooted(name)) throw new ArgumentException("path must be relative", nameof(name));

            Guard.NotNull(model, nameof(model));

            // merge images when explicitly requested.
            var mergeImages = this.ImageWriting == ResourceWriteMode.BufferView;

            model = this._PreprocessSchema2(model, mergeImages, this.MergeBuffers, this.BuffersMaxSize);
            Guard.NotNull(model, nameof(model));

            var baseName = Path.GetFileNameWithoutExtension(name);

            model._PrepareBuffersForSatelliteWriting(this, baseName);

            model._PrepareImagesForWriting(this, baseName, false, ResourceWriteMode.SatelliteFile);

            _ValidateBeforeWriting(model);

            string finalName = Path.HasExtension(name) ? name : $"{name}.gltf";

            if (_StreamWriter != null) // write directly to a stream
            {                
                using (var f = _StreamWriter.Invoke(finalName))
                {
                    model._WriteJSON(f, this.JsonOptions, this.JsonPostprocessor);
                }
            }
            else // write to bytes
            {
                using (var m = new MemoryStream())
                {
                    model._WriteJSON(m, this.JsonOptions, this.JsonPostprocessor);
                    WriteAllBytesToEnd(finalName, m.ToArraySegment());
                }
            }

            model._AfterWriting();
        }

		/// <summary>
		/// Writes <paramref name="model"/> to this context using the GLB binary container.
		/// </summary>
		/// <param name="name">The name to use for asset files. It is possible to specify a custom extension for the main file</param>
		/// <param name="model">The <see cref="MODEL"/> to write.</param>
		/// <remarks>
		/// The file extension will be .glb if none is provided.
		/// </remarks>
		public void WriteBinarySchema2(string name, MODEL model)
        {
            Guard.NotNullOrEmpty(name, nameof(name));
            Guard.FilePathMustBeValid(name, nameof(name));
            if (System.IO.Path.IsPathRooted(name)) throw new ArgumentException("path must be relative", nameof(name));

            Guard.NotNull(model, nameof(model));            

            // merge images for all cases except for satellite files
            var mergeImages = this.ImageWriting != ResourceWriteMode.SatelliteFile;

            model = this._PreprocessSchema2(model, mergeImages, true, int.MaxValue);
            Guard.NotNull(model, nameof(model));

            var ex = _BinarySerialization.IsBinaryCompatible(model);
            if (ex != null) throw ex;

            model._PrepareBuffersForInternalWriting();

            var baseName = Path.GetFileNameWithoutExtension(name);

            model._PrepareImagesForWriting(this, baseName, true, ResourceWriteMode.BufferView);

            _ValidateBeforeWriting(model);

            string finalName = Path.HasExtension(name) ? name : $"{name}.glb";

            if (_StreamWriter != null) // write to stream
            {
                using(var s = _StreamWriter.Invoke(finalName))
                {
                    using (var w = new BinaryWriter(s))
                    {
                        _BinarySerialization.WriteBinaryModel(w, model);
                    }
                }
            }
            else // write to bytes
            {
                using (var m = new MemoryStream())
                {
                    using (var w = new BinaryWriter(m))
                    {
                        _BinarySerialization.WriteBinaryModel(w, model);
                    }                    

                    WriteAllBytesToEnd(finalName, m.ToArraySegment());
                }
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
