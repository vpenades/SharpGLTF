using System;
using System.Collections.Generic;
using System.Text;

namespace SharpGLTF.Schema2
{
    partial class _NodePunctualLight
    {
        #pragma warning disable CA1801 // Review unused parameters
        internal _NodePunctualLight(Node node) { }
        #pragma warning restore CA1801 // Review unused parameters

        public int LightIndex
        {
            get => _light;
            set => _light = value;
        }
    }

    partial class Node
    {
        /// <summary>
        /// Gets or sets the <see cref="Schema2.PunctualLight"/> of this <see cref="Node"/>.
        /// </summary>
        /// <remarks>
        /// This is part of <see href="https://github.com/KhronosGroup/glTF/tree/master/extensions/2.0/Khronos/KHR_lights_punctual"/> extension.
        /// </remarks>
        public PunctualLight PunctualLight
        {
            get
            {
                var ext = this.GetExtension<_NodePunctualLight>();
                if (ext == null) return null;

                return this.LogicalParent.LogicalPunctualLights[ext.LightIndex];
            }
            set
            {
                if (value == null) { this.RemoveExtensions<_NodePunctualLight>(); return; }

                Guard.MustShareLogicalParent(this, value, nameof(value));

                // this.UsingExtension(typeof(_NodePunctualLight));

                var ext = new _NodePunctualLight(this);
                ext.LightIndex = value.LogicalIndex;

                this.SetExtension(ext);
            }
        }
    }
}
