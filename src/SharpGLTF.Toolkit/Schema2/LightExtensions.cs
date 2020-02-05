using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace SharpGLTF.Schema2
{
    public static partial class Schema2Toolkit
    {
        /// <summary>
        /// Sets the cone angles for the <see cref="PunctualLightType.Spot"/> light.
        /// </summary>
        /// <param name="light">This <see cref="PunctualLight"/> instance.</param>
        /// <param name="innerConeAngle">
        /// Gets the Angle, in radians, from centre of spotlight where falloff begins.
        /// Must be greater than or equal to 0 and less than outerConeAngle.
        /// </param>
        /// <param name="outerConeAngle">
        /// Gets Angle, in radians, from centre of spotlight where falloff ends.
        /// Must be greater than innerConeAngle and less than or equal to PI / 2.0.
        /// </param>
        /// <returns>This <see cref="PunctualLight"/> instance.</returns>
        public static PunctualLight WithSpotCone(this PunctualLight light, float innerConeAngle, float outerConeAngle)
        {
            Guard.NotNull(light, nameof(light));

            light.SetSpotCone(innerConeAngle, outerConeAngle);
            return light;
        }

        /// <summary>
        /// Defines the light color, intensity and range for the current <see cref="PunctualLight"/>.
        /// </summary>
        /// <param name="light">This <see cref="PunctualLight"/> instance.</param>
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
        /// <returns>This <see cref="PunctualLight"/> instance.</returns>
        public static PunctualLight WithColor(this PunctualLight light, Vector3 color, float intensity = 1, float range = float.PositiveInfinity)
        {
            Guard.NotNull(light, nameof(light));

            light.Color = color;
            light.Intensity = intensity;
            light.Range = range;

            return light;
        }
    }
}
