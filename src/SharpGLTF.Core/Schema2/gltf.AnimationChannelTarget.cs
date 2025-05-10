using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.Numerics;

using SharpGLTF.Collections;
using SharpGLTF.Transforms;
using SharpGLTF.Validation;
using System.Xml.Linq;
using System.IO;
using System.Reflection;
using SharpGLTF.IO;

namespace SharpGLTF.Schema2
{
    /// <summary>
    /// Child of <see cref="AnimationChannel"/>
    /// </summary>
    sealed partial class AnimationChannelTarget : IChildOf<AnimationChannel>
    {
        #region lifecycle

        internal AnimationChannelTarget() { }

        internal AnimationChannelTarget(Node targetNode, PropertyPath targetPath)
        {
            _node = targetNode.LogicalIndex;
            _path = targetPath;
        }

        internal AnimationChannelTarget(string pointer)
        {
            if (AnimationPointer.TryParseNodeTransform(pointer, out var nidx, out var nprop))
            {
                _node = nidx;
                _path = nprop;
                this.RemoveExtensions<AnimationPointer>();
            }
            else
            {
                _node = null;
                _path = PropertyPath.pointer;
                var aptr = this.UseExtension<AnimationPointer>();
                aptr.Pointer = pointer;
            }
        }

        AnimationChannel IChildOf<AnimationChannel>.LogicalParent => _Parent;        

        void IChildOf<AnimationChannel>.SetLogicalParent(AnimationChannel parent)
        {
            _Parent = parent;
        }

        #endregion

        #region data

        private AnimationChannel _Parent;

        [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
        internal PropertyPath _NodePath => this._path;

        #endregion

        #region API

        /// <summary>
        /// Gets the index of the <see cref="Node"/> pointed by this animation,
        /// or -1 if it points to a target that is not a <see cref="Node"/>.
        /// </summary>
        /// <returns>A node index, or -1</returns>
        public int GetNodeIndex()
        {
            if (this._node != null) return this._node.Value;

            if (_NodePath == PropertyPath.pointer)
            {
                var aptr = this.GetExtension<AnimationPointer>();
                if (aptr != null && AnimationPointer.TryParseNodeIndex(aptr.Pointer, out var nidx)) return nidx;
            }

            return -1;
        }

        /// <summary>
        /// If the target is a node, it returns a <see cref="PropertyPath"/> resolved to <see cref="PropertyPath.scale"/>, <see cref="PropertyPath.rotation"/> or <see cref="PropertyPath.translation"/>
        /// otherwise it will return <see cref="PropertyPath.pointer"/>
        /// </summary>
        /// <returns>A <see cref="PropertyPath"/> value.</returns>
        public PropertyPath GetNodePath()
        {
            if (_NodePath == PropertyPath.pointer)
            {
                var aptr = this.GetExtension<AnimationPointer>();
                if (aptr != null && AnimationPointer.TryParseNodeTransform(aptr.Pointer, out _, out var nprop)) return nprop;
            }

            return _NodePath;
        }

        /// <summary>
        /// Returns a pointer path regardless of whether it's defined by default <see cref="PropertyPath"/> or by the animation pointer extension.
        /// </summary>
        /// <returns>An animation pointer path.</returns>
        public string GetPointerPath()
        {
            var aptr = this.GetExtension<AnimationPointer>();
            if (aptr != null) return aptr.Pointer;

            if (this._node == null || this._node.Value < 0) return null;

            return $"/nodes/{this._node.Value}/{_NodePath}";
        }

        #endregion

        #region Validation

        protected override void OnValidateReferences(ValidationContext validate)
        {
            base.OnValidateReferences(validate);

            validate.IsNullOrIndex("Node", _node, validate.Root.LogicalNodes);

            var aptr = this.GetExtension<AnimationPointer>();
            if (aptr != null)
            {

            }
        }

        #endregion
    }

    /// <summary>
    /// Extends <see cref="AnimationChannelTarget"/> with extra targets
    /// </summary>
    /// <remarks>
    /// <see href="https://github.com/KhronosGroup/glTF/tree/main/extensions/2.0/Khronos/KHR_animation_pointer">KHR_animation_pointer specification</see>
    /// </remarks>
    sealed partial class AnimationPointer
    {
        #region lifecycle
        public AnimationPointer(AnimationChannelTarget parent)
        {
            _LogicalParent = parent;
        }

        private AnimationChannelTarget _LogicalParent;

        #endregion

        #region properties

        public string Pointer
        {
            get => this._pointer;
            set => this._pointer = value;
        }

        #endregion

        #region API

        /// <summary>
        /// Parses the pointer path to see if it can be converted to a standard nodeIndex-PropertyPath path.
        /// </summary>
        /// <param name="pointerPath">The path to try parse.</param>
        /// <param name="nodeIndex">the logical index of the node.</param>
        /// <param name="property">the transformation property.</param>
        /// <returns>true if the parsing succeeded.</returns>
        public static bool TryParseNodeTransform(string pointerPath, out int nodeIndex, out PropertyPath property)
        {
            property = PropertyPath.pointer;

            if (!TryParseNodeIndex(pointerPath, out nodeIndex)) return false;

            pointerPath = pointerPath.Substring(7);
            var next = pointerPath.IndexOf('/', StringComparison.Ordinal);
            if (next < 0) return false;

            var tail = pointerPath.Substring(next + 1);
            switch (tail)
            {
                case "scale": property = PropertyPath.scale; return true;
                case "rotation": property = PropertyPath.rotation; return true;
                case "translation": property = PropertyPath.translation; return true;
                case "weights": property = PropertyPath.weights; return true;
            }

            return false;
        }

        public static bool TryParseNodeIndex(string pointerPath, out int nodeIndex)
        {
            nodeIndex = -1;           

            if (pointerPath == null || !pointerPath.StartsWith("/nodes/")) return false;

            pointerPath = pointerPath.Substring(7);
            var next = pointerPath.IndexOf('/', StringComparison.Ordinal);
            if (next < 0) next = pointerPath.Length;

            return int.TryParse(pointerPath.Substring(0, next), System.Globalization.NumberStyles.Integer, System.Globalization.CultureInfo.InvariantCulture, out nodeIndex);
        }

        #endregion
    }
}
