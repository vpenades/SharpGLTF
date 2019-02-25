using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;

namespace SharpGLTF.Schema2
{
    public interface IVisualNodeContainer
    {
        IEnumerable<Node> VisualChildren { get; }

        Node AddVisualNode(string name);

        Node FindVisualNode(string name);
    }

    [System.Diagnostics.DebuggerDisplay("Node[{LogicalIndex}] {Name} SkinJoint:{IsSkinJoint} T:{LocalTransform.Translation.X} {LocalTransform.Translation.Y} {LocalTransform.Translation.Z}")]
    public partial class Node : IVisualNodeContainer
    {
        #region lifecycle

        public Node()
        {
            _children = new List<int>();
            _weights = new List<double>();
        }

        #endregion

        #region properties - hierarchy

        public int LogicalIndex => this.LogicalParent.LogicalNodes.IndexOfReference(this);

        public Node VisualParent => this.LogicalParent._GetVisualParentNode(this);

        public IEnumerable<Node> VisualChildren => GetVisualChildren(0);

        public Boolean IsSkinJoint => Skin.GetSkinsUsing(this).Any();

        public Scene VisualScene
        {
            get
            {
                var rootNode = this;
                while (rootNode.VisualParent != null) rootNode = rootNode.VisualParent;
                return LogicalParent.LogicalScenes.FirstOrDefault(item => item._ContainsVisualNode(rootNode, false));
            }
        }

        #endregion

        #region properties - transform

        internal Matrix4x4? RawMatrix { get => _matrix; set => _matrix = value; }

        internal Quaternion? RawRotation { get => _rotation; set => _rotation = value; }

        internal Vector3? RawTranslation { get => _translation; set => _translation = value; }

        internal Vector3? RawScale  { get => _scale; set => _scale = value; }

        public Matrix4x4 LocalMatrix
        {
            get => AffineTransform.Evaluate(_matrix, _scale, _rotation, _translation);
            set
            {
                if (value == System.Numerics.Matrix4x4.Identity) _matrix = null;
                else _matrix = value;

                _scale = null;
                _rotation = null;
                _translation = null;
            }
        }

        public AffineTransform LocalTransform
        {
            get => new AffineTransform(_matrix, _scale, _rotation, _translation);
            set
            {
                _matrix = null;
                _scale = value.Scale;
                _rotation = value.Rotation;
                _translation = value.Translation;
            }
        }

        public Matrix4x4 WorldMatrix
        {
            get
            {
                var vs = VisualParent;
                return vs == null ? LocalMatrix : AffineTransform.LocalToWorld(vs.WorldMatrix, LocalMatrix);
            }
            set
            {
                var vs = VisualParent;
                LocalMatrix = vs == null ? value : AffineTransform.WorldToLocal(vs.WorldMatrix, value);
            }
        }

        #endregion

        #region properties - content

        public Mesh Mesh
        {
            get => this._mesh.HasValue ? this.LogicalParent.LogicalMeshes[this._mesh.Value] : null;
            set
            {
                Guard.MustShareLogicalParent(this.LogicalParent, value, nameof(value));
                var idx = this.LogicalParent.LogicalMeshes.IndexOfReference(value);
                this._mesh = idx < 0 ? (int?)null : idx;
            }
        }

        public Skin Skin
        {
            get => this._skin.HasValue ? this.LogicalParent.LogicalSkins[this._skin.Value] : null;
            set
            {
                Guard.MustShareLogicalParent(this.LogicalParent, value, nameof(value));
                var idx = this.LogicalParent.LogicalSkins.IndexOfReference(value);
                this._skin = idx < 0 ? (int?)null : idx;
            }
        }

        public IReadOnlyList<float> MorphWeights => _weights == null ? Mesh?.MorphWeights : _weights.Select(item => (float)item).ToArray();

        public BoundingBox3? WorldBounds3 => BoundingBox3.Create(this);

        #endregion

        #region API

        internal bool _ContainsVisualNode(Node node, bool recursive)
        {
            Guard.NotNull(node, nameof(node));
            Guard.MustShareLogicalParent(this, node, nameof(node));

            if (!recursive) return VisualChildren.Any(item => item == node);

            return VisualChildren.Any(item => item == node || item._ContainsVisualNode(node, recursive));
        }

        internal bool _HasVisualChild(int nodeIndex) { return _children.Contains(nodeIndex); }

        public IEnumerable<Node> GetVisualChildren(int lod)
        {
            // TODO: handle MSFT_Lod here ?
            // maybe we can have a VisualHierarchyManager abstract class with a default implementation
            // a class declared in the extension... but then, it makes edition horribly complicated.
            // maybe it's better to have a non serializable _LodLevel that is applied when serializing.

            var allChildren = _children.Select(idx => LogicalParent.LogicalNodes[idx]);

            return allChildren;
        }

        public Node AddVisualNode(string name)
        {
            var node = this.LogicalParent._AddLogicalNode(this._children);
            node.Name = name;
            return node;
        }

        public void AddNode(Node node)
        {
            Guard.NotNull(node, nameof(node));
            Guard.MustShareLogicalParent(this, node, nameof(node));

            var idx = this.LogicalParent._UseLogicaNode(node);

            this._children.Add(idx);
        }

        public Node FindVisualNode(string name)
        {
            return this.VisualChildren.FirstOrDefault(item => item.Name == name);
        }

        // TODO: AddVisualChild must return a "NodeBuilder"
        // public Node AddVisualChild() { return LogicalParent._AddLogicalNode(_children); }

        public static IEnumerable<Node> Flatten(IVisualNodeContainer container)
        {
            if (container is Node n) yield return n;

            foreach (var c in container.VisualChildren)
            {
                var cc = Flatten(c);

                foreach (var ccc in cc) yield return ccc;
            }
        }

        public static IEnumerable<Node> GetNodesUsingMesh(Mesh mesh)
        {
            if (mesh == null) return Enumerable.Empty<Node>();

            var meshIdx = mesh.LogicalIndex;

            return mesh.LogicalParent
                .LogicalNodes
                .Where(item => item._mesh.HasValue && item._mesh.Value == meshIdx);
        }

        public static IEnumerable<Node> GetNodesUsingSkin(Skin skin)
        {
            if (skin == null) return Enumerable.Empty<Node>();

            var meshIdx = skin.LogicalIndex;

            return skin.LogicalParent
                .LogicalNodes
                .Where(item => item._skin.HasValue && item._skin.Value == meshIdx);
        }

        public override IEnumerable<Exception> Validate()
        {
            foreach (var ex in base.Validate()) yield return ex;

            // check out of range indices
            foreach (var idx in this._children)
            {
                if (idx < 0 || idx >= this.LogicalParent.LogicalNodes.Count) yield return new ModelException(this, $"references invalid Node[{idx}]");
            }

            // check duplicated indices
            if (this._children.Distinct().Count() != this._children.Count) yield return new ModelException(this, "has duplicated node references");

            // check self references
            if (this._children.Contains(this.LogicalIndex)) yield return new ModelException(this, "has self references");

            // check circular references
            var p = this;
            while (true)
            {
                p = p.VisualParent;
                if (p == null) break;
                if (p.LogicalIndex == this.LogicalIndex)
                {
                    yield return new ModelException(this, "has a circular reference");
                    break;
                }
            }

            // check Transforms (out or range, NaN, etc)

            // check morph weights
        }

        #endregion
    }

    [System.Diagnostics.DebuggerDisplay("Scene[{LogicalIndex}] {Name}")]
    public partial class Scene : IVisualNodeContainer
    {
        #region lifecycle

        public Scene()
        {
            _nodes = new List<int>();
        }

        #endregion

        #region properties

        public int LogicalIndex => this.LogicalParent.LogicalScenes.IndexOfReference(this);

        internal IReadOnlyList<int> _VisualChildrenIndices => _nodes;

        public IEnumerable<Node> VisualChildren => _nodes.Select(idx => LogicalParent.LogicalNodes[idx]);

        public BoundingBox3? WorldBounds3 => BoundingBox3.Create(this);

        #endregion

        #region API

        internal bool _ContainsVisualNode(Node node, bool recursive)
        {
            Guard.NotNull(node, nameof(node));
            Guard.MustShareLogicalParent(this, node, nameof(node));

            if (_nodes.Contains(node.LogicalIndex)) return true;

            if (!recursive) return false;

            return VisualChildren.Any(item => item._ContainsVisualNode(node, true));
        }

        public Node AddVisualNode(String name)
        {
            return this.LogicalParent._AddLogicalNode(this._nodes);
        }

        public void AddVisualNode(Node node)
        {
            var idx = this.LogicalParent._UseLogicaNode(node);

            this._nodes.Add(idx);
        }

        public Node FindVisualNode(String name)
        {
            return this.VisualChildren.FirstOrDefault(item => item.Name == name);
        }

        public override IEnumerable<Exception> Validate()
        {
            foreach (var ex in base.Validate()) yield return ex;

            // check out of range indices
            foreach (var idx in this._nodes)
            {
                if (idx < 0 || idx >= this.LogicalParent.LogicalNodes.Count) yield return new ModelException(this, $"references invalid Node[{idx}]");
            }

            // check duplicated indices
            if (this._nodes.Distinct().Count() != this._nodes.Count) yield return new ModelException(this, "has duplicated node references");
        }

        // TODO: AddVisualChild must return a "NodeBuilder"
        // public Node AddVisualChild() { return LogicalParent._AddLogicalNode(_nodes); }

        #endregion
    }

    public partial class ModelRoot
    {
        public Scene UseScene(int index)
        {
            Guard.MustBeGreaterThanOrEqualTo(index, 0, nameof(index));

            while (index >= _scenes.Count)
            {
                _scenes.Add(new Scene());
            }

            return _scenes[index];
        }

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

        internal Node _GetVisualParentNode(Node childNode)
        {
            var childIdx = _nodes.IndexOf(childNode);
            if (childIdx < 0) return null;

            // find the logical owner
            return _nodes.FirstOrDefault(item => item._HasVisualChild(childIdx));
        }

        internal Node _AddLogicalNode()
        {
            var n = new Node();
            _nodes.Add(n);
            return n;
        }

        internal Node _AddLogicalNode(IList<int> children)
        {
            var n = _AddLogicalNode();
            children.Add(n.LogicalIndex);
            return n;
        }

        internal int _UseLogicaNode(Node node) { return _nodes.Use(node); }

        internal Boolean _CheckNodeIsJoint(Node n)
        {
            var idx = n.LogicalIndex;
            return _skins.Any(s => s._ContainsNode(idx));
        }
    }
}
