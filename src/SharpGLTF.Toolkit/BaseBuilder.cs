using System;
using System.Collections.Generic;
using System.Text;

namespace SharpGLTF
{
    public abstract class BaseBuilder
    {
        #region lifecycle

        protected BaseBuilder() { }

        protected BaseBuilder(string name)
        {
            this.Name = name;
        }

        protected BaseBuilder(string name, IO.JsonContent extras)
        {
            this.Name = name;
            this.Extras = extras;
        }

        protected BaseBuilder(BaseBuilder other)
        {
            Guard.NotNull(other, nameof(other));

            this.Name = other.Name;
            this.Extras = other.Extras.DeepClone();
        }

        #endregion

        #region data

        /// <summary>
        /// Gets or sets the display text name, or null.
        /// <para><b>⚠️ DO NOT USE AS AN OBJECT ID ⚠️</b> see remarks.</para>
        /// </summary>
        /// <remarks>
        /// glTF does not define any rule for object names.<br/>
        /// This means that names can be null or non unique.<br/>
        /// So don't use <see cref="Name"/> for anything other than object name display.<br/>
        /// If you need to reference objects by some ID, use lookup tables instead.
        /// </remarks>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the custom data of this object.
        /// </summary>
        public IO.JsonContent Extras { get; set; }

        protected static int GetContentHashCode(BaseBuilder x)
        {
            return x?.Name?.GetHashCode(StringComparison.InvariantCulture) ?? 0;
        }

        protected static bool AreEqualByContent(BaseBuilder x, BaseBuilder y)
        {
            if ((x, y).AreSameReference(out bool areTheSame)) return areTheSame;

            if (x.Name != y.Name) return false;

            return IO.JsonContent.AreEqualByContent(x.Extras, y.Extras, 0.0001f);
        }

        #endregion

        #region API

        internal void SetNameAndExtrasFrom(BaseBuilder source)
        {
            this.Name = source.Name;
            this.Extras = source.Extras.DeepClone();
        }

        internal void SetNameAndExtrasFrom(Schema2.LogicalChildOfRoot source)
        {
            this.Name = source.Name;
            this.Extras = source.Extras.DeepClone();
        }

        /// <summary>
        /// Copies the Name and Extras values to <paramref name="target"/> only if the values are defined.
        /// </summary>
        /// <param name="target">The target object</param>
        internal void TryCopyNameAndExtrasTo(Schema2.LogicalChildOfRoot target)
        {
            if (this.Name != null) target.Name = this.Name;
            if (this.Extras.Content != null) target.Extras = this.Extras.DeepClone();
        }

        #endregion
    }
}
