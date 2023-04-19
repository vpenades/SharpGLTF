using System;
using System.Collections.Generic;
using System.Linq;

using SharpGLTF.Collections;

namespace SharpGLTF.Schema2
{
    /// <summary>
    /// All gltf elements stored in ModelRoot must inherit from this class.
    /// </summary>
    public abstract partial class LogicalChildOfRoot : IChildOfList<ModelRoot>
    {
        #region properties

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
        public String Name
        {
            get => _name;
            set => _name = value;
        }

        #endregion

        #region IChildOflist<ROOT>

        /// <summary>
        /// Gets the <see cref="ModelRoot"/> instance that owns this object.
        /// </summary>
        public ModelRoot LogicalParent { get; private set; }

        /// <summary>
        /// Gets the zero-based index of this object in the Logical resources of <see cref="ModelRoot"/>.
        /// </summary>
        public int LogicalIndex { get; private set; } = -1;

        void IChildOfList<ModelRoot>.SetLogicalParent(ModelRoot parent, int index)
        {
            LogicalParent = parent;
            LogicalIndex = index;
        }

        #endregion

        #region validation

        protected bool SharesLogicalParent(params LogicalChildOfRoot[] items)
        {
            return items.All(item => Object.ReferenceEquals(this.LogicalParent, item.LogicalParent));
        }

        #endregion

        #region API

        /// <summary>
        /// Renames all the unnamed and duplicate name items in the collection so all the items have a unique valid name.
        /// </summary>
        /// <typeparam name="T">Any <see cref="LogicalChildOfRoot"/> derived type.</typeparam>
        /// <param name="collection">The source collection.</param>
        /// <param name="namePrefix">The name prefix to use.</param>
        public static void RenameLogicalElements<T>(IEnumerable<T> collection, string namePrefix)
            where T : LogicalChildOfRoot
        {
            if (collection == null) return;

            var names = new HashSet<string>();
            var index = -1;

            foreach (var item in collection)
            {
                ++index;

                // if the current name is already valid, keep it.
                if (!string.IsNullOrWhiteSpace(item.Name))
                {
                    if (item.RenameIfAvailable(item.Name, names)) continue;
                }

                // try with a default name
                var newName = $"{namePrefix}{index}";
                if (item.RenameIfAvailable(newName, names)) continue;

                // retry with different names until finding a valid name.
                for (int i = 0; i < int.MaxValue; ++i)
                {
                    newName = $"{namePrefix}{index}-{i}";

                    if (item.RenameIfAvailable(newName, names)) break;
                }
            }
        }

        private bool RenameIfAvailable(string newName, ISet<string> usedNames)
        {
            if (usedNames.Contains(newName)) return false;
            this.Name = newName;
            usedNames.Add(newName);
            return true;
        }

        #endregion
    }
}