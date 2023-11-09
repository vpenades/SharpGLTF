using NUnit.Framework;
using SharpGLTF.Scenes;
using SharpGLTF.Schema2;
using SharpGLTF.Transforms;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text.Json.Nodes;

namespace SharpGLTF.ThirdParty
{
    public class CesiumTests
    {
        [Test]
        [ResourcePathFormat("*\\Assets")]
        public void WriteInstancedGlbWithFeatureIds()
        {
            var settings = SceneBuilderSchema2Settings.WithGpuInstancing;
            settings.GpuMeshInstancingMinCount = 0;

            var modelRoot = ModelRoot.Load(ResourceInfo.From("tree.glb"));
            var meshBuilder = modelRoot.LogicalMeshes.First().ToMeshBuilder();
            var sceneBuilder = new SceneBuilder();
            var quaternion = Quaternion.CreateFromYawPitchRoll(0, 0, 0);
            var scale = Vector3.One;

            sceneBuilder.
                    AddRigidMesh(meshBuilder, new AffineTransform(scale, quaternion, new Vector3(-10, 0, 10))).
                    WithExtras(JsonNode.Parse("{\"_FEATURE_ID_0\":0}"));
            sceneBuilder.
                    AddRigidMesh(meshBuilder, new AffineTransform(scale, quaternion, new Vector3(0, 0, 0))).
                    WithExtras(JsonNode.Parse("{\"_FEATURE_ID_0\":1}"));

            var instancedModel = sceneBuilder.ToGltf2(settings);

            var dstPath = AttachmentInfo
                .From("instanced_model_with_feature_id.glb")
                .WriteObject(f => instancedModel.SaveGLB(f));
        }
    }
}
