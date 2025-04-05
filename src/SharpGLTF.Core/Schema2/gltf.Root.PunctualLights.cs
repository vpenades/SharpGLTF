using System;
using System.Collections.Generic;
using System.Numerics;
using System.Linq;

using SharpGLTF.Collections;

namespace SharpGLTF.Schema2
{
    partial class _ModelPunctualLights
    {
        internal _ModelPunctualLights(ModelRoot root)
        {
            _lights = new ChildrenList<PunctualLight, ModelRoot>(root);
        }

        public IReadOnlyList<PunctualLight> Lights => _lights;

        public PunctualLight CreateLight(string name, PunctualLightType ltype)
        {
            var light = new PunctualLight(ltype);
            light.Name = name;

            _lights.Add(light);

            return light;
        }
    }

    partial class ModelRoot
    {
        /// <summary>
        /// Gets A collection of <see cref="PunctualLight"/> instances.
        /// </summary>
        /// <remarks>
        /// This is part of <see href="https://github.com/KhronosGroup/glTF/tree/master/extensions/2.0/Khronos/KHR_lights_punctual">KHR_lights_punctual</see> extension.
        /// </remarks>
        public IReadOnlyList<PunctualLight> LogicalPunctualLights
        {
            get
            {
                var ext = this.GetExtension<_ModelPunctualLights>();
                return ext == null ? Array.Empty<PunctualLight>() : ext.Lights;
            }
        }

        /// <summary>
        /// Creates a new <see cref="PunctualLight"/> instance and
        /// adds it to <see cref="ModelRoot.LogicalPunctualLights"/>.
        /// </summary>
        /// <param name="lightType">A value of <see cref="PunctualLightType"/> describing the type of light to create.</param>
        /// <returns>A <see cref="PunctualLight"/> instance.</returns>
        public PunctualLight CreatePunctualLight(PunctualLightType lightType)
        {
            return CreatePunctualLight(null, lightType);
        }

        /// <summary>
        /// Creates a new <see cref="PunctualLight"/> instance.
        /// and adds it to <see cref="ModelRoot.LogicalPunctualLights"/>.
        /// </summary>
        /// <param name="name">The name of the instance.</param>
        /// <param name="lightType">A value of <see cref="PunctualLightType"/> describing the type of light to create.</param>
        /// <returns>A <see cref="PunctualLight"/> instance.</returns>
        public PunctualLight CreatePunctualLight(string name, PunctualLightType lightType)
        {
            return UseExtension<_ModelPunctualLights>().CreateLight(name, lightType);
        }
    }
}
