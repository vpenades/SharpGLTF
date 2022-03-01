using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using SharpGLTF.Schema2;

namespace SharpGLTF.IO
{
    /// <summary>
    /// Represents a context to read gltf files from a Zip archive.
    /// </summary>
    public sealed class ZipReader : IDisposable
    {
        #region static API

        public static ModelRoot LoadModelFromZip(string zipPath, ReadSettings settings = null)
        {
            using (var zip = new ZipReader(zipPath))
            {
                return zip.LoadModel(settings);
            }
        }

        #endregion

        #region lifecycle

        public ZipReader(string zipPath, Encoding encoding = null)
        {
            _Archive = encoding == null
                ? System.IO.Compression.ZipFile.Open(zipPath, System.IO.Compression.ZipArchiveMode.Read)
                : System.IO.Compression.ZipFile.Open(zipPath, System.IO.Compression.ZipArchiveMode.Read, encoding);
        }

        public ZipReader(System.IO.Stream zipStream, bool leaveOpen = false, Encoding encoding = null)
        {
            _Archive = encoding == null
                ? new System.IO.Compression.ZipArchive(zipStream, System.IO.Compression.ZipArchiveMode.Read, leaveOpen)
                : new System.IO.Compression.ZipArchive(zipStream, System.IO.Compression.ZipArchiveMode.Read, leaveOpen, encoding);
        }

        public void Dispose()
        {
            System.Threading.Interlocked.Exchange(ref _Archive, null)?.Dispose();
        }

        #endregion

        #region data

        private System.IO.Compression.ZipArchive _Archive;

        #endregion

        #region properties

        public IEnumerable<string> ModelFiles => _GetEntries().Select(item => item.FullName);

        #endregion

        #region API

        private IEnumerable<System.IO.Compression.ZipArchiveEntry> _GetEntries()
        {
            return _Archive
                .Entries
                .Where(item => item.FullName.EndsWith(".gltf", StringComparison.OrdinalIgnoreCase) || item.FullName.EndsWith(".glb", StringComparison.OrdinalIgnoreCase))
                .OrderBy(item => item.FullName);
        }

        public ModelRoot LoadModel(ReadSettings settings = null)
        {
            var gltfFile = ModelFiles.First();
            return this.LoadModel(gltfFile, settings);
        }

        public ModelRoot LoadModel(string gltfFile, ReadSettings settings = null)
        {
            var context = ReadContext
                .Create(_ReadAsset)
                .WithSettingsFrom(settings);

            return context.ReadSchema2(gltfFile);
        }

        private ArraySegment<Byte> _ReadAsset(string rawUri)
        {
            var filePath = Uri.UnescapeDataString(rawUri);

            System.IO.Compression.ZipArchiveEntry entry = _FindEntry(filePath);

            using (var s = entry.Open())
            {
                using (var m = new System.IO.MemoryStream())
                {
                    s.CopyTo(m);

                    if (m.TryGetBuffer(out ArraySegment<Byte> data)) return data;
                    else return new ArraySegment<byte>(m.ToArray());
                }
            }
        }

        private System.IO.Compression.ZipArchiveEntry _FindEntry(string filePath)
        {
            var entry = _Archive.Entries.FirstOrDefault(item => item.FullName.Equals(filePath, StringComparison.OrdinalIgnoreCase) );
            if (entry == null) throw new System.IO.FileNotFoundException(filePath);
            return entry;
        }

        #endregion
    }

    /// <summary>
    /// Represents a context to write gltf files to a Zip archive.
    /// </summary>
    public sealed class ZipWriter : IDisposable
    {
        #region lifecycle

        public ZipWriter(string zipPath, Encoding encoding = null)
        {
            _Archive = encoding == null
                ? System.IO.Compression.ZipFile.Open(zipPath, System.IO.Compression.ZipArchiveMode.Create)
                : System.IO.Compression.ZipFile.Open(zipPath, System.IO.Compression.ZipArchiveMode.Create, encoding);
        }

        public ZipWriter(System.IO.Stream zipStream, bool leaveOpen = false, Encoding encoding = null)
        {
            _Archive = encoding == null
                ? new System.IO.Compression.ZipArchive(zipStream, System.IO.Compression.ZipArchiveMode.Create, leaveOpen)
                : new System.IO.Compression.ZipArchive(zipStream, System.IO.Compression.ZipArchiveMode.Create, leaveOpen, encoding);
        }

        public void Dispose()
        {
            System.Threading.Interlocked.Exchange(ref _Archive, null)?.Dispose();
        }

        #endregion

        #region data

        private System.IO.Compression.ZipArchive _Archive;

        #endregion

        #region API

        public void AddModel(string filePath, ModelRoot model, WriteSettings settings = null)
        {
            Guard.NotNullOrEmpty(filePath, nameof(filePath));
            Guard.NotNull(model, nameof(model));

            var baseName = System.IO.Path.GetFileNameWithoutExtension(filePath);

            bool isGltfExtension = filePath.EndsWith(".GLTF", StringComparison.OrdinalIgnoreCase);

            var context = WriteContext.Create(_WriteAsset);
            if (isGltfExtension) context.WithTextSettings();
            else context.WithBinarySettings();

            settings?.CopyTo(context);

            if (isGltfExtension) context.WriteTextSchema2(baseName, model);
            else context.WriteBinarySchema2(baseName, model);
        }

        private void _WriteAsset(string filePath, ArraySegment<Byte> bytes)
        {
            // TODO: should check for already existing assets with the same name, and same content.

            var entry = _Archive.CreateEntry(filePath);

            using (var s = entry.Open())
            {
                s.Write(bytes.Array, bytes.Offset, bytes.Count);
            }
        }

        #endregion
    }
}
