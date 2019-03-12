using System;
using System.Collections.Generic;
using System.Linq;

namespace SharpGLTF.Schema2
{
    using Collections;

    /// <summary>
    /// All gltf elements stored in ModelRoot must inherit from this class.
    /// </summary>
    public abstract partial class LogicalChildOfRoot : IChildOf<ModelRoot>
    {
        #region properties

        public String Name
        {
            get => _name;
            internal set => _name = value;
        }

        #endregion

        #region IChildOf<ROOT>

        /// <summary>
        /// Gets the <see cref="ModelRoot"/> instance that owns this object.
        /// </summary>
        public ModelRoot LogicalParent { get; private set; }

        void IChildOf<ModelRoot>._SetLogicalParent(ModelRoot parent) { LogicalParent = parent; }

        #endregion

        #region validation

        protected bool SharesLogicalParent(params LogicalChildOfRoot[] items)
        {
            return items.All(item => Object.ReferenceEquals(this.LogicalParent, item.LogicalParent));
        }

        public override IEnumerable<Exception> Validate()
        {
            foreach (var ex in base.Validate()) yield return ex;

            if (_name == null) yield break;

            // todo, verify the name does not have invalid characters
        }

        #endregion

        #region API

        internal void UsingExtension(Type extensionType)
        {
            LogicalParent.UsingExtension(this.GetType(), extensionType);
        }

        #endregion
    }
}