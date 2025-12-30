using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.Xna.Framework.Graphics;

using SharpGLTF.Runtime.Template;

namespace SharpGLTF.Runtime.Pipeline
{
    /// <summary>
    /// This is the entry point class used to load glTF models and its resources into MonoGame.
    /// </summary>
    public static class TemplateFactory
    {
        public static MonoGameDeviceContent<MonoGameModelTemplate> LoadDeviceModel(GraphicsDevice device, string filePath, MeshesFactory context = null)
        {
            var model = Schema2.ModelRoot.Load(filePath, Validation.ValidationMode.TryFix);

            return CreateDeviceModel(device, model, context);
        }

        public static MonoGameDeviceContent<MonoGameModelTemplate> CreateDeviceModel(GraphicsDevice device, Schema2.ModelRoot srcModel, MeshesFactory context = null)
        {
            context ??= MeshesFactory.Create(device);

            context.Reset();

            var options = new RuntimeOptions { IsolateMemory = true };

            var templates = srcModel.LogicalScenes
                .Select(item => SceneTemplate.Create(item, options))
                .ToArray();

            var srcMeshes = templates
                .SelectMany(item => item.LogicalMeshIds)
                .Distinct()
                .Select(idx => srcModel.LogicalMeshes[idx]);

            var dstMeshes = context.CreateRuntimeMeshes(srcMeshes);

            var mdl = new MonoGameModelTemplate(templates, srcModel.DefaultScene.LogicalIndex, dstMeshes);

            return new MonoGameDeviceContent<MonoGameModelTemplate>(mdl, context.Disposables.ToArray());
        }
    }
}
