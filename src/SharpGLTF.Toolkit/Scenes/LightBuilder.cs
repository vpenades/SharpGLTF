using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace SharpGLTF.Scenes
{
    /// <summary>
    /// Represents the base class light object.
    /// </summary>
    /// <remarks>
    /// Derived types are:<br/>
    /// - <see cref="Directional"/><br/>
    /// - <see cref="Point"/><br/>
    /// - <see cref="Spot"/><br/>
    /// </remarks>
    public abstract class LightBuilder : BaseBuilder
    {
        #region lifecycle        
        protected LightBuilder()
        {
            this.Color = Vector3.One;
            this.Intensity = 1f;
        }

        protected LightBuilder(Schema2.PunctualLight light)
        {
            Guard.NotNull(light, nameof(light));

            this.SetNameAndExtrasFrom(light);

            this.Color = light.Color;
            this.Intensity = light.Intensity;
        }

        public abstract LightBuilder Clone();

        protected LightBuilder(LightBuilder other)
            : base(other)
        {
            Guard.NotNull(other, nameof(other));

            this.Color = other.Color;
            this.Intensity = other.Intensity;
        }

        #endregion

        #region data

        public static Vector3 LocalDirection => -Vector3.UnitZ;

        /// <summary>
        /// Gets or sets the RGB value for light's color in linear space.
        /// </summary>
        public Vector3 Color { get; set; }

        /// <summary>
        /// Gets or sets the Brightness of light in.<br/>
        /// The units that this is defined in depend on the type of light.<br/>
        /// Point and spot lights use luminous intensity in candela (lm/sr)
        /// while directional lights use illuminance in lux (lm/m2)
        /// </summary>
        public Single Intensity { get; set; }

        #endregion

        #region Nested types

        /// <inheritdoc/>
        [System.Diagnostics.DebuggerDisplay("LightBuilder.Directional")]
        public sealed class Directional : LightBuilder
        {
            #region lifecycle

            public Directional()
                : base()
            { }

            internal Directional(Schema2.PunctualLight light)
                : base(light)
            {
                System.Diagnostics.Debug.Assert(light.LightType == Schema2.PunctualLightType.Directional);
            }

            public override LightBuilder Clone()
            {
                return new Directional(this);
            }

            private Directional(Directional other)
                : base(other) { }

            #endregion
        }

        /// <inheritdoc/>
        [System.Diagnostics.DebuggerDisplay("LightBuilder.Point")]
        public sealed class Point : LightBuilder
        {
            #region lifecycle

            public Point()
                : base()
            {
                this.Range = float.PositiveInfinity;
            }

            internal Point(Schema2.PunctualLight light)
                : base(light)
            {
                System.Diagnostics.Debug.Assert(light.LightType == Schema2.PunctualLightType.Point);
                this.Range = light.Range;
            }

            public override LightBuilder Clone()
            {
                return new Point(this);
            }

            private Point(Point other)
                : base(other)
            {
                this.Range = other.Range;
            }

            #endregion

            #region data

            /// <summary>
            /// Gets or sets a Hint defining a distance cutoff at which the
            /// light's intensity may be considered to have reached zero.<br/>
            /// Supported only for point and spot lights. Must be > 0.<br/>
            /// When undefined, range is assumed to be infinite.
            /// </summary>
            public Single Range { get; set; }

            #endregion
        }

        /// <inheritdoc/>
        [System.Diagnostics.DebuggerDisplay("LightBuilder.Spot")]
        public sealed class Spot : LightBuilder
        {
            #region lifecycle

            public Spot()
                : base()
            {
                this.Range = float.PositiveInfinity;
                this.InnerConeAngle = 0;
                this.OuterConeAngle = 0.7853981633974483f;
            }

            internal Spot(Schema2.PunctualLight light)
                : base(light)
            {
                System.Diagnostics.Debug.Assert(light.LightType == Schema2.PunctualLightType.Spot);
                this.Range = light.Range;
                this.InnerConeAngle = light.InnerConeAngle;
                this.OuterConeAngle = light.OuterConeAngle;
            }

            public override LightBuilder Clone()
            {
                return new Spot(this);
            }

            private Spot(Spot other)
                : base(other)
            {
                this.Range = other.Range;
                this.InnerConeAngle = other.InnerConeAngle;
                this.OuterConeAngle = other.OuterConeAngle;
            }

            #endregion

            #region data

            /// <summary>
            /// Gets or sets a Hint defining a distance cutoff at which the
            /// light's intensity may be considered to have reached zero.<br/>
            /// Supported only for point and spot lights. Must be > 0.<br/>
            /// When undefined, range is assumed to be infinite.
            /// </summary>
            public Single Range { get; set; }

            /// <summary>
            /// Gets or sets the Angle, in radians, from centre of spotlight where falloff begins.<br/>
            /// Must be greater than or equal to 0 and less than outerConeAngle.
            /// </summary>
            public Single InnerConeAngle { get; set; }

            /// <summary>
            /// Gets or sets Angle, in radians, from centre of spotlight where falloff ends.<br/>
            /// Must be greater than innerConeAngle and less than or equal to PI / 2.0.
            /// </summary>
            public Single OuterConeAngle { get; set; }

            #endregion
        }

        #endregion
    }
}
