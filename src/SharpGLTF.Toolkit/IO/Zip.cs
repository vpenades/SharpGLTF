using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using SharpGLTF.Schema2;

namespace SharpGLTF.IO
{
    public sealed class ZipReader : IDisposable
    {
        #region lifecycle

        public ZipReader(string zipPath)
        {
            _Archive = System.IO.Compression.ZipFile.Open(zipPath, System.IO.Compression.ZipArchiveMode.Read);
        }

        public void Dispose()
        {
            _Archive?.Dispose();
            _Archive = null;
        }

        public static ModelRoot LoadSchema2(string zipPath, ReadSettings settings = null)
        {
            using (var zip = new ZipReader(zipPath))
            {
                return zip.LoadSchema2(settings);
            }
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

        public ModelRoot LoadSchema2(ReadSettings settings = null)
        {
            var gltfFile = ModelFiles.First();
            return this._LoadSchema2(gltfFile, settings);
        }

        private ModelRoot _LoadSchema2(string gltfFile, ReadSettings settings = null)
        {
            var context = ReadContext.Create(_ReadAsset);

            settings?.CopyTo(context);

            using (var m = new System.IO.MemoryStream())
            {
                using (var s = _Archive.GetEntry(gltfFile).Open())
                {
                    s.CopyTo(m);
                }

                m.Position = 0;

                return context.ReadSchema2(m);
            }
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

    public sealed class ZipWriter : IDisposable
    {
        #region lifecycle

        public ZipWriter(string zipPath)
        {
            _Archive = System.IO.Compression.ZipFile.Open(zipPath, System.IO.Compression.ZipArchiveMode.Create);
        }

        public void Dispose()
        {
            _Archive?.Dispose();
            _Archive = null;
        }

        #endregion

        #region data

        private System.IO.Compression.ZipArchive _Archive;

        #endregion

        #region API

        public void AddModel(string filePath, ModelRoot model, WriteSettings settings = null)
        {
            Guard.NotNullOrEmpty(filePath, nameof(filePath));

            var baseName = System.IO.Path.GetFileNameWithoutExtension(filePath);

            bool isGltfExtension = filePath
                .ToLower(System.Globalization.CultureInfo.InvariantCulture)
                .EndsWith(".gltf", StringComparison.OrdinalIgnoreCase);

            var context = WriteContext.Create(_WriteAsset);

            if (!isGltfExtension) context.WithBinarySettings();

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
