using System;
using System.Collections.Generic;
using System.Text;
using System.Numerics;

namespace SharpGLTF.Schema2
{
    using Collections;
    using System.Linq;

    partial class KHR_lights_punctualglTFextension
    {
        internal KHR_lights_punctualglTFextension(ModelRoot root)
        {
            _lights = new ChildrenCollection<PunctualLight, ModelRoot>(root);
        }

        protected override IEnumerable<glTFProperty> GetLogicalChildren()
        {
            return base.GetLogicalChildren().Concat(_lights);
        }

        public IReadOnlyList<PunctualLight> Lights => _lights;

        public PunctualLight CreateLight(string name = null)
        {
            var light = new PunctualLight();
            light.Name = name;

            _lights.Add(light);

            return light;
        }
    }

    public enum PunctualLightType { Directional, Point, Spot }

    /// <remarks>
    /// This is part of <see cref="https://github.com/KhronosGroup/glTF/tree/master/extensions/2.0/Khronos/KHR_lights_punctual"/> extension.
    /// </remarks>
    [System.Diagnostics.DebuggerDisplay("{LightType} {Color} {Intensity} {Range}")]
    public partial class PunctualLight
    {
        /// <summary>
        /// Gets the zero-based index of this <see cref="PunctualLight"/> at <see cref="ModelRoot.LogicalPunctualLights"/>
        /// </summary>
        public int LogicalIndex => this.LogicalParent.LogicalPunctualLights.IndexOfReference(this);

        public Vector3 Color
        {
            get => _color.AsValue(_colorDefault);
            set => _color = value.AsNullable(_colorDefault, Vector3.Zero, Vector3.One);
        }

        public Double Intensity
        {
            get => _intensity.AsValue(_intensityDefault);
            set => _intensity = value.AsNullable(_intensityDefault, _intensityMinimum, float.MaxValue);
        }

        public Double Range
        {
            get => _range.AsValue(0);
            set => _range = value.AsNullable(0, _rangeMinimum, float.MaxValue);
        }

        public PunctualLightType LightType
        {
            get => (PunctualLightType)Enum.Parse(typeof(PunctualLightType), _type, true);
            set
            {
                this._type = value.ToString().ToLower();
                if (value != PunctualLightType.Spot) this._spot = null;
                else if (this._spot == null) this._spot = new PunctualLightSpot();
            }
        }

        public float InnerConeAngle
        {
            get => this._spot == null ? 0 : (float)this._spot.InnerConeAngle;
            set { if (this._spot != null) this._spot.InnerConeAngle = value; }
        }

        public float OuterConeAngle
        {
            get => this._spot == null ? 0 : (float)this._spot.OuterConeAngle;
            set { if (this._spot != null) this._spot.OuterConeAngle = value; }
        }
    }

    partial class PunctualLightSpot
    {
        public Double InnerConeAngle
        {
            get => _innerConeAngle.AsValue(_innerConeAngleDefault);
            set => _innerConeAngle = value.AsNullable(_innerConeAngleDefault, _innerConeAngleMinimum, _innerConeAngleMaximum);
        }

        public Double OuterConeAngle
        {
            get => _outerConeAngle.AsValue(_outerConeAngleDefault);
            set => _outerConeAngle = value.AsNullable(_outerConeAngleDefault, _outerConeAngleMinimum, _outerConeAngleMaximum);
        }
    }

    partial class ModelRoot
    {
        /// <summary>
        /// A collection of <see cref="PunctualLight"/> instances.
        /// </summary>
        /// <remarks>
        /// This is part of <see cref="https://github.com/KhronosGroup/glTF/tree/master/extensions/2.0/Khronos/KHR_lights_punctual">KHR_lights_punctual</see> extension.
        /// </remarks>
        public IReadOnlyList<PunctualLight> LogicalPunctualLights
        {
            get
            {
                var ext = this.GetExtension<KHR_lights_punctualglTFextension>();
                if (ext == null) return new PunctualLight[0];

                return ext.Lights;
            }
        }

        /// <summary>
        /// Creates a new <see cref="PunctualLight"/> instance.
        /// and adds it to <see cref="ModelRoot.LogicalPunctualLights"/>.
        /// </summary>
        /// <param name="name">The name of the instance.</param>
        /// <returns>A <see cref="PunctualLight"/> instance.</returns>
        public PunctualLight CreatePunctualLight(string name = null)
        {
            var ext = this.GetExtension<KHR_lights_punctualglTFextension>();
            if (ext == null)
            {
                this.UsingExtension(typeof(ModelRoot), typeof(KHR_lights_punctualglTFextension));

                ext = new KHR_lights_punctualglTFextension(this);
                this.SetExtension(ext);
            }

            return ext.CreateLight(name);
        }
    }

    partial class KHR_lights_punctualnodeextension
    {
        internal KHR_lights_punctualnodeextension(Node node)
        {
        }

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
        /// This is part of <see cref="https://github.com/KhronosGroup/glTF/tree/master/extensions/2.0/Khronos/KHR_lights_punctual"/> extension.
        /// </remarks>
        public PunctualLight PunctualLight
        {
            get
            {
                var ext = this.GetExtension<KHR_lights_punctualnodeextension>();
                if (ext == null) return null;

                return this.LogicalParent.LogicalPunctualLights[ext.LightIndex];
            }
            set
            {
                if (value == null) { this.RemoveExtensions<KHR_lights_punctualnodeextension>(); return; }

                Guard.MustShareLogicalParent(this, value, nameof(value));

                this.UsingExtension(typeof(KHR_lights_punctualnodeextension));

                var ext = new KHR_lights_punctualnodeextension(this);
                ext.LightIndex = value.LogicalIndex;

                this.SetExtension(ext);
            }
        }
    }
}
