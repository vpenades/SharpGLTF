using SharpGLTF.Schema2;

namespace SharpGLTF
{
    internal static class Validator
    {
        internal static void ValidateAccessor(ModelRoot model, Accessor accessor)
        {
            Guard.NotNull(accessor, nameof(accessor));
            Guard.MustShareLogicalParent(model, "this", accessor, nameof(accessor));
            Guard.IsTrue(accessor.Encoding == EncodingType.UNSIGNED_INT, nameof(accessor));
            Guard.IsTrue(accessor.Dimensions == DimensionType.SCALAR, nameof(accessor));
            Guard.IsFalse(accessor.Normalized, nameof(accessor));
        }
    }
}
