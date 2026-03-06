using System;

namespace SharpGLTF.Schema2
{
    public partial class GaussianSplatting
    {
        #region lifecycle

        internal GaussianSplatting(MeshPrimitive primitive)
        {
            _Owner = primitive;
        }

        #endregion

        #region constants

        public const string KernelEllipse = "ellipse";

        public const string ColorSpaceSrgbRec709Display = "srgb_rec709_display";
        public const string ColorSpaceLinRec709Display = "lin_rec709_display";

        public const string ProjectionPerspective = "perspective";
        public const string SortingMethodCameraDistance = "cameraDistance";

        public const string AttributeRotation = "KHR_gaussian_splatting:ROTATION";
        public const string AttributeScale = "KHR_gaussian_splatting:SCALE";
        public const string AttributeOpacity = "KHR_gaussian_splatting:OPACITY";
        public const string AttributeSHDegree0Coef0 = "KHR_gaussian_splatting:SH_DEGREE_0_COEF_0";

        #endregion

        #region data (not serializable)

        private readonly MeshPrimitive _Owner;

        #endregion

        #region properties

        public MeshPrimitive LogicalParent => _Owner;

        public string Kernel
        {
            get => _kernel;
            set
            {
                Guard.NotNullOrEmpty(value, nameof(value));
                _kernel = value;
            }
        }

        public string ColorSpace
        {
            get => _colorSpace;
            set
            {
                Guard.NotNullOrEmpty(value, nameof(value));
                _colorSpace = value;
            }
        }

        public string Projection
        {
            get => _projection ?? _projectionDefault;
            set
            {
                value = value.AsEmptyNullable();
                _projection = value == _projectionDefault ? null : value;
            }
        }

        public string SortingMethod
        {
            get => _sortingMethod ?? _sortingMethodDefault;
            set
            {
                value = value.AsEmptyNullable();
                _sortingMethod = value == _sortingMethodDefault ? null : value;
            }
        }

        #endregion

        #region API

        public static string GetSphericalHarmonicsAttribute(int degree, int coefficient)
        {
            Guard.MustBeBetweenOrEqualTo(degree, 0, 3, nameof(degree));

            var coefficientCount = 2 * degree + 1;
            Guard.MustBeBetweenOrEqualTo(coefficient, 0, coefficientCount - 1, nameof(coefficient));

            return $"KHR_gaussian_splatting:SH_DEGREE_{degree}_COEF_{coefficient}";
        }

        #endregion
    }

    public sealed partial class MeshPrimitive
    {
        private static readonly string[] _GaussianRequiredAttributes =
        {
            GaussianSplatting.AttributeRotation,
            GaussianSplatting.AttributeScale,
            GaussianSplatting.AttributeOpacity,
            GaussianSplatting.AttributeSHDegree0Coef0
        };

        public GaussianSplatting GetGaussianSplatting()
        {
            return this.GetExtension<GaussianSplatting>();
        }

        public GaussianSplatting UseGaussianSplatting()
        {
            var ext = GetGaussianSplatting();
            if (ext == null)
            {
                ext = new GaussianSplatting(this);
                this.SetExtension(ext);
            }

            return ext;
        }

        public void RemoveGaussianSplatting()
        {
            this.RemoveExtensions<GaussianSplatting>();
        }

        private void _ValidateGaussianSplatting(Validation.ValidationContext validate)
        {
            var gaussian = GetGaussianSplatting();
            if (gaussian == null) return;

            validate.EnumsAreEqual(nameof(DrawPrimitiveType), DrawPrimitiveType, PrimitiveType.POINTS);
            validate.IsDefined("Attributes.POSITION", GetVertexAccessor("POSITION"));

            foreach (var semantic in _GaussianRequiredAttributes)
            {
                validate.IsDefined($"Attributes.{semantic}", GetVertexAccessor(semantic));
            }

            var rotation = GetVertexAccessor(GaussianSplatting.AttributeRotation);
            if (rotation != null)
            {
                validate.IsAnyOf
                (
                    $"Attributes.{GaussianSplatting.AttributeRotation}.Format",
                    rotation.Format,
                    (DimensionType.VEC4, EncodingType.FLOAT),
                    (DimensionType.VEC4, EncodingType.BYTE, true),
                    (DimensionType.VEC4, EncodingType.SHORT, true)
                );
            }

            var scale = GetVertexAccessor(GaussianSplatting.AttributeScale);
            if (scale != null)
            {
                validate.IsAnyOf
                (
                    $"Attributes.{GaussianSplatting.AttributeScale}.Format",
                    scale.Format,
                    (DimensionType.VEC3, EncodingType.FLOAT),
                    (DimensionType.VEC3, EncodingType.BYTE),
                    (DimensionType.VEC3, EncodingType.BYTE, true),
                    (DimensionType.VEC3, EncodingType.SHORT),
                    (DimensionType.VEC3, EncodingType.SHORT, true)
                );
            }

            var opacity = GetVertexAccessor(GaussianSplatting.AttributeOpacity);
            if (opacity != null)
            {
                validate.IsAnyOf
                (
                    $"Attributes.{GaussianSplatting.AttributeOpacity}.Format",
                    opacity.Format,
                    (DimensionType.SCALAR, EncodingType.FLOAT),
                    (DimensionType.SCALAR, EncodingType.UNSIGNED_BYTE, true),
                    (DimensionType.SCALAR, EncodingType.UNSIGNED_SHORT, true)
                );
            }

            var sh0 = GetVertexAccessor(GaussianSplatting.AttributeSHDegree0Coef0);
            if (sh0 != null)
            {
                validate.IsAnyOf
                (
                    $"Attributes.{GaussianSplatting.AttributeSHDegree0Coef0}.Format",
                    sh0.Format,
                    (DimensionType.VEC3, EncodingType.FLOAT)
                );
            }

            validate.IsTrue(nameof(gaussian.Kernel), !string.IsNullOrWhiteSpace(gaussian.Kernel), "must be defined.");
            validate.IsTrue(nameof(gaussian.ColorSpace), !string.IsNullOrWhiteSpace(gaussian.ColorSpace), "must be defined.");
        }
    }
}
