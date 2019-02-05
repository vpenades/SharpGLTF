using System;
using System.Collections.Generic;
using System.Linq;

namespace glTF2Sharp.Schema2
{
    using Collections;

    using ROOT = ModelRoot;

    public abstract partial class LogicalChildOfRoot : IChildOf<ROOT>
    {
        public ROOT LogicalParent { get; private set; }

        void IChildOf<ROOT>._SetLogicalParent(ROOT parent) { LogicalParent = parent; }

        public String Name
        {
            get => _name;
            internal set => _name = value;
        }

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
    }
}