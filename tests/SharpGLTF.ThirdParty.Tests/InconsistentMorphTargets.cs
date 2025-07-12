using NUnit.Framework;
using System.Numerics;

using SharpGLTF.Geometry;
using SharpGLTF.Geometry.VertexTypes;
using SharpGLTF.Scenes;
using SharpGLTF.Schema2;
using SharpGLTF.Materials;

namespace SharpGLTF.ThirdParty
{
    
    public class InconsistentMorphTargets
    {
        
        [TestCase(false, Description = "check if all primitives has the same number of morph target.")]
        [TestCase(true, Description = "check if all primitives has the same number of morph target. use_temp_fix to fix the problem with a trick.")]
        public void BuildMesh_WithPartiallyAppliedMorphTargets_AllPrimitivesShouldHaveSameTargetCount(bool use_temp_fix)
        {
            

            var material1 = new MaterialBuilder("material1");
            var material2 = new MaterialBuilder("material2");
            var material3 = new MaterialBuilder("material3");

            // here we will construct 3 primitive and 2 morph targets.
            // And the first morph target only involve p_0 and p_1
            // The 2nd morph target only involve p_1 and p_2

            var meshBuilder = new MeshBuilder<VertexPosition>("mesh with partial morph targets");

            var prim0 = meshBuilder.UsePrimitive(material1);
            prim0.AddTriangle( new VertexPosition(-1, 0, 0), new VertexPosition(-2, 0, 0), new VertexPosition(-1, 1, 0) ); 

            var prim1 = meshBuilder.UsePrimitive(material2);
            prim1.AddTriangle( new VertexPosition(0, 0, 0), new VertexPosition(1, 0, 0), new VertexPosition(0, 1, 0) ); 

            var prim2 = meshBuilder.UsePrimitive(material3);
            prim2.AddTriangle( new VertexPosition(2, 0, 0), new VertexPosition(3, 0, 0), new VertexPosition(2, 1, 0) ); 

            var morphTarget0 = meshBuilder.UseMorphTarget(0);
            var morphTarget1 = meshBuilder.UseMorphTarget(1);

            morphTarget0.SetVertexDelta(new VertexPosition(-1, 0, 0), new VertexGeometryDelta(new Vector3(0.1f, 0.1f, 0), Vector3.Zero, Vector3.Zero)); 
            morphTarget0.SetVertexDelta(new VertexPosition(0, 0, 0), new VertexGeometryDelta(new Vector3(0.1f, 0.1f, 0), Vector3.Zero, Vector3.Zero));   

            morphTarget1.SetVertexDelta(new VertexPosition(1, 0, 0), new VertexGeometryDelta(new Vector3(-0.1f, 0.2f, 0), Vector3.Zero, Vector3.Zero)); 
            morphTarget1.SetVertexDelta(new VertexPosition(3, 0, 0), new VertexGeometryDelta(new Vector3(-0.1f, 0.2f, 0), Vector3.Zero, Vector3.Zero));

            // use_temp_fix decide whether fix the error with a trick
            if(use_temp_fix)
            {
                var morphTarget2 = meshBuilder.UseMorphTarget(2);
                foreach (var primitive in meshBuilder.Primitives)
                {
                    var index = primitive.Vertices[0];
                    morphTarget2.SetVertexDelta(primitive.Vertices[0].Position, new VertexGeometryDelta(Vector3.Zero, Vector3.Zero, Vector3.Zero));
                }
            }
            
            var sceneBuilder = new SceneBuilder();
            sceneBuilder.AddRigidMesh(meshBuilder, Matrix4x4.Identity);

            var model = sceneBuilder.ToGltf2();

            // save the output glb for test
            AttachmentInfo.From("InconsistentMorphTargets_unfixed.glb").WriteObject(f => model.SaveGLB(f));

            Assert.That(model.LogicalMeshes.Count, Is.EqualTo(1), "The model should have only one mesh");
            var logicalMesh = model.LogicalMeshes[0];

            Assert.That(logicalMesh.Primitives.Count, Is.EqualTo(3), "The mesh should have three primitives.");

            var prim0_test = logicalMesh.Primitives[0];
            var prim1_test = logicalMesh.Primitives[1];
            var prim2_test = logicalMesh.Primitives[2];

            int expectedTargetCount = prim0_test.MorphTargetsCount;

            // check if the number of morph targets of primitives are equal.
            Assert.That(prim1_test.MorphTargetsCount, Is.EqualTo(expectedTargetCount), "The number of morph targets of the second primitive is not equal to the first one.");
            Assert.That(prim2_test.MorphTargetsCount, Is.EqualTo(expectedTargetCount), "The number of morph targets of the third primitive is not equal to the first one.");
        }
    }
}