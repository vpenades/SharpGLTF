using System;
using System.Collections.Generic;
using System.Text;
using System.Numerics;

namespace SharpGLTF.Schema2
{
    using Collections;

    partial class KHR_lights_punctualglTFextension
    {
        public ChildrenCollection<PunctualLight, ModelRoot> GetLightsCollection(ModelRoot root)
        {
            if (_lights == null) _lights = new ChildrenCollection<PunctualLight, ModelRoot>(root);

            return _lights;
        }
    }

    public partial class PunctualLight
    {
        /// <summary>
        /// Gets the zero-based index of this <see cref="PunctualLight"/> at <see cref="ModelRoot.LogicalPunctualLights"/>
        /// </summary>
        public int LogicalIndex => this.LogicalParent.LogicalPunctualLights.IndexOfReference(this);

        public Vector3 Color
        {
            get => _color.AsValue(_colorDefault);
            set => _color = value.AsNullable(_colorDefault);
        }

        public Double Intensity
        {
            get => _intensity.AsValue(_intensityDefault);
            set => _intensity = value.AsNullable(_intensityDefault, _intensityMinimum, double.MaxValue);
        }

        public Double Range
        {
            get => _range.AsValue(0);
            set => _range = value.AsNullable(0, _rangeMinimum, double.MaxValue);
        }

        public string LightType => _type;
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
        public IReadOnlyList<PunctualLight> LogicalPunctualLights
        {
            get
            {
                var ext = this.GetExtension<KHR_lights_punctualglTFextension>();
                if (ext == null) return new PunctualLight[0];

                return ext.GetLightsCollection(this);
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
                ext = new KHR_lights_punctualglTFextension();
                this.SetExtension(ext);
            }

            var light = new PunctualLight();
            light.Name = name;

            ext.GetLightsCollection(this).Add(light);

            return light;
        }
    }

    partial class KHR_lights_punctualnodeextension
    {
        public int LightIndex
        {
            get => _light;
            set => _light = value;
        }
    }

    partial class Node
    {
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

                var ext = new KHR_lights_punctualnodeextension();
                ext.LightIndex = value.LogicalIndex;

                this.SetExtension(ext);
            }
        }
    }
}
