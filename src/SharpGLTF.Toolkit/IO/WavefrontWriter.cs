using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text;

using SharpGLTF.Memory;
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

            var dir = System.IO.Path.GetDirectoryName(filePath);

            foreach (var fileNameAndGenerator in _GetFileGenerators(System.IO.Path.GetFileNameWithoutExtension(filePath)))
            {
                var fpath = System.IO.Path.Combine(dir, fileNameAndGenerator.Key);
                using var fs = File.OpenWrite(fpath);
                fileNameAndGenerator.Value(fs);
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
            foreach (var fileNameAndGenerator in _GetFileGenerators(baseName)) 
            {
                using var mem = new MemoryStream();
                fileNameAndGenerator.Value(mem);

                mem.TryGetBuffer(out var bytes);

                files[fileNameAndGenerator.Key] = bytes;
            }

            return files;
        }

        private IReadOnlyDictionary<String, Action<Stream>> _GetFileGenerators(string baseName) 
        {
            Guard.IsFalse(baseName.Any(c => char.IsWhiteSpace(c)), nameof(baseName), "Whitespace characters not allowed in filename");

            var fileGenerators = new Dictionary<String, Action<Stream>>();

            var materials = _GetMaterialFileGenerator(fileGenerators, baseName, _Mesh.Primitives.Select(item => item.Material));

            fileGenerators[baseName + ".obj"] = fs => _GetGeometryContent(new StreamWriter(fs), materials, baseName + ".mtl");

            return fileGenerators;
        }

        private static IReadOnlyDictionary<Material, string> _GetMaterialFileGenerator(IDictionary<String, Action<Stream>> fileGenerators, string baseName, IEnumerable<Material> materials)
        {
            // write all image files
            var images = materials
                .Select(item => item.DiffuseTexture)
                .Where(item => item.IsValid)
                .Distinct();

            bool firstImg = true;

            var imageNameByImage = new Dictionary<MemoryImage, string>();
            foreach (var img in images)
            {
                var imgName = firstImg
                    ? $"{baseName}.{img.FileExtension}"
                    : $"{baseName}_{fileGenerators.Count}.{img.FileExtension}";

                fileGenerators[imgName] = fs => {
                    var bytes = img.Content.ToArray();
                    fs.Write(bytes, 0, bytes.Length);
                };
                firstImg = false;

                imageNameByImage[img] = imgName;
            }

            // write materials

            var mmap = new Dictionary<Material, string>();
            foreach (var m in materials) 
            {
                mmap[m] = $"Material_{mmap.Count}";
            }

            // write material library
            fileGenerators[baseName + ".mtl"] = fs =>
            {
                var sw = new StreamWriter(fs);
                foreach (var m in materials) 
                {
                    sw.WriteLine($"newmtl {mmap[m]}");
                    sw.WriteLine("illum 2");
                    sw.WriteLine(Invariant($"Ka {m.DiffuseColor.X} {m.DiffuseColor.Y} {m.DiffuseColor.Z}"));
                    sw.WriteLine(Invariant($"Kd {m.DiffuseColor.X} {m.DiffuseColor.Y} {m.DiffuseColor.Z}"));
                    sw.WriteLine(Invariant($"Ks {m.SpecularColor.X} {m.SpecularColor.Y} {m.SpecularColor.Z}"));

                    if (m.DiffuseTexture.IsValid) {
                        var imgName = imageNameByImage[m.DiffuseTexture];
                        sw.WriteLine($"map_Kd {imgName}");
                    }

                    sw.WriteLine();
                }
            };

            return mmap;
        }

        private void _GetGeometryContent(StreamWriter sw, IReadOnlyDictionary<Material, string> materials, string mtlLib)
        {
            sw.WriteLine($"mtllib {mtlLib}");

            sw.WriteLine();

            foreach (var p in _Mesh.Primitives)
            {
                foreach (var v in p.Vertices)
                {
                    var pos = v.Position;
                    sw.WriteLine(Invariant($"v {pos.X} {pos.Y} {pos.Z}"));
                }
            }

            sw.WriteLine();

            foreach (var p in _Mesh.Primitives)
            {
                foreach (var v in p.Vertices)
                {
                    var nrm = v.Geometry.Normal;
                    sw.WriteLine(Invariant($"vn {nrm.X} {nrm.Y} {nrm.Z}"));
                }
            }

            sw.WriteLine();

            foreach (var p in _Mesh.Primitives)
            {
                foreach (var v in p.Vertices)
                {
                    var uv = v.Material.TexCoord;
                    uv.Y = 1 - uv.Y;

                    sw.WriteLine(Invariant($"vt {uv.X} {uv.Y}"));
                }
            }

            sw.WriteLine();

            sw.WriteLine("g default");

            var baseVertexIndex = 1;

            foreach (var p in _Mesh.Primitives)
            {
                var mtl = materials[p.Material];

                sw.WriteLine($"usemtl {mtl}");

                foreach (var t in p.Triangles)
                {
                    var a = t.A + baseVertexIndex;
                    var b = t.B + baseVertexIndex;
                    var c = t.C + baseVertexIndex;

                    sw.WriteLine(Invariant($"f {a}/{a}/{a} {b}/{b}/{b} {c}/{c}/{c}"));
                }

                baseVertexIndex += p.Vertices.Count;
            }
        }

        #endregion

        #region schema2 API

        public void AddModel(ModelRoot model)
        {
            // retrieve a "snapshot" of all the triangles of the scene
            var triangles = Toolkit.EvaluateTriangles<VGEOMETRY, VMATERIAL>(model.DefaultScene);

            // bake the material transforms into the UV coordinates
            triangles = EvaluatedTriangle<VGEOMETRY, VMATERIAL, VEMPTY>.TransformTextureCoordsByMaterial(triangles);

            foreach (var triangle in triangles)
            {
                var dstMaterial = GetMaterialFromTriangle(triangle.Material);
                this.AddTriangle(dstMaterial, triangle.A, triangle.B, triangle.C);
            }
        }

        public void AddModel(ModelRoot model, Animation animation, float time)
        {
            var options = new Runtime.RuntimeOptions();
            options.IsolateMemory = false;
            // options.BakeTextureTransforms = true;
            options.GpuMeshInstancing = Runtime.MeshInstancing.SingleMesh;

            // retrieve a "snapshot" of all the triangles of the scene
            var triangles = Toolkit.EvaluateTriangles<VGEOMETRY, VMATERIAL>(model.DefaultScene, options, animation, time);

            // bake the material transforms into the UV coordinates
            triangles = EvaluatedTriangle<VGEOMETRY, VMATERIAL, VEMPTY>.TransformTextureCoordsByMaterial(triangles);

            foreach (var triangle in triangles)
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
        public struct Material : IEquatable<Material>
        {
            public Vector3 DiffuseColor;
            public Vector3 SpecularColor;
            public Memory.MemoryImage DiffuseTexture;            

            public readonly override int GetHashCode()
            {
                return DiffuseColor.GetHashCode() ^ SpecularColor.GetHashCode() ^ DiffuseTexture.GetHashCode();
            }

            public readonly override bool Equals(object obj) { return obj is Material other && this.Equals(other); }

            public readonly bool Equals(Material other)
            {
                if (this.DiffuseColor != other.DiffuseColor) return false;
                if (this.SpecularColor != other.SpecularColor) return false;
                if (this.DiffuseTexture != other.DiffuseTexture) return false;
                return true;
            }
        }

        #endregion
    }
}
