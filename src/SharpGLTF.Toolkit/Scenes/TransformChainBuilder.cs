using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

using TRANSFORM = SharpGLTF.Transforms.AffineTransform;

namespace SharpGLTF.Scenes
{
    public readonly struct TransformChainBuilder
    {
        #region constructors

        public static implicit operator TransformChainBuilder(NodeBuilder node)
        {
            return new TransformChainBuilder(node);
        }

        public static implicit operator TransformChainBuilder(TRANSFORM transform)
        {
            return new TransformChainBuilder(transform);
        }

        public static implicit operator TransformChainBuilder(Matrix4x4 transform)
        {
            return new TransformChainBuilder(transform);
        }

        public TransformChainBuilder(TRANSFORM transform)
        {
            _ParentTransform = null;
            _ChildTransform = transform;
        }

        public TransformChainBuilder(NodeBuilder node)
        {
            _ParentTransform = node;
            _ChildTransform = null;
        }

        public TransformChainBuilder(NodeBuilder parent, TRANSFORM child)
        {
            _ParentTransform = parent;
            _ChildTransform = child;
        }

        #endregion

        #region data

        private readonly NodeBuilder _ParentTransform;
        private readonly TRANSFORM? _ChildTransform;

        #endregion

        #region properties

        public NodeBuilder Parent => _ParentTransform;
        public TRANSFORM? Child => _ChildTransform;

        #endregion
    }
}
