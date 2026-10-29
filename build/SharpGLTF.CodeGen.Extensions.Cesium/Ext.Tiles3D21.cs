using SharpGLTF.CodeGen;
using SharpGLTF.SchemaReflection;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json.Nodes;

namespace SharpGLTF
{
    /// <summary>
    /// Generates code for an extension defined in CesiumGS/glTF (branch 3d-tiles-2.0, extensions/2.1/Vendor).
    /// </summary>
    /// <remarks>
    /// The schemas in Schemas/[extension]/schema are unmodified copies of the upstream files.
    /// Before being loaded, they are normalized into Schemas.Normalized so the code generator can process them:
    /// <list type="bullet">
    /// <item>A root level "$ref" is converted to "allOf".</item>
    /// <item>"extensions" and "extras" properties are added.</item>
    /// <item>The root schema title is replaced so it is recognized as a glTF extension.</item>
    /// </list>
    /// </remarks>
    class Tiles3D21Extension : SchemaProcessor
    {
        public Tiles3D21Extension(string extension, string schemaFile, string rootTitle, string className, string targetSuffix = null)
        {
            _Extension = extension;
            _SchemaFile = schemaFile;
            _RootTitle = rootTitle;
            _ClassName = className;
            _TargetSuffix = targetSuffix;
        }

        private readonly string _Extension;
        private readonly string _SchemaFile;
        private readonly string _RootTitle;
        private readonly string _ClassName;
        private readonly string _TargetSuffix;

        /// <summary>
        /// Additional (schema title, class name) pairs for nested types.
        /// </summary>
        public (string Title, string ClassName)[] NestedTypes { get; set; } = new (string, string)[0];

        /// <summary>
        /// Schema titles of types that are emitted by another extension and must not be emitted again.
        /// </summary>
        public string[] IgnoredTypes { get; set; } = new string[0];

        public override string GetTargetProject() { return CesiumExtensions.CesiumProjectDirectory; }

        public override void PrepareTypes(CSharpEmitter newEmitter, SchemaType.Context ctx)
        {
            newEmitter.SetRuntimeName("glTF Property", "ExtraProperties");
            newEmitter.SetRuntimeName(_RootTitle, _ClassName, CesiumExtensions.CesiumNameSpace);

            // arrays are deserialized by appending items, so they must be lists instead of fixed size arrays
            foreach (var cls in ctx.Classes)
            {
                foreach (var field in cls.Fields.Where(f => f.FieldType is ArrayType || f.FieldType is DictionaryType).ToList())
                {
                    var container = field.FieldType is ArrayType ? "List<TItem>" : null;
                    newEmitter.SetCollectionContainer(field, container);
                }
            }
            foreach (var (title, className) in NestedTypes)
            {
                newEmitter.SetRuntimeName(title, className, CesiumExtensions.CesiumNameSpace);
            }
        }

        public override IEnumerable<(string TargetFileName, SchemaType.Context Schema)> ReadSchema()
        {
            var normalizedDir = NormalizeSchemas();
            var path = Path.Combine(normalizedDir, _SchemaFile);

            var targetName = _TargetSuffix == null ? $"Ext.{_Extension}.g" : $"Ext.{_Extension}.{_TargetSuffix}.g";

            var ctx = SchemaProcessing.LoadExtensionSchemaContext(path);
            foreach (var ignored in IgnoredTypes) ctx.IgnoredByCodeEmitter(ignored);

            if (_Extension == "EXT_node_visibility_volume")
            {
                var bv = ctx.FindClass("Bounding Volume");
                bv.GetField("rotation").SetDataType(typeof(System.Numerics.Quaternion), true).SetDefaultValue("Quaternion.Identity").SetItemsRange(0);
                bv.GetField("scale").SetDataType(typeof(System.Numerics.Vector3), true).SetDefaultValue("Vector3.One").SetItemsRange(0);
                bv.GetField("translation").SetDataType(typeof(System.Numerics.Vector3), true).SetDefaultValue("Vector3.Zero").SetItemsRange(0);
            }

            yield return (targetName, ctx);
        }

        private string NormalizeSchemas()
        {
            var srcDir = Path.GetDirectoryName(CesiumExtensions.CustomExtensionsPath(_Extension, "x.json"));
            var dstDir = Path.Combine(CesiumExtensions.ProgramDirectory, "Schemas.Normalized", _Extension, "schema");
            Directory.CreateDirectory(dstDir);

            foreach (var srcFile in Directory.GetFiles(srcDir, "*.json"))
            {
                var fileName = Path.GetFileName(srcFile);
                var root = JsonNode.Parse(File.ReadAllText(srcFile)).AsObject();

                if (root.TryGetPropertyValue("$ref", out var rootRef))
                {
                    var refValue = rootRef.GetValue<string>();
                    root.Remove("$ref");
                    root["allOf"] = new JsonArray(new JsonObject { ["$ref"] = refValue });
                }

                if (!(root["properties"] is JsonObject props))
                {
                    props = new JsonObject();
                    root["properties"] = props;
                }

                if (!props.ContainsKey("extensions")) props["extensions"] = new JsonObject();
                if (!props.ContainsKey("extras")) props["extras"] = new JsonObject();

                if (fileName == _SchemaFile) root["title"] = _RootTitle;

                RemoveMultiTypes(root);

                // root level "oneOf" only expresses validation constraints (required properties), which the reader can't handle
                root.Remove("oneOf");
                File.WriteAllText(Path.Combine(dstDir, fileName), root.ToJsonString());
            }

            return dstDir;
        }

        /// <summary>
        /// The schema reader does not support "type" declared as an array, so these are converted to untyped values.
        /// </summary>
        private static void RemoveMultiTypes(JsonNode node)
        {
            if (node is JsonObject obj)
            {
                if (obj["type"] is JsonArray) obj.Remove("type");

                // the enumeration reader expects string constants without explicit type
                if (obj["const"] is JsonValue cv && cv.TryGetValue<string>(out _)) obj.Remove("type");

                foreach (var child in obj.Select(kvp => kvp.Value).ToList()) RemoveMultiTypes(child);
            }
            else if (node is JsonArray arr)
            {
                foreach (var child in arr.ToList()) RemoveMultiTypes(child);
            }
        }
    }

    static class Tiles3D21Extensions
    {
        public static IEnumerable<SchemaProcessor> GetProcessors()
        {
            yield return new Tiles3D21Extension("EXT_georeference", "node.EXT_georeference.schema.json", "EXT_georeference glTF Node extension", "ExtGeoreferenceNode");

            yield return new Tiles3D21Extension("EXT_geospatial_crs", "EXT_geospatial_crs.schema.json", "EXT_geospatial_crs glTF Root extension", "ExtGeospatialCrs");
            yield return new Tiles3D21Extension("EXT_geospatial_crs_wkid", "EXT_geospatial_crs_wkid.schema.json", "EXT_geospatial_crs_wkid glTF extension", "ExtGeospatialCrsWkid");
            yield return new Tiles3D21Extension("EXT_geospatial_crs_wkt2", "EXT_geospatial_crs_wkt2.schema.json", "EXT_geospatial_crs_wkt2 glTF extension", "ExtGeospatialCrsWkt2");

            yield return new Tiles3D21Extension("EXT_node_visibility_volume", "EXT_node_visibility_volume.schema.json", "EXT_node_visibility_volume glTF extension", "ExtNodeVisibilityVolume")
            {
                NestedTypes = new[] { ("Bounding Volume", "Tiles3DBoundingVolume") }
            };

            yield return new Tiles3D21Extension("EXT_node_visibility_conditions", "EXT_node_visibility_conditions.schema.json", "EXT_node_visibility_conditions glTF extension", "ExtNodeVisibilityConditions", "visibility");
            yield return new Tiles3D21Extension("EXT_node_visibility_conditions", "glTF.EXT_node_visibility_conditions.schema.json", "EXT_node_visibility_conditions extension", "ExtNodeVisibilityConditionsRoot", "root");

            yield return new Tiles3D21Extension("EXT_voxels", "node.EXT_voxels.schema.json", "EXT_voxels glTF Node extension", "ExtVoxelsNode")
            {
                NestedTypes = new[] { ("Padding", "Tiles3DVoxelPadding") }
            };

            yield return new Tiles3D21Extension("3DTILES_horizon_occlusion_point", "3DTILES_horizon_occlusion_point.schema.json", "3DTILES_horizon_occlusion_point glTF extension", "Tiles3DHorizonOcclusionPoint");
            yield return new Tiles3D21Extension("3DTILES_implicit_tiling", "node.3DTILES_implicit_tiling.schema.json", "3DTILES_implicit_tiling glTF Node extension", "Tiles3DImplicitTiling")
            {
                NestedTypes = new[] { ("OCTREE-QUADTREE", "Tiles3DSubdivisionScheme") }
            };

            yield return new Tiles3D21Extension("3DTILES_layers", "glTF.3DTILES_layers.schema.json", "3DTILES_layers glTF Document extension", "Tiles3DLayersRoot", "root")
            {
                NestedTypes = new[] { ("Layer in 3DTILES_layers", "Tiles3DLayer") }
            };
            yield return new Tiles3D21Extension("3DTILES_layers", "node.3DTILES_layers.schema.json", "3DTILES_layers node extension", "Tiles3DLayersNode", "node");

            yield return new Tiles3D21Extension("3DTILES_shape_cylinder_region", "shape.3DTILES_shape_cylinder_region.schema.json", "3DTILES_shape_cylinder_region glTF extension", "Tiles3DShapeCylinderRegion");
            yield return new Tiles3D21Extension("3DTILES_shape_ellipsoid_region", "shape.3DTILES_shape_ellipsoid_region.schema.json", "3DTILES_shape_ellipsoid_region glTF extension", "Tiles3DShapeEllipsoidRegion");
            yield return new Tiles3D21Extension("3DTILES_shape_s2", "shape.3DTILES_shape_s2.schema.json", "3DTILES_shape_s2 glTF extension", "Tiles3DShapeS2");

            yield return new Tiles3D21Extension("3DTILES_subtree", "glTF.3DTILES_subtree.schema.json", "3DTILES_subtree glTF extension", "Tiles3DSubtree")
            {
                NestedTypes = new[] { ("Availability", "Tiles3DSubtreeAvailability"), ("AVAILABLE-UNAVAILABLE", "Tiles3DAvailabilityConstant") }
            };

            yield return new Tiles3D21Extension("3DTILES_tileset", "glTF.3DTILES_tileset.schema.json", "3DTILES_tileset glTF Document extension", "Tiles3DTilesetRoot", "root");
            yield return new Tiles3D21Extension("3DTILES_tileset", "node.3DTILES_tileset.schema.json", "3DTILES_tileset glTF Node extension", "Tiles3DTilesetNode", "node")
            {
                NestedTypes = new[] { ("Content", "Tiles3DTilesetContent"), ("ADD-REPLACE", "Tiles3DRefineType") },
                IgnoredTypes = new[] { "Bounding Volume" }
            };

            yield return new Tiles3D21Extension("3DTILES_tileset_vectors", "glTF.3DTILES_tileset_vectors.schema.json", "3DTILES_tileset_vectors glTF extension", "Tiles3DTilesetVectors");
            yield return new Tiles3D21Extension("3DTILES_tileset_voxels", "glTF.3DTILES_tileset_voxels.schema.json", "3DTILES_tileset_voxels glTF extension", "Tiles3DTilesetVoxels")
            {
                NestedTypes = new[] { ("Padding", "Tiles3DVoxelPadding") },
                IgnoredTypes = new[] { "Padding" }
            };
        }
    }
}
