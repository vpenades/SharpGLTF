using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

using SharpGLTF.Schema2;
using SharpGLTF.Geometry;
using SharpGLTF.Geometry.VertexTypes;

namespace InfiniteSkinnedTentacle
{
    using VERTEX = VertexBuilder<VertexPosition, VertexColor1, VertexJoints8x4>;
    using MESH = MeshBuilder<VertexPosition, VertexColor1, VertexJoints8x4>;

    class Program
    {
        // Skinning use cases and examples: https://github.com/KhronosGroup/glTF/issues/1403

        // hierarchy created:

        // Mesh1
        // Skin1─> Armature1
        // Skin2─> Armature2
        // Skin3─> Armature3
        // Scene
        // ├── Armature1
        // │   ├── Bone1
        // │   ├── Bone2
        // │   └── Bone3        
        // ├── SkinnedMesh1─> Mesh1, Skin1
        // ├── Armature2
        // │   ├── Bone1
        // │   ├── Bone2
        // │   └── Bone3
        // ├── SkinnedMesh2─> Mesh1, Skin2
        // ├── Armature3
        // │   ├── Bone1
        // │   ├── Bone2
        // │   └── Bone3
        // └── SkinnedMesh3─> Mesh1, Skin3

        private static readonly Random _Randomizer = new Random(17);

        static void Main(string[] args)
        {
            var model = ModelRoot.CreateModel();
            var scene = model.UseScene("default");

            var mesh = model
                .CreateMeshes(CreateMesh(10))
                .First();

            AddTentacleSkeleton(scene, mesh, Matrix4x4.CreateTranslation(-100, 0, 0), Quaternion.CreateFromYawPitchRoll(0f, 0.2f, 0f));
            AddTentacleSkeleton(scene, mesh, Matrix4x4.CreateTranslation(   0, 0, 0), Quaternion.CreateFromYawPitchRoll(0.2f, 0f, 0f));
            AddTentacleSkeleton(scene, mesh, Matrix4x4.CreateTranslation( 100, 0, 0), Quaternion.CreateFromYawPitchRoll(0f, 0f, 0.2f));

            model.SaveGLB("tentacle.glb");
        }

        static void AddTentacleSkeleton(Scene scene, Mesh mesh, Matrix4x4 origin, Quaternion anim)
        {
            var bindings = new List<Node>();

            Node skeleton = scene.CreateNode();
            skeleton.LocalTransform = origin;

            Node bone = skeleton;

            for (int i = 0; i < 10; ++i)
            {
                if (bone == null)
                {
                    bone = skeleton.CreateNode();
                }
                else
                {
                    bone = bone.CreateNode();
                    bone.LocalTransform = Matrix4x4.CreateTranslation(0, 10, 0);
                }

                bone.WithRotationAnimation("Track0", (0, Quaternion.Identity), (1, anim), (2, Quaternion.Identity));

                bindings.Add(bone);                
            }

            scene.CreateNode()
                .WithMesh(mesh)
                .WithSkinBinding(bindings.ToArray());
        }

        static MESH CreateMesh(int boneCount)
        {
            var mesh = new MESH("skinned mesh");
            var prim = mesh.UsePrimitive(new SharpGLTF.Materials.MaterialBuilder("Default"));

            var a0 = default(VERTEX);
            var a1 = default(VERTEX);
            var a2 = default(VERTEX);
            var a3 = default(VERTEX);            

            for (int i = 0; i < boneCount; ++i)
            {
                var b0 = new VERTEX(new Vector3(-5, i * 10, -5), Vector4.One, (i, 1));
                var b1 = new VERTEX(new Vector3(+5, i * 10, -5), Vector4.One, (i, 1));
                var b2 = new VERTEX(new Vector3(+5, i * 10, +5), Vector4.One, (i, 1));
                var b3 = new VERTEX(new Vector3(-5, i * 10, +5), Vector4.One, (i, 1));

                if (i > 0)
                {
                    prim.AddPolygon(b0, b1, a1, a0);
                    prim.AddPolygon(b1, b2, a2, a1);
                    prim.AddPolygon(b2, b3, a3, a2);
                    prim.AddPolygon(b3, b0, a0, a3);
                }

                a0 = b0;
                a1 = b1;
                a2 = b2;
                a3 = b3;
            }

            return mesh;
        }
    }
}
