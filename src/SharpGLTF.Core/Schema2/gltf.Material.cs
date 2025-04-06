using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;

namespace SharpGLTF.Schema2
{
    [System.Diagnostics.DebuggerDisplay("Material[{LogicalIndex}] {Name}")]
    public sealed partial class Material
    {
        #region lifecycle

        internal Material() { }

        #endregion

        #region properties

        /// <summary>
        /// Gets or sets the <see cref="AlphaMode"/>.
        /// </summary>
        public AlphaMode Alpha
        {
            get => _alphaMode.AsValue(_alphaModeDefault);
            set => _alphaMode = value.AsNullable(_alphaModeDefault);
        }

        /// <summary>
        /// Gets or sets the <see cref="AlphaCutoff"/> value.<br/>
        /// It needs to be used in combination with <see cref="Alpha"/> = <see cref="AlphaMode.MASK"/>.
        /// </summary>
        public Single AlphaCutoff
        {
            get => (Single)_alphaCutoff.AsValue(_alphaCutoffDefault);
            set => _alphaCutoff = ((Double)value).AsNullable(_alphaCutoffDefault, _alphaCutoffMinimum, double.MaxValue);
        }

        /// <summary>
        /// Gets or sets a value indicating whether this <see cref="Material"/> will render as Double Sided.<br/>
        /// Default value: False
        /// </summary>
        public Boolean DoubleSided
        {
            get => _doubleSided.AsValue(_doubleSidedDefault);
            set => _doubleSided = value.AsNullable(_doubleSidedDefault);
        }

        /// <summary>
        /// Gets a value indicating whether this <see cref="Material"/> instance has Unlit extension.
        /// </summary>
        public Boolean Unlit => this.GetExtension<MaterialUnlit>() != null;

        /// <summary>
        /// Gets a collection of <see cref="MaterialChannel"/> elements available in this <see cref="Material"/> instance.
        /// </summary>
        public IEnumerable<MaterialChannel> Channels => _GetChannels();

        /// <summary>
        /// Gets or sets the index of refraction.
        /// </summary>
        /// <remarks>
        /// <para>
        /// The index of refraction (IOR) is a measured physical number usually in the range between 1 and 2
        /// that determines how much the path of light is bent, or refracted, when entering a material.
        /// It also influences the ratio between reflected and transmitted light, calculated from the Fresnel equations.
        /// </para>
        /// <para>
        /// This property backs KHR_Materials_IOR extension.
        /// </para>
        /// </remarks>
        public float IndexOfRefraction
        {
            get => this.GetExtension<MaterialIOR>()?.IndexOfRefraction ?? MaterialIOR.DefaultIndexOfRefraction;
            set
            {
                if (this.GetExtension<MaterialUnlit>() != null) return;
                if (this.GetExtension<MaterialPBRSpecularGlossiness>() != null) return;

                // setting the IOR to its default value
                // essentially makes the extension unneccesary.
                if (value == MaterialIOR.DefaultIndexOfRefraction)
                {
                    this.RemoveExtensions<MaterialIOR>();
                    return;
                }

                this.UseExtension<MaterialIOR>().IndexOfRefraction = value;
            }
        }

        /// <summary>
        /// Gets or sets the Dispersion.
        /// </summary>
        /// <remarks>
        /// This property backs KHR_Materials_Dispersion extension.
        /// </remarks>
        public float Dispersion
        {
            get => this.GetExtension<MaterialDispersion>()?.Dispersion ?? MaterialDispersion.DefaultDispersion;
            set
            {
                if (this.GetExtension<MaterialUnlit>() != null) return;
                if (this.GetExtension<MaterialPBRSpecularGlossiness>() != null) return;

                // setting the Dispersion to its default value
                // essentially makes the extension unneccesary.
                if (value == MaterialDispersion.DefaultDispersion)
                {
                    this.RemoveExtensions<MaterialDispersion>();
                    return;
                }

                this.UseExtension<MaterialDispersion>().Dispersion = value;
            }
        }

        #endregion

        #region API

        /// <summary>
        /// Finds an instance of <see cref="MaterialChannel"/>
        /// </summary>
        /// <param name="channelKey">
        /// The channel key. Currently, these values are used:
        /// - "Normal"
        /// - "Occlusion"
        /// - "Emissive"
        /// - When material is <see cref="MaterialPBRMetallicRoughness"/>:
        ///   - "BaseColor"
        ///   - "MetallicRoughness"
        /// - When material is <see cref="MaterialPBRSpecularGlossiness"/>:
        ///   - "Diffuse"
        ///   - "SpecularGlossiness"
        /// </param>
        /// <returns>A <see cref="MaterialChannel"/> structure. or null if it does not exist</returns>
        [System.Diagnostics.DebuggerStepThrough]
        public MaterialChannel? FindChannel(string channelKey)
        {
            foreach (var ch in Channels)
            {
                if (ch.Key.Equals(channelKey, StringComparison.OrdinalIgnoreCase)) return ch;
            }

            return null;
        }        

        #endregion

        #region validation

        protected override void OnValidateContent(Validation.ValidationContext result)
        {
            base.OnValidateContent(result);

            if (this.GetExtension<MaterialPBRSpecularGlossiness>() != null || this.GetExtension<MaterialUnlit>() != null)
            {
                result.MustBeNull("ClearCoat", this.GetExtension<MaterialClearCoat>());
                result.MustBeNull("Transmission", this.GetExtension<MaterialTransmission>());
                result.MustBeNull("Sheen", this.GetExtension<MaterialSheen>());
            }
        }

        #endregion
    }

    public partial class ModelRoot
    {
        /// <summary>
        /// Creates a new <see cref="Material"/> instance and appends it to <see cref="ModelRoot.LogicalMaterials"/>.
        /// </summary>
        /// <param name="name">The name of the instance.</param>
        /// <returns>A <see cref="Material"/> instance.</returns>
        public Material CreateMaterial(string name = null)
        {
            var mat = new Material();
            mat.Name = name;

            _materials.Add(mat);

            return mat;
        }
    }
}
