using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;

using static System.FormattableString;

namespace SharpGLTF.IO
{
    using BYTES = ArraySegment<Byte>;

    using POSITION = Geometry.VertexTypes.VertexPositionNormal;
    using TEXCOORD = Geometry.VertexTypes.VertexTexture1;

    using VERTEX = ValueTuple<Geometry.VertexTypes.VertexPositionNormal, Geometry.VertexTypes.VertexTexture1>;

    /// <summary>
    /// Tiny wavefront object writer
    /// </summary>
    /// <see href="https://www.fileformat.info/format/wavefrontobj/egff.htm"/>
    class WavefrontWriter
    {
        #region data

        public struct Material
        {
            public Vector4 DiffuseColor;
            public BYTES DiffuseTexture;
        }

        private readonly Geometry.MeshBuilder<Material, POSITION, TEXCOORD> _Mesh = new Geometry.MeshBuilder<Material, POSITION, TEXCOORD>();

        #endregion

        #region API

        public void AddTriangle(Material material, VERTEX a, VERTEX b, VERTEX c)
        {
            _Mesh.UsePrimitive(material).AddTriangle(a, b, c);
        }

        public void WriteFiles(string filePath)
        {
            var dir = System.IO.Path.GetDirectoryName(filePath);

            var files = GetFiles(System.IO.Path.GetFileNameWithoutExtension(filePath));

            foreach (var f in files)
            {
                var fpath = System.IO.Path.Combine(dir, f.Key);
                System.IO.File.WriteAllBytes(fpath, f.Value.ToArray());
            }
        }

        public IReadOnlyDictionary<String, BYTES> GetFiles(string baseName)
        {
            var files = new Dictionary<String, BYTES>();

            var materials = _WriteMaterials(files, baseName, _Mesh.Primitives.Select(item => item.Material));

            var geocontent = _GetGeometryContent(materials, baseName + ".mtl");

            _WriteTextContent(files, baseName + ".obj", geocontent);

            return files;
        }

        private IReadOnlyDictionary<Material, string> _WriteMaterials(IDictionary<String, BYTES> files, string baseName, IEnumerable<Material> materials)
        {
            // write all image files
            var images = materials.Select(item => item.DiffuseTexture);

            foreach (var img in images.Distinct())
            {
                if (img.Array == null) continue;

                var imgName = $"{baseName}_{files.Count}";

                if (_IsPng(img)) files[imgName + ".png"] = img;
                if (_IsJpeg(img)) files[imgName + ".jpg"] = img;
            }

            // write materials

            var mmap = new Dictionary<Material, string>();

            var sb = new StringBuilder();

            foreach (var m in materials)
            {
                mmap[m] = $"Material_{mmap.Count}";

                sb.AppendLine($"newmtl {mmap[m]}");
                sb.AppendLine("illum 2");
                sb.AppendLine(Invariant($"Ka {m.DiffuseColor.X} {m.DiffuseColor.Y} {m.DiffuseColor.Z}"));
                sb.AppendLine(Invariant($"Kd {m.DiffuseColor.X} {m.DiffuseColor.Y} {m.DiffuseColor.Z}"));

                if (m.DiffuseTexture.Array != null)
                {
                    var imgName = files.FirstOrDefault(kvp => kvp.Value == m.DiffuseTexture).Key;
                    sb.AppendLine($"map_Kd {imgName}");
                }

                sb.AppendLine();
            }

            // write material library
            _WriteTextContent(files, baseName + ".mtl", sb);

            return mmap;
        }

        private StringBuilder _GetGeometryContent(IReadOnlyDictionary<Material, string> materials, string mtlLib)
        {
            var sb = new StringBuilder();

            sb.AppendLine($"mtllib {mtlLib}");

            sb.AppendLine();

            foreach (var p in _Mesh.Primitives)
            {
                foreach (var v in p.Vertices)
                {
                    var pos = v.Item1.Position;
                    sb.AppendLine(Invariant($"v {pos.X} {pos.Y} {pos.Z}"));
                }
            }

            sb.AppendLine();

            foreach (var p in _Mesh.Primitives)
            {
                foreach (var v in p.Vertices)
                {
                    var nrm = v.Item1.Normal;
                    sb.AppendLine(Invariant($"vn {nrm.X} {nrm.Y} {nrm.Z}"));
                }
            }

            sb.AppendLine();

            foreach (var p in _Mesh.Primitives)
            {
                foreach (var v in p.Vertices)
                {
                    var uv = v.Item2.TexCoord;
                    uv.Y = 1 - uv.Y;

                    sb.AppendLine(Invariant($"vt {uv.X} {uv.Y}"));
                }
            }

            sb.AppendLine();

            sb.AppendLine("g default");

            var baseVertexIndex = 1;

            foreach (var p in _Mesh.Primitives)
            {
                var mtl = materials[p.Material];

                sb.AppendLine($"usemtl {mtl}");

                foreach (var t in p.Triangles)
                {
                    var a = t.Item1 + baseVertexIndex;
                    var b = t.Item2 + baseVertexIndex;
                    var c = t.Item3 + baseVertexIndex;

                    sb.AppendLine(Invariant($"f {a}/{a}/{a} {b}/{b}/{b} {c}/{c}/{c}"));
                }

                baseVertexIndex += p.Vertices.Count;
            }

            return sb;
        }

        private static void _WriteTextContent(IDictionary<string, BYTES> files, string fileName, StringBuilder sb)
        {
            using (var mem = new System.IO.MemoryStream())
            {
                using (var tex = new System.IO.StreamWriter(mem))
                {
                    tex.Write(sb.ToString());
                }

                mem.TryGetBuffer(out BYTES content);

                files[fileName] = content;
            }
        }

        private static bool _IsPng(IReadOnlyList<Byte> data)
        {
            if (data[0] != 0x89) return false;
            if (data[1] != 0x50) return false;
            if (data[2] != 0x4e) return false;
            if (data[3] != 0x47) return false;

            return true;
        }

        private static bool _IsJpeg(IReadOnlyList<Byte> data)
        {
            if (data[0] != 0xff) return false;
            if (data[1] != 0xd8) return false;

            return true;
        }

        #endregion

        #region schema2 API

        public void AddModel(Schema2.ModelRoot model)
        {
            foreach (var triangle in model.Triangulate())
            {
                var dstMaterial = new Material();

                var srcMaterial = triangle.Item4;
                if (srcMaterial != null)
                {
                    var baseColor = srcMaterial.FindChannel("BaseColor");
                    dstMaterial.DiffuseColor = baseColor.Factor;
                    dstMaterial.DiffuseTexture = baseColor.Image?.GetImageContent() ?? default;
                }

                this.AddTriangle(dstMaterial, triangle.Item1, triangle.Item2, triangle.Item3);
            }
        }

        #endregion
    }
}
