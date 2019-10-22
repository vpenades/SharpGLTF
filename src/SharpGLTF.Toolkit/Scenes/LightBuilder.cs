using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace SharpGLTF.Scenes
{
    public abstract class LightBuilder
    {
        protected LightBuilder(Schema2.PunctualLight light)
        {
            Guard.NotNull(light, nameof(light));

            this.Color = light.Color;
            this.Intensity = light.Intensity;
        }

        public static Vector3 LocalDirection => -Vector3.UnitZ;

        /// <summary>
        /// Gets or sets the RGB value for light's color in linear space.
        /// </summary>
        public Vector3 Color { get; set; }

        /// <summary>
        /// Gets or sets the Brightness of light in. The units that this is defined in depend on the type of light.
        /// point and spot lights use luminous intensity in candela (lm/sr) while directional
        /// lights use illuminance in lux (lm/m2)
        /// </summary>
        public Single Intensity { get; set; }

        #region types

        #pragma warning disable CA1034 // Nested types should not be visible

        [System.Diagnostics.DebuggerDisplay("Directional")]
        public sealed class Directional : LightBuilder
        {
            internal Directional(Schema2.PunctualLight light)
                : base(light) { }
        }

        [System.Diagnostics.DebuggerDisplay("Point")]
        public sealed class Point : LightBuilder
        {
            internal Point(Schema2.PunctualLight light)
                : base(light)
            {
                this.Range = light.Range;
            }

            /// <summary>
            /// Gets or sets a Hint defining a distance cutoff at which the light's intensity may be considered
            /// to have reached zero. Supported only for point and spot lights. Must be > 0.
            /// When undefined, range is assumed to be infinite.
            /// </summary>
            public Single Range { get; set; }
        }

        [System.Diagnostics.DebuggerDisplay("Spot")]

        public sealed class Spot : LightBuilder
        {
            internal Spot(Schema2.PunctualLight light)
                : base(light)
            {
                this.Range = light.Range;
                this.InnerConeAngle = light.InnerConeAngle;
                this.OuterConeAngle = light.OuterConeAngle;
            }

            /// <summary>
            /// Gets or sets a Hint defining a distance cutoff at which the light's intensity may be considered
            /// to have reached zero. Supported only for point and spot lights. Must be > 0.
            /// When undefined, range is assumed to be infinite.
            /// </summary>
            public Single Range { get; set; }

            /// <summary>
            /// Gets or sets the Angle, in radians, from centre of spotlight where falloff begins.
            /// Must be greater than or equal to 0 and less than outerConeAngle.
            /// </summary>
            public Single InnerConeAngle { get; set; }

            /// <summary>
            /// Gets or sets Angle, in radians, from centre of spotlight where falloff ends.
            /// Must be greater than innerConeAngle and less than or equal to PI / 2.0.
            /// </summary>
            public Single OuterConeAngle { get; set; }
        }

        #pragma warning restore CA1034 // Nested types should not be visible

        #endregion
    }
}
