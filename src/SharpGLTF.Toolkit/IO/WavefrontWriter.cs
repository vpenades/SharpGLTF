using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;

using SharpGLTF.Schema2;

using static System.FormattableString;

namespace SharpGLTF.IO
{
    #pragma warning disable SA1135 // Using directives should be qualified
    using BYTES = ArraySegment<Byte>;
    using VEMPTY = Geometry.VertexTypes.VertexEmpty;
    using VERTEX = Geometry.VertexBuilder<Geometry.VertexTypes.VertexPositionNormal, Geometry.VertexTypes.VertexTexture1, Geometry.VertexTypes.VertexEmpty>;
    using VGEOMETRY = Geometry.VertexTypes.VertexPositionNormal;
    using VMATERIAL = Geometry.VertexTypes.VertexTexture1;
    #pragma warning restore SA1135 // Using directives should be qualified

    /// <summary>
    /// Tiny <see href="https://www.fileformat.info/format/wavefrontobj/egff.htm">wavefront object</see> writer
    /// </summary>
    /// <remarks>
    /// Wavefront obj export is not intended to be a fully featured wavefront / converter exporter, but
    /// to serve as support and reference for glTF development. Consider this feature as an "easter egg".
    /// </remarks>
    class WavefrontWriter
    {
        #region data

        [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.RootHidden)]
        private readonly Geometry.MeshBuilder<Material, VGEOMETRY, VMATERIAL, VEMPTY> _Mesh = new Geometry.MeshBuilder<Material, VGEOMETRY, VMATERIAL, VEMPTY>();

        #endregion

        #region API

        public void AddTriangle(Material material, in VERTEX a, in VERTEX b, in VERTEX c)
        {
            _Mesh.UsePrimitive(material).AddTriangle(a, b, c);
        }

        /// <summary>
        /// Writes this model to two files:<br/>
        /// - filePath.OBJ<br/>
        /// - filePath.MTL<br/>
        /// </summary>
        /// <param name="filePath">the base file path.</param>
        /// <remarks>
        /// The extension of <paramref name="filePath"/> is discarded and replaced by .OBJ and .MTL when required.
        /// </remarks>
        public void WriteFiles(string filePath)
        {
            Guard.NotNullOrEmpty(filePath, nameof(filePath));

            var files = GetFiles(System.IO.Path.GetFileNameWithoutExtension(filePath));

            var dir = System.IO.Path.GetDirectoryName(filePath);

            foreach (var f in files)
            {
                var fpath = System.IO.Path.Combine(dir, f.Key);
                System.IO.File.WriteAllBytes(fpath, f.Value.ToArray());
            }
        }

        /// <summary>
        /// Serializes this model to a dictionary with 2 entries:<br/>
        /// - ("baseName.OBJ", Byte[])<br/>
        /// - ("baseName.MTL", Byte[])<br/>
        /// </summary>
        /// <param name="baseName">the file name.</param>
        /// <returns>A dictionary with two entries.</returns>
        /// <remarks>
        /// The extension of <paramref name="baseName"/> is discarded and replaced by .OBJ and .MTL when required.
        /// </remarks>
        public IReadOnlyDictionary<String, BYTES> GetFiles(string baseName)
        {
            Guard.IsFalse(baseName.Any(c => char.IsWhiteSpace(c)), nameof(baseName), "Whitespace characters not allowed in filename");

            var files = new Dictionary<String, BYTES>();

            var materials = _WriteMaterials(files, baseName, _Mesh.Primitives.Select(item => item.Material));

            var geocontent = _GetGeometryContent(materials, baseName + ".mtl");

            _WriteTextContent(files, baseName + ".obj", geocontent);

            return files;
        }

        private static IReadOnlyDictionary<Material, string> _WriteMaterials(IDictionary<String, BYTES> files, string baseName, IEnumerable<Material> materials)
        {
            // write all image files
            var images = materials
                .Select(item => item.DiffuseTexture)
                .Where(item => item.IsValid)
                .Distinct();

            bool firstImg = true;

            foreach (var img in images)
            {
                var imgName = firstImg ? baseName : $"{baseName}_{files.Count}.{img.FileExtension}";
                files[imgName] = new BYTES(img.Content.ToArray());
                firstImg = false;
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
                sb.AppendLine(Invariant($"Ks {m.SpecularColor.X} {m.SpecularColor.Y} {m.SpecularColor.Z}"));

                if (m.DiffuseTexture.IsValid)
                {
                    var imgName = files.FirstOrDefault(kvp => new Memory.MemoryImage(kvp.Value) == m.DiffuseTexture ).Key;
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
                    var pos = v.Position;
                    sb.AppendLine(Invariant($"v {pos.X} {pos.Y} {pos.Z}"));
                }
            }

            sb.AppendLine();

            foreach (var p in _Mesh.Primitives)
            {
                foreach (var v in p.Vertices)
                {
                    var nrm = v.Geometry.Normal;
                    sb.AppendLine(Invariant($"vn {nrm.X} {nrm.Y} {nrm.Z}"));
                }
            }

            sb.AppendLine();

            foreach (var p in _Mesh.Primitives)
            {
                foreach (var v in p.Vertices)
                {
                    var uv = v.Material.TexCoord;
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
                    var a = t.A + baseVertexIndex;
                    var b = t.B + baseVertexIndex;
                    var c = t.C + baseVertexIndex;

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

        #endregion

        #region schema2 API

        public void AddModel(ModelRoot model)
        {
            foreach (var triangle in Toolkit.EvaluateTriangles<VGEOMETRY, VMATERIAL>(model.DefaultScene))
            {
                var dstMaterial = GetMaterialFromTriangle(triangle.Material);
                this.AddTriangle(dstMaterial, triangle.A, triangle.B, triangle.C);
            }
        }

        public void AddModel(ModelRoot model, Animation animation, float time)
        {
            var options = new Runtime.RuntimeOptions();
            options.IsolateMemory = false;
            options.GpuMeshInstancing = Runtime.MeshInstancing.SingleMesh;

            foreach (var triangle in Toolkit.EvaluateTriangles<VGEOMETRY, VMATERIAL>(model.DefaultScene, options, animation, time))
            {
                var dstMaterial = GetMaterialFromTriangle(triangle.Material);
                this.AddTriangle(dstMaterial, triangle.A, triangle.B, triangle.C);
            }
        }

        private static Material GetMaterialFromTriangle(Schema2.Material srcMaterial)
        {
            if (srcMaterial == null) return default;

            // https://stackoverflow.com/questions/36510170/how-to-calculate-specular-contribution-in-pbr

            var diffuse = srcMaterial.GetDiffuseColor(Vector4.One);

            var dstMaterial = default(Material);

            dstMaterial.DiffuseColor = new Vector3(diffuse.X, diffuse.Y, diffuse.Z);
            dstMaterial.SpecularColor = new Vector3(0.2f);

            dstMaterial.DiffuseTexture = srcMaterial.GetDiffuseTexture()?.PrimaryImage?.Content ?? default;

            return dstMaterial;
        }

        #endregion

        #region nested types

        [System.Diagnostics.DebuggerDisplay("{DiffuseColor} {DiffuseTexture.ToDebuggerDisplay()}")]
        public struct Material
        {
            public Vector3 DiffuseColor;
            public Vector3 SpecularColor;
            public Memory.MemoryImage DiffuseTexture;
        }

        #endregion
    }
}
