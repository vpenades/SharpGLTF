using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

using Newtonsoft.Json;

namespace glTF2Sharp.Schema2
{
    using ROOT = ModelRoot;

    public delegate Byte[] AssetReader(String assetName);

    public delegate void AssetWriter(String assetName, Byte[] assetData);

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

        public static ROOT Load(string filePath)
        {
            Guard.FilePathMustExist(filePath, nameof(filePath));

            var settings = new ReadSettings(filePath);

            using (var s = File.OpenRead(filePath))
            {
                return Read(s, settings);
            }
        }

        public static ROOT ParseGLB(Byte[] glb)
        {
            Guard.NotNull(glb, nameof(glb));

            using (var m = new MemoryStream(glb))
            {
                return ReadGLB(m, new ReadSettings());
            }
        }

        /// <summary>
        /// Reads a <code>Schema.Gltf</code> model from a stream
        /// </summary>
        /// <param name="stream">Readable stream to a gltf/glb model</param>
        /// <returns><code>Schema.Gltf</code> model</returns>
        public static ROOT Read(Stream stream, ReadSettings settings)
        {
            bool binaryFile = glb._Identify(stream);

            if (binaryFile) return ReadGLB(stream, settings);
            else            return ReadGLTF(stream, settings);
        }

        public static ROOT ReadGLTF(Stream stream, ReadSettings settings)
        {
            Guard.NotNull(stream, nameof(stream));
            Guard.NotNull(settings, nameof(settings));

            string content = null;

            using (var streamReader = new StreamReader(stream))
            {
                content = streamReader.ReadToEnd();
            }

            return Parse(content, settings);
        }

        public static ROOT ReadGLB(Stream stream, ReadSettings settings)
        {
            Guard.NotNull(stream, nameof(stream));
            Guard.NotNull(settings, nameof(settings));

            var chunks = glb.ReadBinaryFile(stream);

            var dom = Encoding.UTF8.GetString(chunks[glb.CHUNKJSON]);

            if (chunks.ContainsKey(glb.CHUNKBIN))
            {
                settings.FileReader = key => string.IsNullOrEmpty(key) ? chunks[glb.CHUNKBIN] : settings.FileReader?.Invoke(key);
            }

            return Parse(dom, settings);
        }

        public static ROOT Parse(String jsonContent, ReadSettings settings)
        {
            Guard.NotNullOrEmpty(jsonContent, nameof(jsonContent));
            Guard.NotNull(settings, nameof(settings));

            using (var tr = new StringReader(jsonContent))
            {
                return _Read(tr, settings);
            }
        }

        private static ROOT _Read(StringReader textReader, ReadSettings settings)
        {
            Guard.NotNull(textReader, nameof(textReader));
            Guard.NotNull(settings, nameof(settings));

            using (var reader = new JsonTextReader(textReader))
            {
                var root = new ROOT();
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
        // which goes against good practices of not modyfing any file when it is being saved.
        // a possible solution would be to do a shallow copy of RootObject and update Buffers, BufferViews, etc
        // an issue that complicates things is that it requires to copy the extensions of root, buffers, etc

        public void SaveGLB(string filePath)
        {
            Guard.FilePathMustBeValid(filePath, nameof(filePath));

            var settings = new WriteSettings(filePath)
            {
                JSonFormatting = Formatting.None,
                BinaryMode = true
            };

            var name = Path.GetFileNameWithoutExtension(filePath);

            Write(name, settings);
        }

        public void SaveGLTF(string filePath, Formatting fmt = Formatting.None)
        {
            Guard.FilePathMustBeValid(filePath, nameof(filePath));

            var settings = new WriteSettings(filePath)
            {
                JSonFormatting = fmt,
                BinaryMode = false
            };

            var name = Path.GetFileNameWithoutExtension(filePath);

            Write(name, settings);
        }

        public void Write(string name, WriteSettings settings)
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
                    var bname = $"{name}.{i}.bin";
                    buffer._WriteToExternal(bname, settings.FileWriter);
                }
            }

            for (int i = 0; i < this._images.Count; ++i)
            {
                var image = this._images[i];
                var iname = $"{name}.{i}";
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

                    settings.FileWriter($"{name}.glb", m.ToArray());
                }
                else
                {
                    using (var w = new StreamWriter(m))
                    {
                        WriteJSON(w, settings.JSonFormatting);
                    }

                    settings.FileWriter($"{name}.gltf", m.ToArray());
                }
            }

            foreach (var b in this._buffers) b._ClearAfterWrite();
            foreach (var i in this._images) i._ClearAfterWrite();
        }

        public void WriteJSON(TextWriter sw, Formatting fmt)
        {
            using (var writer = new JsonTextWriter(sw))
            {
                writer.Formatting = fmt;

                this.Serialize(writer);
            }
        }

        public string GetJSON(Formatting fmt)
        {
            using (var ss = new StringWriter())
            {
                WriteJSON(ss, fmt);
                return ss.ToString();
            }
        }

        public Byte[] GetGLB()
        {
            using (var m = new MemoryStream())
            {
                var settings = new WriteSettings(m);

                Write("model", settings);

                return m.ToArray();
            }
        }

        #endregion
    }
}
