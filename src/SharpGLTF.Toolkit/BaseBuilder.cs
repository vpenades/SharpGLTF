using System;
using System.Collections.Generic;
using System.Text;

namespace SharpGLTF
{
    public abstract class BaseBuilder
    {
        #region lifecycle

        public BaseBuilder() { }

        public BaseBuilder(string name)
        {
            this.Name = name;
        }

        public BaseBuilder(string name, IO.JsonContent extras)
        {
            this.Name = name;
            this.Extras = extras;
        }

        public BaseBuilder(BaseBuilder other)
        {
            this.Name = other.Name;
            this.Extras = other.Extras.DeepClone();
        }

        #endregion

        #region data

        /// <summary>
        /// Display text name, or null.<br/>⚠️ DO NOT USE AS AN OBJECT ID ⚠️
        /// </summary>
        /// <remarks>
        /// glTF does not define any name ruling for object names.
        /// This means that names can be null or non unique.
        /// So don't use names for anything other than object name display.
        /// Use lookup tables instead.
        /// </remarks>
        public string Name { get; set; }

        public IO.JsonContent Extras { get; set; }

        protected static int GetContentHashCode(BaseBuilder x)
        {
            return x?.Name?.GetHashCode() ?? 0;
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
