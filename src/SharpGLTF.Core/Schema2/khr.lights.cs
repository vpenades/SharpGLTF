using System;
using System.Collections.Generic;
using System.Numerics;
using System.Linq;

using SharpGLTF.Collections;

namespace SharpGLTF.Schema2
{
    partial class KHR_lights_punctualglTFextension
    {
        internal KHR_lights_punctualglTFextension(ModelRoot root)
        {
            _lights = new ChildrenCollection<PunctualLight, ModelRoot>(root);
        }

        protected override IEnumerable<ExtraProperties> GetLogicalChildren()
        {
            return base.GetLogicalChildren().Concat(_lights);
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

    /// <summary>
    /// Defines all the types of <see cref="PunctualLight"/> types.
    /// </summary>
    public enum PunctualLightType
    {
        Directional,
        Point,
        Spot
    }

    /// <remarks>
    /// This is part of <see href="https://github.com/KhronosGroup/glTF/tree/master/extensions/2.0/Khronos/KHR_lights_punctual"/> extension.
    /// </remarks>
    [System.Diagnostics.DebuggerDisplay("{LightType} {Color} {Intensity} {Range}")]
    public sealed partial class PunctualLight
    {
        #region constants

        private const Double _rangeDefault = Double.PositiveInfinity;

        #endregion

        #region lifecycle

        internal PunctualLight() { }

        internal PunctualLight(PunctualLightType ltype)
        {
            _type = ltype.ToString().ToLowerInvariant();

            if (ltype == PunctualLightType.Spot) _spot = new PunctualLightSpot();
        }

        /// <summary>
        /// Sets the cone angles for the <see cref="PunctualLightType.Spot"/> light.
        /// </summary>
        /// <param name="innerConeAngle">
        /// Gets the Angle, in radians, from centre of spotlight where falloff begins.
        /// Must be greater than or equal to 0 and less than outerConeAngle.
        /// </param>
        /// <param name="outerConeAngle">
        /// Gets Angle, in radians, from centre of spotlight where falloff ends.
        /// Must be greater than innerConeAngle and less than or equal to PI / 2.0.
        /// </param>
        public void SetSpotCone(float innerConeAngle, float outerConeAngle)
        {
            if (_spot == null) throw new InvalidOperationException($"Expected {PunctualLightType.Spot} but found {LightType}");

            if (innerConeAngle > outerConeAngle) throw new ArgumentException($"{nameof(innerConeAngle)} must be equal or smaller than {nameof(outerConeAngle)}");

            _spot.InnerConeAngle = innerConeAngle;
            _spot.OuterConeAngle = outerConeAngle;
        }

        /// <summary>
        /// Defines the light color, intensity and range for the current <see cref="PunctualLight"/>.
        /// </summary>
        /// <param name="color">RGB value for light's color in linear space.</param>
        /// <param name="intensity">
        /// Brightness of light in. The units that this is defined in depend on the type of light.
        /// point and spot lights use luminous intensity in candela (lm/sr) while directional
        /// lights use illuminance in lux (lm/m2)
        /// </param>
        /// <param name="range">
        /// Hint defining a distance cutoff at which the light's intensity may be considered
        /// to have reached zero. Supported only for point and spot lights. Must be > 0.
        /// When undefined, range is assumed to be infinite.
        /// </param>
        public void SetColor(Vector3 color, float intensity = 1, float range = float.PositiveInfinity)
        {
            this.Color = color;
            this.Intensity = intensity;
            this.Range = range;
        }

        #endregion

        #region properties

        /// <summary>
        /// Gets the Local light direction.
        /// </summary>
        /// <remarks>
        /// For light types that have a direction (directional and spot lights),
        /// the light's direction is defined as the 3-vector (0.0, 0.0, -1.0)
        /// </remarks>
        public static Vector3 LocalDirection => -Vector3.UnitZ;

        /// <summary>
        /// Gets the zero-based index of this <see cref="PunctualLight"/> at <see cref="ModelRoot.LogicalPunctualLights"/>
        /// </summary>
        public int LogicalIndex => this.LogicalParent.LogicalPunctualLights.IndexOfReference(this);

        /// <summary>
        /// Gets the type of light.
        /// </summary>
        public PunctualLightType LightType => string.IsNullOrEmpty(_type) ? PunctualLightType.Directional : (PunctualLightType)Enum.Parse(typeof(PunctualLightType), _type, true);

        /// <summary>
        /// Gets the Angle, in radians, from centre of spotlight where falloff begins.
        /// Must be greater than or equal to 0 and less than outerConeAngle.
        /// </summary>
        public Single InnerConeAngle => this._spot == null ? 0 : (Single)this._spot.InnerConeAngle;

        /// <summary>
        /// Gets Angle, in radians, from centre of spotlight where falloff ends.
        /// Must be greater than innerConeAngle and less than or equal to PI / 2.0.
        /// </summary>
        public Single OuterConeAngle => this._spot == null ? 0 : (Single)this._spot.OuterConeAngle;

        /// <summary>
        /// Gets or sets the RGB value for light's color in linear space.
        /// </summary>
        public Vector3 Color
        {
            get => _color.AsValue(_colorDefault);
            set => _color = value.AsNullable(_colorDefault, Vector3.Zero, Vector3.One);
        }

        /// <summary>
        /// Gets or sets the Brightness of light in. The units that this is defined in depend on the type of light.
        /// point and spot lights use luminous intensity in candela (lm/sr) while directional
        /// lights use illuminance in lux (lm/m2)
        /// </summary>
        public Single Intensity
        {
            get => (Single)_intensity.AsValue(_intensityDefault);
            set => _intensity = ((double)value).AsNullable(_intensityDefault, _intensityMinimum, float.MaxValue);
        }

        /// <summary>
        /// Gets or sets a Hint defining a distance cutoff at which the light's intensity may be considered
        /// to have reached zero. Supported only for point and spot lights. Must be > 0.
        /// When undefined, range is assumed to be infinite.
        /// </summary>
        public Single Range
        {
            get => (Single)_range.AsValue(_rangeDefault);
            set
            {
                if (LightType == PunctualLightType.Directional) { _range = null; return; }

                _range = ((double)value).AsNullable(_rangeDefault, _rangeMinimum + 2e-07, float.MaxValue);
            }
        }

        #endregion

        #region API

        protected override IEnumerable<ExtraProperties> GetLogicalChildren()
        {
            var children = base.GetLogicalChildren();

            if (_spot != null) children = children.Concat(new[] { _spot });

            return children;
        }

        #endregion

        #region Validation

        protected override void OnValidateReferences(Validation.ValidationContext validate)
        {
            validate.IsAnyOf("Type", _type, "directional", "point", "spot");

            if (LightType == PunctualLightType.Spot) validate.IsDefined("Spot", _spot);

            base.OnValidateReferences(validate);
        }

        protected override void OnValidateContent(Validation.ValidationContext validate)
        {
            validate.IsDefaultOrWithin(nameof(Intensity), _intensity, _intensityMinimum, float.MaxValue);

            base.OnValidateContent(validate);
        }

        #endregion
    }

    partial class PunctualLightSpot
    {
        public Single InnerConeAngle
        {
            get => (Single)_innerConeAngle.AsValue(_innerConeAngleDefault);
            set => _innerConeAngle = value.AsNullable((Single)_innerConeAngleDefault, (Single)_innerConeAngleMinimum, (Single)_innerConeAngleMaximum);
        }

        public Single OuterConeAngle
        {
            get => (Single)_outerConeAngle.AsValue(_outerConeAngleDefault);
            set => _outerConeAngle = value.AsNullable((Single)_outerConeAngleDefault, (Single)_outerConeAngleMinimum, (Single)_outerConeAngleMaximum);
        }

        protected override void OnValidateContent(Validation.ValidationContext validate)
        {
            validate
                .IsDefaultOrWithin(nameof(InnerConeAngle), InnerConeAngle, (Single)_innerConeAngleMinimum, (Single)_innerConeAngleMaximum)
                .IsDefaultOrWithin(nameof(OuterConeAngle), OuterConeAngle, (Single)_outerConeAngleMinimum, (Single)_outerConeAngleMaximum)
                .IsLess(nameof(InnerConeAngle), InnerConeAngle, OuterConeAngle);

            base.OnValidateContent(validate);
        }
    }

    partial class KHR_lights_punctualnodeextension
    {
        #pragma warning disable CA1801 // Review unused parameters
        internal KHR_lights_punctualnodeextension(Node node) { }
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
                var ext = this.GetExtension<KHR_lights_punctualglTFextension>();
                if (ext == null) return Array.Empty<PunctualLight>();

                return ext.Lights;
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
            var ext = this.GetExtension<KHR_lights_punctualglTFextension>();
            if (ext == null)
            {
                this.UsingExtension(typeof(ModelRoot), typeof(KHR_lights_punctualglTFextension));

                ext = new KHR_lights_punctualglTFextension(this);
                this.SetExtension(ext);
            }

            return ext.CreateLight(name, lightType);
        }
    }
}
