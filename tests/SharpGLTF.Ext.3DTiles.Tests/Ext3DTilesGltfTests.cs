using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace SharpGLTF.Schema2.Tiles3D
{
    [Category("3DTiles")]
    public class Ext3DTilesGltfTests
    {
        [SetUp]
        public void SetUp()
        {
            Tiles3DExtensions.RegisterExtensions();
        }

        private static ModelRoot _RoundTrip(ModelRoot model)
        {
            var bytes = model.WriteGLB();
            return ModelRoot.ParseGLB(bytes);
        }

        private static ModelRoot _CreateModel(out Node node)
        {
            var model = ModelRoot.CreateModel();
            var scene = model.UseScene("default");
            node = scene.CreateNode("node");
            return model;
        }

        [Test]
        public void GeoreferenceNodeRoundTrip()
        {
            var model = _CreateModel(out var node);

            var ext = node.UseExtension<ExtGeoreferenceNode>();
            ext.Longitude = 4.9;
            ext.Latitude = 52.37;
            ext.Height = 12.5;

            Assert.Throws<System.ArgumentOutOfRangeException>(() => ext.Latitude = 91);

            var loaded = _RoundTrip(model).LogicalNodes[0].GetExtension<ExtGeoreferenceNode>();

            Assert.That(loaded, Is.Not.Null);
            Assert.That(loaded.Longitude, Is.EqualTo(4.9));
            Assert.That(loaded.Latitude, Is.EqualTo(52.37));
            Assert.That(loaded.Height, Is.EqualTo(12.5));
        }

        [Test]
        public void GeospatialCrsRoundTrip()
        {
            var model = _CreateModel(out _);

            var crs = model.UseExtension<ExtGeospatialCrs>();
            crs.Format = "wkid";

            var wkid = crs.UseExtension<ExtGeospatialCrsWkid>();
            wkid.Authority = "EPSG";
            wkid.Wkid = 4978;

            var loaded = _RoundTrip(model).GetExtension<ExtGeospatialCrs>();

            Assert.That(loaded.Format, Is.EqualTo("wkid"));

            var loadedWkid = loaded.GetExtension<ExtGeospatialCrsWkid>();
            Assert.That(loadedWkid.Authority, Is.EqualTo("EPSG"));
            Assert.That(loadedWkid.Wkid, Is.EqualTo(4978));
            Assert.That(loadedWkid.VcsWkid, Is.Null);
        }

        [Test]
        public void GeospatialCrsWkt2RoundTrip()
        {
            var model = _CreateModel(out _);

            var crs = model.UseExtension<ExtGeospatialCrs>();
            crs.Format = "wkt2";
            crs.UseExtension<ExtGeospatialCrsWkt2>().Wkt2 = "GEOGCRS[\"WGS 84\"]";

            var loaded = _RoundTrip(model).GetExtension<ExtGeospatialCrs>();

            Assert.That(loaded.GetExtension<ExtGeospatialCrsWkt2>().Wkt2, Is.EqualTo("GEOGCRS[\"WGS 84\"]"));
        }

        [Test]
        public void ImplicitTilingRoundTrip()
        {
            var model = _CreateModel(out var node);

            var ext = node.UseExtension<Tiles3DImplicitTiling>();
            ext.ContentUri = "content/{level}/{x}/{y}.glb";
            ext.SubtreeUri = "subtrees/{level}/{x}/{y}.json";
            ext.SubdivisionScheme = Tiles3DSubdivisionScheme.QUADTREE;
            ext.AvailableLevels = 5;
            ext.SubtreeLevels = 3;

            var loaded = _RoundTrip(model).LogicalNodes[0].GetExtension<Tiles3DImplicitTiling>();

            Assert.That(loaded.ContentUri, Is.EqualTo(ext.ContentUri));
            Assert.That(loaded.SubtreeUri, Is.EqualTo(ext.SubtreeUri));
            Assert.That(loaded.SubdivisionScheme, Is.EqualTo(Tiles3DSubdivisionScheme.QUADTREE));
            Assert.That(loaded.AvailableLevels, Is.EqualTo(5));
            Assert.That(loaded.SubtreeLevels, Is.EqualTo(3));
        }

        [Test]
        public void TilesetNodeRoundTrip()
        {
            var model = _CreateModel(out var node);

            model.UseExtension<Tiles3DTilesetRoot>().GeometricError = 100;

            var ext = node.UseExtension<Tiles3DTilesetNode>();
            ext.GeometricError = 10;
            ext.Refine = Tiles3DRefineType.REPLACE;
            ext.Content = new Tiles3DTilesetContent
            {
                BoundingVolume = new Tiles3DBoundingVolume { Shape = 0, Translation = new System.Numerics.Vector3(1, 2, 3) }
            };

            var loaded = _RoundTrip(model);

            Assert.That(loaded.GetExtension<Tiles3DTilesetRoot>().GeometricError, Is.EqualTo(100));

            var loadedNode = loaded.LogicalNodes[0].GetExtension<Tiles3DTilesetNode>();
            Assert.That(loadedNode.GeometricError, Is.EqualTo(10));
            Assert.That(loadedNode.Refine, Is.EqualTo(Tiles3DRefineType.REPLACE));
            Assert.That(loadedNode.Content.BoundingVolume.Translation, Is.EqualTo(new System.Numerics.Vector3(1, 2, 3)));
        }

        [Test]
        public void LayersRoundTrip()
        {
            var model = _CreateModel(out var node);

            model.UseExtension<Tiles3DLayersRoot>().Layers = new[] { new Tiles3DLayer(), new Tiles3DLayer() };
            node.UseExtension<Tiles3DLayersNode>().Layer = 1;

            var loaded = _RoundTrip(model);

            Assert.That(loaded.GetExtension<Tiles3DLayersRoot>().Layers.Count, Is.EqualTo(2));
            Assert.That(loaded.LogicalNodes[0].GetExtension<Tiles3DLayersNode>().Layer, Is.EqualTo(1));
        }

        [Test]
        public void VoxelsRoundTrip()
        {
            var model = _CreateModel(out var node);

            var ext = node.UseExtension<ExtVoxelsNode>();
            ext.Dimensions = new[] { 4, 4, 4 };
            ext.Padding = new Tiles3DVoxelPadding { Before = new[] { 1, 1, 1 }, After = new[] { 0, 0, 0 } };
            ext.Attributes = new Dictionary<string, int> { ["temperature"] = 0 };

            model.UseExtension<Tiles3DTilesetVoxels>().Class = "voxel";

            var loaded = _RoundTrip(model);

            var loadedExt = loaded.LogicalNodes[0].GetExtension<ExtVoxelsNode>();
            Assert.That(loadedExt.Dimensions, Is.EqualTo(new[] { 4, 4, 4 }));
            Assert.That(loadedExt.Padding.Before, Is.EqualTo(new[] { 1, 1, 1 }));
            Assert.That(loadedExt.Attributes["temperature"], Is.EqualTo(0));
            Assert.That(loaded.GetExtension<Tiles3DTilesetVoxels>().Class, Is.EqualTo("voxel"));
        }

        [Test]
        public void SubtreeRoundTrip()
        {
            var model = _CreateModel(out _);

            var ext = model.UseExtension<Tiles3DSubtree>();
            ext.TileAvailability = new Tiles3DSubtreeAvailability { Constant = Tiles3DAvailabilityConstant.AVAILABLE };
            ext.ChildSubtreeAvailability = new Tiles3DSubtreeAvailability { Bitstream = 0, AvailableCount = 4 };
            ext.TileAttributes = new Dictionary<string, int> { ["a"] = 1 };

            var loaded = _RoundTrip(model).GetExtension<Tiles3DSubtree>();

            Assert.That(loaded.TileAvailability.Constant, Is.EqualTo(Tiles3DAvailabilityConstant.AVAILABLE));
            Assert.That(loaded.ChildSubtreeAvailability.Bitstream, Is.EqualTo(0));
            Assert.That(loaded.ChildSubtreeAvailability.AvailableCount, Is.EqualTo(4));
            Assert.That(loaded.TileAttributes["a"], Is.EqualTo(1));
        }

        [Test]
        public void VectorsAndHorizonOcclusionRoundTrip()
        {
            var model = _CreateModel(out _);

            model.UseExtension<Tiles3DTilesetVectors>().Clip = true;
            model.UseExtension<Tiles3DHorizonOcclusionPoint>().HorizonOcclusionPoint = new[] { 0.1, 0.2, 0.3 };

            var loaded = _RoundTrip(model);

            Assert.That(loaded.GetExtension<Tiles3DTilesetVectors>().Clip, Is.True);
            Assert.That(loaded.GetExtension<Tiles3DHorizonOcclusionPoint>().HorizonOcclusionPoint, Is.EqualTo(new[] { 0.1, 0.2, 0.3 }));
        }
    }
}
