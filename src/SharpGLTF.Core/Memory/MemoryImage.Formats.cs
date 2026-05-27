using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using BYTES = System.ArraySegment<System.Byte>;
using LAZYBYTES = System.Lazy<System.ArraySegment<System.Byte>>;

namespace SharpGLTF.Memory
{
    /// <summary>
    /// Provides information related to an image file format.
    /// </summary>
    abstract class ImageFormatInfo
    {
        internal const string EMBEDDED_OCTET_STREAM = "data:application/octet-stream";
        internal const string EMBEDDED_GLTF_BUFFER = "data:application/gltf-buffer";

        public virtual bool RequiresExtension => true;

        public abstract string DisplayName { get; }

        public abstract string Extension { get; }

        public abstract string GetMime(bool embedded);

        public abstract bool IsFormat(IReadOnlyList<byte> header);

        public virtual void Verify(BYTES data, string paramName) { }
    }

    class PngFormatInfo : ImageFormatInfo
    {
        const string EMBEDDED_PNG_BUFFER = "data:image/png";
        const string MIME_PNG = "image/png";

        /// <summary>
        /// Represents a 4x4 white PNG image.
        /// </summary>
        private const string DEFAULT_PNG_IMAGE = "iVBORw0KGgoAAAANSUhEUgAAABAAAAAQCAIAAACQkWg2AAAACXBIWXMAAA7DAAAOwwHHb6hkAAAAHXpUWHRUaXRsZQAACJlzSU1LLM0pCUmtKCktSgUAKVIFt/VCuZ8AAAAoelRYdEF1dGhvcgAACJkLy0xOzStJVQhIzUtMSS1WcCzKTc1Lzy8BAG89CQyAoFAQAAAANElEQVQoz2O8cuUKAwxoa2vD2VevXsUqzsRAIqC9Bsb///8TdDey+CD0Awsx7h6NB5prAADPsx0VAB8VRQAAAABJRU5ErkJggg==";

        internal static Byte[] DefaultPngImage => Convert.FromBase64String(DEFAULT_PNG_IMAGE);

        public override bool RequiresExtension => false;

        public override string DisplayName => "PNG";

        public override string Extension => "png";

        public override string GetMime(bool embedded)
        {
            return embedded ? EMBEDDED_PNG_BUFFER : MIME_PNG;
        }

        public override bool IsFormat(IReadOnlyList<byte> data)
        {
            if (data.Count < 4) return false;

            if (data[0] != 0x89) return false;
            if (data[1] != 0x50) return false;
            if (data[2] != 0x4e) return false;
            if (data[3] != 0x47) return false;

            return true;
        }
    }

    class JpgFormatInfo : ImageFormatInfo
    {
        const string EMBEDDED_JPEG_BUFFER = "data:image/jpeg";
        const string MIME_JPG = "image/jpeg";

        public override bool RequiresExtension => false;

        public override string DisplayName => "JPEG";

        public override string Extension => "jpg";

        public override string GetMime(bool embedded)
        {
            return embedded ? EMBEDDED_JPEG_BUFFER : MIME_JPG;
        }

        public override bool IsFormat(IReadOnlyList<byte> data)
        {
            if (data.Count < 2) return false;

            if (data[0] != 0xff) return false;
            if (data[1] != 0xd8) return false;

            return true;
        }
    }

    class DdsFormatInfo : ImageFormatInfo
    {
        const string EMBEDDED_DDS_BUFFER = "data:image/vnd-ms.dds";
        const string MIME_DDS = "image/vnd-ms.dds";

        public override string DisplayName => "DDS";

        public override string Extension => "dds";

        public override string GetMime(bool embedded)
        {
            return embedded ? EMBEDDED_DDS_BUFFER : MIME_DDS;
        }

        public override bool IsFormat(IReadOnlyList<byte> data)
        {
            if (data.Count < 4) return false;

            if (data[0] != 0x44) return false;
            if (data[1] != 0x44) return false;
            if (data[2] != 0x53) return false;
            if (data[3] != 0x20) return false;
            return true;
        }
    }

    class XnbFormatInfo : ImageFormatInfo
    {
        const string MIME_XNB = "image/vnd-ms.xnb";
        const string EMBEDDED_XNB_BUFFER = "data:image/vnd-ms.xnb";

        public override string DisplayName => "XNB";

        public override string Extension => "xnb";

        public override string GetMime(bool embedded)
        {
            return embedded ? EMBEDDED_XNB_BUFFER : MIME_XNB;
        }

        public override bool IsFormat(IReadOnlyList<byte> data)
        {
            if (data.Count < 3) return false;

            if (data[0] != 0x58) return false;
            if (data[1] != 0x4E) return false;
            if (data[2] != 0x42) return false;

            return true;
        }
    }

    class WebpFormatInfo : ImageFormatInfo
    {
        const string EMBEDDED_WEBP_BUFFER = "data:image/webp";
        const string MIME_WEBP = "image/webp";

        public override string DisplayName => "WEBP";

        public override string Extension => "webp";

        public override string GetMime(bool embedded)
        {
            return embedded ? EMBEDDED_WEBP_BUFFER : MIME_WEBP;
        }

        public override bool IsFormat(IReadOnlyList<byte> data)
        {
            if (data.Count < 12) return false;

            // RIFF
            if (data[0] != 0x52) return false;
            if (data[1] != 0x49) return false;
            if (data[2] != 0x46) return false;
            if (data[3] != 0x46) return false;

            // WEBP
            if (data[8] != 0x57) return false;
            if (data[9] != 0x45) return false;
            if (data[10] != 0x42) return false;
            if (data[11] != 0x50) return false;

            return true;
        }
    }

    class Ktx2FormatInfo : ImageFormatInfo
    {
        const string EMBEDDED_KTX2_BUFFER = "data:image/ktx2";
        const string MIME_KTX2 = "image/ktx2";
        public override string DisplayName => "KTX2";

        public override string Extension => "ktx2";

        public override string GetMime(bool embedded)
        {
            return embedded ? EMBEDDED_KTX2_BUFFER : MIME_KTX2;
        }

        public override bool IsFormat(IReadOnlyList<byte> data)
        {
            try
            {
                if (!Ktx2Header.TryGetHeader(data, out Ktx2Header header)) return false;
                return header.IsValidHeader;
            }
            catch { return false; }
        }

        public override void Verify(BYTES data, string paramName)
        {
            Ktx2Header.Verify(data, paramName);
        }
    }



    [System.Diagnostics.DebuggerDisplay("{PixelWidth}x{PixelHeight}x{PixelDepth}")]
    readonly struct Ktx2Header
    {
        // http://github.khronos.org/KTX-Specification/

        public readonly UInt64 Header0;
        public readonly UInt32 Header1;

        public readonly UInt32 VkFormat;
        public readonly UInt32 TypeSize;
        public readonly UInt32 PixelWidth;
        public readonly UInt32 PixelHeight;
        public readonly UInt32 PixelDepth;
        public readonly UInt32 LayerCount;
        public readonly UInt32 FaceCount;
        public readonly UInt32 LevelCount;
        public readonly UInt32 SupercompressionScheme;

        public bool IsValidHeader
        {
            get
            {
                if (Header0 != 0xbb30322058544BAb) return false;
                if (Header1 != 0x0A1A0A0D) return false;
                return true;
            }
        }

        public static unsafe bool TryGetHeader(IReadOnlyList<Byte> data, out Ktx2Header header)
        {
            if (!(data is Byte[] array)) array = data?.ToArray() ?? Array.Empty<Byte>();

            if (array.Length < sizeof(Ktx2Header)) { header = default; return false; }

            header = System.Runtime.InteropServices.MemoryMarshal.Cast<Byte, Ktx2Header>(array)[0];
            return true;
        }

        public static void Verify(IReadOnlyList<Byte> data, string paramName)
        {
            // https://github.com/KhronosGroup/glTF/tree/master/extensions/2.0/Khronos/KHR_texture_basisu#ktx-v2-images-with-basis-universal-supercompression

            Guard.IsTrue(TryGetHeader(data, out Ktx2Header header), paramName);

            // header must be valid
            Guard.IsTrue(header.IsValidHeader, paramName + ".Header");

            // pixelWidth and pixelHeight MUST be multiples of 4.
            Guard.MustBePositiveAndMultipleOf((int)header.PixelWidth, 4, $"{paramName}.{nameof(PixelWidth)}");
            Guard.MustBePositiveAndMultipleOf((int)header.PixelHeight, 4, $"{paramName}.{nameof(PixelHeight)}");

            // For 2D and cubemap textures, pixelDepth must be 0.
            Guard.MustBeEqualTo((int)header.PixelDepth, 0, $"{paramName}.{nameof(PixelDepth)}");

            Guard.MustBeLessThan((int)header.SupercompressionScheme, 3, $"{paramName}.{nameof(SupercompressionScheme)}");

            // TODO: more checks required
        }
    }
}
