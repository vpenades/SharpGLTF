using System;
using System.Collections.Generic;
using System.Text;

namespace SharpGLTF.Runtime
{
    public enum MeshInstancing
    {
        Discard,
        Enabled,
        SingleMesh,
        // TODO: add options to trim the number of instances
    }

    public class RuntimeOptions
    {
        /// <summary>
        /// True if we want to copy buffers data instead of sharing it.
        /// </summary>
        public bool IsolateMemory { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether GPU instancing is enabled or disabled.
        /// </summary>
        /// <remarks>
        /// When true, if a gltf mesh has gpu instancing elements, they will be converted<br/>
        /// internally to the runtime as <see cref="InstancedDrawableTemplate"/> elements.
        /// </remarks>
        public MeshInstancing GpuMeshInstancing { get; set; } = MeshInstancing.Enabled;

        /// <summary>
        /// Gets or sets the custom extras converter.
        /// </summary>
        public Converter<Schema2.ExtraProperties, Object> ExtrasConverterCallback { get; set; }

        internal static Object ConvertExtras(Schema2.ExtraProperties source, RuntimeOptions options)
        {
            if (source.Extras.Content == null) return null;

            var callback = options?.ExtrasConverterCallback;

            return callback != null ? callback(source) : source.Extras.DeepClone();
        }
    }
}
