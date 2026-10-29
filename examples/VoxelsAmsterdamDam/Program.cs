using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using SharpGLTF.Memory;
using SharpGLTF.Schema2;
using SharpGLTF.Schema2.Tiles3D;

// Creates a small voxel tileset (tileset.json + tile.glb using EXT_voxels) located at the Dam square, Amsterdam.
// Usage: VoxelsAmsterdamDam [outputDirectory]   (default: the build output directory, where index.html is copied to)

const double Longitude = 4.89352;
const double Latitude = 52.37312;
const double EllipsoidHeight = 0.0; // index.html uses a flat ellipsoid basemap, so the ground is at height 0

const int Size = 16;                       // voxels per axis
var halfAxes = new[] { 40.0, 40.0, 20.0 }; // box half sizes in meters (east, north, up)

var outDir = args.Length > 0 ? args[0] : AppContext.BaseDirectory;
Directory.CreateDirectory(outDir);

// class 0 = empty; 1..4 = "material" classes that get a color in index.html
var classes = new byte[Size * Size * Size];
for (int z = 0; z < Size; z++)
{
    for (int y = 0; y < Size; y++)
    {
        for (int x = 0; x < Size; x++)
        {
            double dx = x - (Size - 1) / 2.0;
            double dy = y - (Size - 1) / 2.0;

            // stepped pyramid with a hole through the middle
            double half = 7.5 - z * 0.45;
            bool inPyramid = Math.Max(Math.Abs(dx), Math.Abs(dy)) <= half;
            bool hole = Math.Abs(dx) < 1.5 && Math.Abs(dy) < 1.5 && z > 3;
            if (!inPyramid || hole) continue;

            classes[x + y * Size + z * Size * Size] = (byte)(1 + Math.Min(3, z / 4));
        }
    }
}

Tiles3DExtensions.RegisterExtensions();

var model = ModelRoot.CreateModel();
var scene = model.UseScene("default");
var node = scene.CreateNode("voxels");

var bufferView = model.CreateBufferView(classes.Length);
var accessor = model.CreateAccessor("CLASS");
accessor.SetData(bufferView, 0, classes.Length, AttributeFormat.Byte1);
classes.CopyTo(bufferView.Content.AsSpan());

var voxels = node.UseExtension<ExtVoxelsNode>();
voxels.Dimensions = new[] { Size, Size, Size };
voxels.Attributes = new Dictionary<string, int> { ["CLASS"] = accessor.LogicalIndex };
voxels.NoData = new Dictionary<string, double[]> { ["CLASS"] = new[] { 0.0 } };

model.SaveGLB(Path.Combine(outDir, "tile.glb"));

var transform = EastNorthUp(Longitude, Latitude, EllipsoidHeight);
string F(double v) => v.ToString("R", CultureInfo.InvariantCulture);

var json = $$"""
{
  "asset": { "version": "1.1" },
  "geometricError": 100,
  "root": {
    "transform": [{{string.Join(", ", Array.ConvertAll(transform, F))}}],
    "boundingVolume": {
      "box": [0, 0, {{F(halfAxes[2])}}, {{F(halfAxes[0])}}, 0, 0, 0, {{F(halfAxes[1])}}, 0, 0, 0, {{F(halfAxes[2])}}]
    },
    "geometricError": 0,
    "refine": "ADD",
    "content": { "uri": "tile.glb" }
  }
}
""";
File.WriteAllText(Path.Combine(outDir, "tileset.json"), json);

Console.WriteLine($"Written tile.glb and tileset.json to {Path.GetFullPath(outDir)}");
Console.WriteLine($"Serve this folder (e.g. 'npx http-server') and open index.html");

// East-North-Up frame at the location as a column-major ECEF 4x4 matrix
static double[] EastNorthUp(double lonDeg, double latDeg, double height)
{
    const double a = 6378137.0;
    const double e2 = 0.00669437999014;
    double lon = lonDeg * Math.PI / 180, lat = latDeg * Math.PI / 180;
    double sl = Math.Sin(lat), cl = Math.Cos(lat), so = Math.Sin(lon), co = Math.Cos(lon);
    double n = a / Math.Sqrt(1 - e2 * sl * sl);
    double px = (n + height) * cl * co;
    double py = (n + height) * cl * so;
    double pz = (n * (1 - e2) + height) * sl;

    return new[]
    {
        -so, co, 0, 0,
        -sl * co, -sl * so, cl, 0,
        cl * co, cl * so, sl, 0,
        px, py, pz, 1,
    };
}
