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

        public int GetNodeIndex()
        {
            if (this._node != null) return this._node.Value;

            if (_NodePath == PropertyPath.pointer)
            {
                var aptr = this.GetExtension<AnimationPointer>();
                if (aptr != null && AnimationPointer.TryParseNodeTransform(aptr.Pointer, out var nidx, out _)) return nidx;
            }

            return -1;
        }

        public PropertyPath GetNodePath()
        {
            if (_NodePath == PropertyPath.pointer)
            {
                var aptr = this.GetExtension<AnimationPointer>();
                if (aptr != null && AnimationPointer.TryParseNodeTransform(aptr.Pointer, out _, out var nprop)) return nprop;
            }

            return _NodePath;
        }

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
        /// <param name="pointer">The path to try parse.</param>
        /// <param name="nodeIndex">the logical index of the node.</param>
        /// <param name="property">the transformation property.</param>
        /// <returns>true if the parsing succeeded.</returns>
        public static bool TryParseNodeTransform(string pointer, out int nodeIndex, out PropertyPath property)
        {
            nodeIndex = -1;
            property = PropertyPath.pointer;

            if (pointer == null || !pointer.StartsWith("/nodes/")) return false;


            pointer = pointer.Substring(7);
            var next = pointer.IndexOf('/');

            if (!int.TryParse(pointer.Substring(0, next), System.Globalization.NumberStyles.Integer, System.Globalization.CultureInfo.InvariantCulture, out var idx)) return false;

            var tail = pointer.Substring(next + 1);
            switch (tail)
            {
                case "scale": nodeIndex = idx; property = PropertyPath.scale; return true;
                case "rotation": nodeIndex = idx; property = PropertyPath.rotation; return true;
                case "translation": nodeIndex = idx; property = PropertyPath.translation; return true;
                case "weights": nodeIndex = idx; property = PropertyPath.weights; return true;
            }

            return false;
        }

        #endregion
    }
}
