using System;
using System.Collections.Generic;
using System.Linq;

namespace SharpGLTF.Schema2
{
    [System.Diagnostics.DebuggerDisplay("Scene[{LogicalIndex}] {Name}")]
    public sealed partial class Scene : IVisualNodeContainer
    {
        #region lifecycle

        internal Scene()
        {
            _nodes = new List<int>();
        }

        #endregion

        #region properties

        /// <summary>
        /// Gets the zero-based index of this <see cref="Scene"/> at <see cref="ModelRoot.LogicalScenes"/>
        /// </summary>
        public int LogicalIndex => this.LogicalParent.LogicalScenes.IndexOfReference(this);

        internal IReadOnlyList<int> _VisualChildrenIndices => _nodes;

        public IEnumerable<Node> VisualChildren => _nodes.Select(idx => LogicalParent.LogicalNodes[idx]);

        #endregion

        #region API

        /// <summary>
        /// Creates a new <see cref="Node"/> instance,
        /// adds it to <see cref="ModelRoot.LogicalNodes"/>
        /// and references it as a child in the current graph.
        /// </summary>
        /// <param name="name">The name of the instance.</param>
        /// <returns>A <see cref="Node"/> instance.</returns>
        public Node CreateNode(String name = null)
        {
            var n = this.LogicalParent._CreateLogicalNode(this._nodes);
            n.Name = name;
            return n;
        }

        internal bool _ContainsVisualNode(Node node, bool recursive)
        {
            Guard.NotNull(node, nameof(node));
            Guard.MustShareLogicalParent(this, node, nameof(node));

            if (_nodes.Contains(node.LogicalIndex)) return true;

            if (!recursive) return false;

            return VisualChildren.Any(item => item._ContainsVisualNode(node, true));
        }

        #endregion

        #region validation

        internal override void Validate(Validation.ValidationContext result)
        {
            base.Validate(result);

            // check out of range indices
            foreach (var idx in this._nodes)
            {
                if (idx < 0 || idx >= this.LogicalParent.LogicalNodes.Count) result.AddError(this, $"references invalid Node[{idx}]");
            }

            // check duplicated indices
            if (this._nodes.Distinct().Count() != this._nodes.Count) result.AddError(this, "has duplicated node references");
        }

        #endregion
    }

    public partial class ModelRoot
    {
        /// <summary>
        /// Creates or reuses a <see cref="Scene"/> instance
        /// at <see cref="ModelRoot.LogicalScenes"/>.
        /// </summary>
        /// <param name="index">The zero-based index of the <see cref="Scene"/> in <see cref="ModelRoot.LogicalScenes"/>.</param>
        /// <returns>A <see cref="Scene"/> instance.</returns>
        public Scene UseScene(int index)
        {
            Guard.MustBeGreaterThanOrEqualTo(index, 0, nameof(index));

            while (index >= _scenes.Count)
            {
                _scenes.Add(new Scene());
            }

            return _scenes[index];
        }

        /// <summary>
        /// Creates or reuses a <see cref="Scene"/> instance that has the
        /// same <paramref name="name"/> at <see cref="ModelRoot.LogicalScenes"/>.
        /// </summary>
        /// <param name="name">The name of the instance.</param>
        /// <returns>A <see cref="Scene"/> instance.</returns>
        public Scene UseScene(string name)
        {
            var scene = _scenes.FirstOrDefault(item => item.Name == name);
            if (scene != null) return scene;

            scene = new Scene
            {
                Name = name
            };

            _scenes.Add(scene);

            return scene;
        }
    }
}
