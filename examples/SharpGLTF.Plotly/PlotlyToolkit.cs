using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;

using SharpGLTF.Geometry;
using SharpGLTF.Materials;
using SharpGLTF.Schema2;

using TRACES = Plotly.Box<Plotly.Types.ITracesProperty>;

namespace SharpGLTF
{
    public static class PlotlyToolkit
    {
        public static PlotlyScene ToPlotly(this Schema2.Scene srcScene, Animation animation = null, float time = 0)
        {
            // create an instantiable scene.
            var sceneInstance = Runtime.SceneTemplate
                .Create(srcScene, false)
                .CreateInstance();

            // set the node animations for our scene instance-
            if (animation == null) { sceneInstance.SetPoseTransforms(); }
            else { sceneInstance.SetAnimationFrame(animation.Name, time); }

            // keep source meshes.
            var meshes = srcScene.LogicalParent.LogicalMeshes;

            // get the drawable instances.
            var instances = sceneInstance
                .DrawableInstances
                .Where(item => item.Transform.Visible);

            // prepare the PlotlyScene.
            var dstScene = new PlotlyScene();

            // enumerate and "render" the instances.
            foreach (var instance in instances)
            {
                var mesh = meshes[instance.Template.LogicalMeshIndex];
                var tris = mesh.EvaluateTriangles(instance.Transform);

                dstScene.AppendTriangles(tris, Matrix4x4.Identity, GetMaterialColor);
            }

            return dstScene;            
        }

        public static PlotlyScene ToPlotly(this Schema2.Mesh mesh)
        {
            var dstScene = new PlotlyScene();
            dstScene.AppendTriangles(mesh.EvaluateTriangles(), Matrix4x4.Identity, GetMaterialColor);
            return dstScene;
        }

        public static PlotlyScene ToPlotly(this IMeshBuilder<MaterialBuilder> mesh)
        {
            IEnumerable<(IVertexBuilder, IVertexBuilder, IVertexBuilder, MaterialBuilder)> _enumTris()
            {
                foreach (var p in mesh.Primitives)
                {
                    foreach (var (A, B, C) in p.Triangles)
                    {
                        var va = p.Vertices[A];
                        var vb = p.Vertices[B];
                        var vc = p.Vertices[C];

                        yield return (va, vb, vc, p.Material);
                    }
                }
            }

            var scene = new PlotlyScene();
            scene.AppendTriangles(_enumTris(), Matrix4x4.Identity, GetMaterialColor);
            return scene;
        }

        private static int GetMaterialColor(Schema2.Material material)
        {
            var mb = new MaterialBuilder();
            material.CopyTo(mb);
            return GetMaterialColor(mb);
        }

        private static int GetMaterialColor(MaterialBuilder material)
        {
            var color = (material.GetChannel(KnownChannel.BaseColor) ?? material.GetChannel(KnownChannel.Diffuse))?.Parameter ?? Vector4.One * 0.8f;

            color *= 255;

            var ccc = color.X * 65536 + color.Y * 256 + color.Z;

            return (int)ccc;
        }
    }
}
