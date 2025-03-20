using System;
using System.Linq;
using System.Numerics;

namespace SharpGLTF.Schema2
{
    /// <summary>
    /// Represents a 2D transform applied to the UV coordinates of a material.
    /// </summary>
    /// <remarks>
    /// Child of <see cref="TextureInfo.Transform"/>
    /// </remarks>
    [System.Diagnostics.DebuggerDisplay("TextureTransform {Offset} {Scale} {Rotation} {TextureCoordinate}")]
    public sealed partial class TextureTransform
    {
        #region lifecycle

        #pragma warning disable CA1801 // Review unused parameters
        internal TextureTransform(TextureInfo parent) { _Parent = parent; }
        #pragma warning restore CA1801 // Review unused parameters

        #endregion

        #region properties

        internal TextureInfo _Parent;

        public Vector2 Offset
        {
            get => _offset.AsValue(_offsetDefault);
            set => _offset = value.AsNullable(_offsetDefault);
        }

        public Vector2 Scale
        {
            get => _scale.AsValue(_scaleDefault);
            set => _scale = value.AsNullable(_scaleDefault);
        }

        public float Rotation
        {
            get => (float)_rotation.AsValue(_rotationDefault);
            set => _rotation = ((double)value).AsNullable(_rotationDefault);
        }

        /// <summary>
        /// Gets or sets a value that overrides <see cref="TextureInfo.TextureCoordinate"/> if supplied, and if this extension is supported.
        /// </summary>
        public int? TextureCoordinateOverride
        {
            get => _texCoord;
            set => _texCoord = value;
        }

        internal bool IsDefault
        {
            get
            {
                if (_texCoord.HasValue) return false;
                if (_offset.HasValue) return false;
                if (_scale.HasValue) return false;
                if (_rotation.HasValue) return false;
                return true;
            }
        }

        public Matrix3x2 Matrix
        {
            get
            {
                var s = Matrix3x2.CreateScale(Scale);
                var r = Matrix3x2.CreateRotation(-Rotation);
                var t = Matrix3x2.CreateTranslation(Offset);

                return s * r * t;
            }
        }

        #endregion

        #region API

        private string _GetAnimationPointer(string propertyName)
        {
            switch (propertyName)
            {
                case "offset": break;
                case "rotation": break;
                case "scale": break;
                default: throw new ArgumentException("invalid property", nameof(propertyName));
            }

            var pointerPath = _Parent._GetAnimationPointer() + $"/extensions/KHR_texture_transform/{propertyName}";

            return pointerPath;
        }

        public Matrix3x2 GetMatrix(Animation track, float time)
        {
            if (track == null) return this.Matrix;

            var scale = track
                .FindChannels(_GetAnimationPointer("scale"))
                .FirstOrDefault()?.GetSamplerOrNull<Vector2>()?.CreateCurveSampler()?.GetPoint(time)
                ?? this.Scale;

            var rotation = track
                .FindChannels(_GetAnimationPointer("rotation"))
                .FirstOrDefault()?.GetSamplerOrNull<Single>()?.CreateCurveSampler()?.GetPoint(time)
                ?? this.Rotation;

            var offset = track
                .FindChannels(_GetAnimationPointer("offset"))
                .FirstOrDefault()?.GetSamplerOrNull<Vector2>()?.CreateCurveSampler()?.GetPoint(time)
                ?? this.Offset;

            var s = Matrix3x2.CreateScale(scale);
            var r = Matrix3x2.CreateRotation(-rotation);
            var t = Matrix3x2.CreateTranslation(offset);

            return s * r * t;
        }

        internal void CopyTo(TextureTransform other)
        {
            if (other == null) throw new ArgumentNullException(nameof(other));
            other.TextureCoordinateOverride = this.TextureCoordinateOverride;
            other.Rotation = this.Rotation;
            other.Offset = this.Offset;
            other.Scale = this.Scale;
        }

        #endregion
    }


}
