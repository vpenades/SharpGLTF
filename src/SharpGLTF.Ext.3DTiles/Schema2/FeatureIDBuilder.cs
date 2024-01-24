using System;
using System.Collections.Generic;
using OneOf;

namespace SharpGLTF.Schema2.Tiles3D
{
    public sealed class FeatureIDBuilder : IMeshFeatureIDInfo, IEquatable<IMeshFeatureIDInfo>
    {
        public FeatureIDBuilder(int featureCount, OneOf<int, Texture>? attributeOrTexture = null, PropertyTable propertyTable = null, IReadOnlyList<int> channels = null, string label = null, int? nullFeatureId = null)
        {
            Guard.MustBeGreaterThanOrEqualTo(featureCount, 1, nameof(featureCount));
            FeatureCount = featureCount;

            if (attributeOrTexture != null)
            {
                attributeOrTexture.Value.Switch(
                    attribute => Attribute = attribute,
                    texture =>
                    {
                        Texture = texture;
                        Channels = channels ?? new[] { 0 };
                    }
                );
            }

            if (propertyTable != null)
            {
                PropertyTableIndex = propertyTable.LogicalIndex;
            }

            Label = label;
            NullFeatureId = nullFeatureId;
        }

        public int FeatureCount { get; set; }
        public int? NullFeatureId { get; set; }
        public int? Attribute { get; set; }
        public string Label { get; set; }

        public int? PropertyTableIndex { get; set; }

        public Texture Texture { get; set;  }
        public IReadOnlyList<int> Channels { get; set; }

        public bool Equals(IMeshFeatureIDInfo other)
        {
            if (other == null) return false;
            if (FeatureCount != other.FeatureCount) return false;
            if (NullFeatureId != other.NullFeatureId) return false;
            if (Attribute != other.Attribute) return false;
            if (Label != other.Label) return false;
            if (PropertyTableIndex != other.PropertyTableIndex) return false;

            if (other is FeatureIDBuilder featureIdBuilder)
            {
                if (Texture != featureIdBuilder.Texture) return false;
                if (Channels != featureIdBuilder.Channels) return false;
            }

            return true;
        }
    }
}
