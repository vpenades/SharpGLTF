using System;
using System.Collections.Generic;
using System.Text;

namespace SharpGLTF.Runtime
{
    public class RuntimeOptions
    {
        /// <summary>
        /// True if we want to copy buffers data instead of sharing it.
        /// </summary>
        public bool IsolateMemory { get; set; }

        /// <summary>
        /// Custom extras converter.
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
