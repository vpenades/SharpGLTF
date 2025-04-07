using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using SharpGLTF.CodeGen;
using SharpGLTF.SchemaReflection;

namespace SharpGLTF
{
    public class MainSchemaProcessor : SchemaProcessor
    {
        public override IEnumerable<(string TargetFileName, SchemaType.Context Schema)> ReadSchema()
        {
            // load and process schema
            var ctx = SchemaProcessing.LoadSchemaContext(Constants.MainSchemaFile);

            // Ignore "glTF Property" because it is completely hand coded.
            ctx.IgnoredByCodeEmitter("glTF Property");

            // We will mimeType "anyof" as a plain string.
            ctx.Remove("image/jpeg-image/png");

            // replace Image.mimeType type from an Enum to String, so we can serialize it with more formats if required
            ctx.FindClass("Image")
                .GetField("mimeType")
                .FieldType = ctx.UseString();

            // replace Node.Matrix, Node.Rotation, Node.Scale and Node.Translation with System.Numerics.Vectors types
            var node = ctx.FindClass("Node");

            // the default values of the transform properties is both a "known value" and null, so
            // we preffer to set here the "known value" since it's also used to check whether
            // the value should be serialized.
            // But then, we need to set the values to null in the Node Constructor,
            // because Matrix and SRT are mutually exclusive.
            node.GetField("matrix").SetDataType(typeof(System.Numerics.Matrix4x4), true).SetDefaultValue("System.Numerics.Matrix4x4.Identity").SetItemsRange(0);
            node.GetField("scale").SetDataType(typeof(System.Numerics.Vector3), true).SetDefaultValue("Vector3.One").SetItemsRange(0);
            node.GetField("rotation").SetDataType(typeof(System.Numerics.Quaternion), true).SetDefaultValue("Quaternion.Identity").SetItemsRange(0);            
            node.GetField("translation").SetDataType(typeof(System.Numerics.Vector3), true).SetDefaultValue("Vector3.Zero").SetItemsRange(0);

            // replace Material.emissiveFactor with System.Numerics.Vectors types
            ctx.FindClass("Material")
                .GetField("emissiveFactor")
                .SetDataType(typeof(System.Numerics.Vector3), true)
                .SetDefaultValue("Vector3.Zero")
                .SetItemsRange(0);

            // replace Material.baseColorFactor with System.Numerics.Vectors types
            ctx.FindClass("Material PBR Metallic Roughness")
                .GetField("baseColorFactor")
                .SetDataType(typeof(System.Numerics.Vector4), true)
                .SetDefaultValue("Vector4.One")
                .SetItemsRange(0);

            ctx.FindEnum("LINEAR-NEAREST")
                .SetValue("DEFAULT", 0);

            ctx.FindEnum("LINEAR-LINEAR_MIPMAP_LINEAR-LINEAR_MIPMAP_NEAREST-NEAREST-NEAREST_MIPMAP_LINEAR-NEAREST_MIPMAP_NEAREST")
                .SetValue("DEFAULT", 0);

            // Accessor.type is declared as AnyOfEnum, but also as a STRING,
            // which can be used by extensions to store non standard values like MAT4x3
            ctx.FindClass("Accessor")
                .GetField("type").SetDataType(typeof(string), true);

            // Since DimensionType can have additional values other than the ones defined by the schema
            // we need a "special" value to define it
            ctx.FindEnum("MAT2-MAT3-MAT4-SCALAR-VEC2-VEC3-VEC4")
                .SetValue("CUSTOM", 0);

            yield return ("gltf.g", ctx);
        }

        public override void PrepareTypes(CSharpEmitter newEmitter, SchemaType.Context ctx)
        {
            newEmitter.SetCollectionContainer("List<TItem>");

            const string rootName = "ModelRoot";

            newEmitter.SetRuntimeName("glTF", rootName);
            newEmitter.SetRuntimeName("glTF Property", "ExtraProperties");
            newEmitter.SetRuntimeName("glTF Child of Root Property", "LogicalChildOfRoot");

            newEmitter.SetRuntimeName("Sampler", "TextureSampler");

            newEmitter.SetRuntimeName("UNSIGNED_BYTE-UNSIGNED_INT-UNSIGNED_SHORT", "IndexEncodingType");
            newEmitter.SetRuntimeName("BYTE-FLOAT-SHORT-UNSIGNED_BYTE-UNSIGNED_INT-UNSIGNED_SHORT", "EncodingType");
            newEmitter.SetRuntimeName("MAT2-MAT3-MAT4-SCALAR-VEC2-VEC3-VEC4", "DimensionType");
            newEmitter.SetRuntimeName("rotation-scale-translation-weights", "PropertyPath");
            newEmitter.SetRuntimeName("ARRAY_BUFFER-ELEMENT_ARRAY_BUFFER", "BufferMode");
            newEmitter.SetRuntimeName("orthographic-perspective", "CameraType");
            newEmitter.SetRuntimeName("BLEND-MASK-OPAQUE", "AlphaMode");
            newEmitter.SetRuntimeName("LINE_LOOP-LINE_STRIP-LINES-POINTS-TRIANGLE_FAN-TRIANGLE_STRIP-TRIANGLES", "PrimitiveType");
            newEmitter.SetRuntimeName("CUBICSPLINE-LINEAR-STEP", "AnimationInterpolationMode");
            newEmitter.SetRuntimeName("LINEAR-NEAREST", "TextureInterpolationFilter");
            newEmitter.SetRuntimeName("CLAMP_TO_EDGE-MIRRORED_REPEAT-REPEAT", "TextureWrapMode");
            newEmitter.SetRuntimeName("LINEAR-LINEAR_MIPMAP_LINEAR-LINEAR_MIPMAP_NEAREST-NEAREST-NEAREST_MIPMAP_LINEAR-NEAREST_MIPMAP_NEAREST", "TextureMipMapFilter");

            // add "pointer" enum value to PropertyPath, which is required by KHR_animation_pointer
            var propertyPathEnum = ctx.FindEnum("rotation-scale-translation-weights");            
            propertyPathEnum?.SetValue("pointer", 0);

            var meshClass = ctx.FindClass("Mesh");
            if (meshClass != null)
            {
                newEmitter.SetCollectionContainer(meshClass.UseField("primitives"), "ChildrenList<TItem,Mesh>");
            }

            var animationClass = ctx.FindClass("Animation");
            if (animationClass != null)
            {
                newEmitter.SetCollectionContainer(animationClass.UseField("channels"), "ChildrenList<TItem,Animation>");
                newEmitter.SetCollectionContainer(animationClass.UseField("samplers"), "ChildrenList<TItem,Animation>");
            }

            var classes = ctx.Classes.ToArray();
            var fields = classes
                .SelectMany(item => item.Fields)
                .ToArray();

            foreach (var f in fields)
            {
                if (f.FieldType is ArrayType atype)
                {
                    if (atype.ItemType is ClassType ctype)
                    {
                        if (ctype.BaseClass != null && ctype.BaseClass.PersistentName == "glTF Child of Root Property")
                        {
                            newEmitter.SetCollectionContainer(f, $"ChildrenList<TItem,{rootName}>");
                        }
                    }
                }
            }
        }        
    }
}
