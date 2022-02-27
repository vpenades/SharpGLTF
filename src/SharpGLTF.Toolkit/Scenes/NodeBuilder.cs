using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

using MATRIX = System.Numerics.Matrix4x4;
using TRANSFORM = SharpGLTF.Transforms.AffineTransform;

namespace SharpGLTF.Scenes
{
    /// <summary>
    /// Defines a node object within an armature.
    /// </summary>
    [System.Diagnostics.DebuggerDisplay("{_GetDebuggerDisplay(),nq}")]
    public class NodeBuilder : BaseBuilder
    {
        #region debug

        private string _GetDebuggerDisplay()
        {
            var txt = $"NodeBuilder";

            if (!string.IsNullOrWhiteSpace(this.Name)) txt += $" {this.Name}";

            var xform = this.LocalTransform.GetDecomposed();
            if (xform.Scale != Vector3.One) txt += $" 𝐒:{xform.Scale}";
            if (xform.Rotation != Quaternion.Identity) txt += $" 𝐑:{xform.Rotation}";
            if (xform.Translation != Vector3.Zero) txt += $" 𝚻:{xform.Translation}";

            if (this.VisualChildren.Any())
            {
                txt += $" | Children[{this.VisualChildren.Count}]";
            }

            return txt;
        }

        #endregion

        #region lifecycle

        public NodeBuilder() { }

        public NodeBuilder(string name)
            : base(name) { }

        public NodeBuilder(string name, IO.JsonContent extras)
            : base(name, extras) { }

        public Dictionary<NodeBuilder, NodeBuilder> DeepClone()
        {
            var dict = new Dictionary<NodeBuilder, NodeBuilder>();

            DeepClone(dict);

            return dict;
        }

        private NodeBuilder DeepClone(IDictionary<NodeBuilder, NodeBuilder> nodeMap)
        {
            var clone = new NodeBuilder();

            clone.SetNameAndExtrasFrom(this);

            nodeMap[this] = clone;

            clone._Matrix = this._Matrix;
            clone._Scale = this._Scale?.Clone();
            clone._Rotation = this._Rotation?.Clone();
            clone._Translation = this._Translation?.Clone();

            foreach (var c in _Children)
            {
                clone.AddNode(c.DeepClone(nodeMap));
            }

            return clone;
        }

        #endregion

        #region data

        private NodeBuilder _Parent;

        private readonly List<NodeBuilder> _Children = new List<NodeBuilder>();

        private MATRIX? _Matrix;
        private Animations.AnimatableProperty<Vector3> _Scale;
        private Animations.AnimatableProperty<Quaternion> _Rotation;
        private Animations.AnimatableProperty<Vector3> _Translation;

        #endregion

        #region properties - hierarchy
        public NodeBuilder Parent => _Parent;

        public NodeBuilder Root => _Parent == null ? this : _Parent.Root;

        public IReadOnlyList<NodeBuilder> VisualChildren => _Children;

        #endregion

        #region properties - transform

        public IEnumerable<string> AnimationTracksNames
        {
            get
            {
                var tracks = Enumerable.Empty<string>();
                if (_Scale != null) tracks = tracks.Concat(_Scale.Tracks.Keys);
                if (_Rotation != null) tracks = tracks.Concat(_Rotation.Tracks.Keys);
                if (_Translation != null) tracks = tracks.Concat(_Translation.Tracks.Keys);
                return tracks.Distinct();
            }
        }

        /// <summary>
        /// Gets a value indicating whether this <see cref="NodeBuilder"/> has animations.
        /// </summary>
        public bool HasAnimations => (_Scale?.IsAnimated ?? false) || (_Rotation?.IsAnimated ?? false) || (_Translation?.IsAnimated ?? false);

        /// <summary>
        /// Gets the current Scale transform, or null.
        /// </summary>
        public Animations.AnimatableProperty<Vector3> Scale => _Scale;

        /// <summary>
        /// Gets the current rotation transform, or null.
        /// </summary>
        public Animations.AnimatableProperty<Quaternion> Rotation => _Rotation;

        /// <summary>
        /// Gets the current translation transform, or null.
        /// </summary>
        public Animations.AnimatableProperty<Vector3> Translation => _Translation;

        /// <summary>
        /// Gets or sets the local transform <see cref="MATRIX"/> of this <see cref="NodeBuilder"/>.
        /// </summary>
        /// <remarks>
        /// When setting the value, If there's no animations currently attached to this node,<br/>
        /// the transform is stored as a matrix. Otherwise, it's decomposed to a SRT chain.
        /// </remarks>
        public MATRIX LocalMatrix
        {
            get => LocalTransform.Matrix;
            set => LocalTransform = value;
        }

        /// <summary>
        /// Gets or sets the local Scale, Rotation and Translation of this <see cref="NodeBuilder"/>.
        /// </summary>
        public TRANSFORM LocalTransform
        {
            get => TRANSFORM.CreateFromAny(_Matrix, _Scale?.Value, _Rotation?.Value, _Translation?.Value);
            set
            {
                Guard.IsTrue(value.IsValid, nameof(value));

                // we cannot set a matrix while holding animation tracks because it would destroy them.
                if (HasAnimations) value = value.GetDecomposed();

                if (value.IsSRT)
                {
                    _Matrix = null;
                    if (_Scale != null || value.Scale != Vector3.One) UseScale().Value = value.Scale;
                    if (_Rotation != null || value.Rotation != Quaternion.Identity) UseRotation().Value = value.Rotation;
                    if (_Translation != null || value.Translation != Vector3.Zero) UseTranslation().Value = value.Translation;
                }
                else
                {
                    _Matrix = value.Matrix;
                    _Scale = null;
                    _Rotation = null;
                    _Translation = null;
                }
            }
        }

        /// <summary>
        /// Gets or sets the world transform <see cref="MATRIX"/> of this <see cref="NodeBuilder"/>.
        /// </summary>
        public MATRIX WorldMatrix
        {
            get
            {
                var p = this.Parent;
                return p == null ? LocalMatrix : Transforms.Matrix4x4Factory.LocalToWorld(p.WorldMatrix, LocalMatrix);
            }
            set
            {
                var p = this.Parent;
                LocalMatrix = p == null ? value : Transforms.Matrix4x4Factory.WorldToLocal(p.WorldMatrix, value);
            }
        }

        /// <summary>
        /// Equivalent to <see cref="LocalMatrix"/> but calculated at double precission.
        /// </summary>
        internal Transforms.Matrix4x4Double LocalMatrixPrecise
        {
            get
            {
                if (_Matrix.HasValue) return new Transforms.Matrix4x4Double(_Matrix.Value);

                var s = _Scale?.Value ?? Vector3.One;
                var r = _Rotation?.Value ?? Quaternion.Identity;
                var t = _Translation?.Value ?? Vector3.Zero;

                return
                    Transforms.Matrix4x4Double.CreateScale(s.X, s.Y, s.Z)
                    *
                    Transforms.Matrix4x4Double.CreateFromQuaternion(r.Sanitized())
                    *
                    Transforms.Matrix4x4Double.CreateTranslation(t.X, t.Y, t.Z);
            }
        }

        /// <summary>
        /// Equivalent to <see cref="WorldMatrix"/> but calculated at double precission.
        /// </summary>
        internal Transforms.Matrix4x4Double WorldMatrixPrecise
        {
            get
            {
                var vs = this.Parent;
                return vs == null ? LocalMatrixPrecise : LocalMatrixPrecise * vs.WorldMatrixPrecise;
            }
        }

        #endregion

        #region API - hierarchy

        public NodeBuilder CreateNode(string name = null)
        {
            var c = new NodeBuilder();
            c.Name = name;
            AddNode(c);
            return c;
        }

        public void AddNode(NodeBuilder node)
        {
            Guard.NotNull(node, nameof(node));
            Guard.IsFalse(Object.ReferenceEquals(this, node), "cannot add to itself");

            if (node._Parent == this) return; // already added to this node.

            Guard.MustBeNull(node._Parent, nameof(node), "is child of another node.");

            node._Parent = this;
            _Children.Add(node);
        }

        /// <summary>
        /// Checks if the collection of joints can be used for skinning a mesh.
        /// </summary>
        /// <param name="joints">A collection of joints.</param>
        /// <returns>True if the joints can be used for skinning.</returns>
        public static bool IsValidArmature(IEnumerable<NodeBuilder> joints)
        {
            if (joints == null) return false;
            if (!joints.Any()) return false;
            if (joints.Any(item => item == null)) return false;

            var root = joints.First().Root;

            // check if all joints share the same root
            if (!joints.All(item => Object.ReferenceEquals(item.Root, root))) return false;

            var nameGroups = Flatten(root)
                .Where(item => item.Name != null)
                .GroupBy(item => item.Name);

            if (nameGroups.Any(group => group.Count() > 1)) return false;

            return true;
        }

        public static IEnumerable<NodeBuilder> Flatten(NodeBuilder container)
        {
            if (container == null) yield break;

            yield return container;

            foreach (var c in container.VisualChildren)
            {
                var cc = Flatten(c);

                foreach (var ccc in cc) yield return ccc;
            }
        }

        #endregion

        #region API - transform

        private void _UseDecomposedTransform()
        {
            var xform = this.LocalTransform;
            if (xform.IsSRT) return;

            // try to convert from matrix representation to decomposed representation.

            xform = xform.GetDecomposed();

            _Matrix = null;
            UseScale().Value = xform.Scale;
            UseRotation().Value = xform.Rotation;
            UseTranslation().Value = xform.Translation;
        }

        public Animations.AnimatableProperty<Vector3> UseScale()
        {
            _UseDecomposedTransform();

            if (_Scale == null)
            {
                _Scale = new Animations.AnimatableProperty<Vector3>();
                _Scale.Value = Vector3.One;
            }

            return _Scale;
        }

        public Animations.CurveBuilder<Vector3> UseScale(string animationTrack)
        {
            return UseScale().UseTrackBuilder(animationTrack);
        }

        public Animations.AnimatableProperty<Quaternion> UseRotation()
        {
            _UseDecomposedTransform();

            if (_Rotation == null)
            {
                _Rotation = new Animations.AnimatableProperty<Quaternion>();
                _Rotation.Value = Quaternion.Identity;
            }

            return _Rotation;
        }

        public Animations.CurveBuilder<Quaternion> UseRotation(string animationTrack)
        {
            return UseRotation().UseTrackBuilder(animationTrack);
        }

        public Animations.AnimatableProperty<Vector3> UseTranslation()
        {
            _UseDecomposedTransform();

            if (_Translation == null)
            {
                _Translation = new Animations.AnimatableProperty<Vector3>();
                _Translation.Value = Vector3.Zero;
            }

            return _Translation;
        }

        public Animations.CurveBuilder<Vector3> UseTranslation(string animationTrack)
        {
            return UseTranslation().UseTrackBuilder(animationTrack);
        }

        public void SetScaleTrack(string track, Animations.ICurveSampler<Vector3> curve)
        {
            UseScale().SetTrack(track, curve);
        }

        public void SetTranslationTrack(string track, Animations.ICurveSampler<Vector3> curve)
        {
            UseTranslation().SetTrack(track, curve);
        }

        public void SetRotationTrack(string track, Animations.ICurveSampler<Quaternion> curve)
        {
            UseRotation().SetTrack(track, curve);
        }

        public TRANSFORM GetLocalTransform(string animationTrack, float time)
        {
            if (animationTrack == null) return this.LocalTransform;

            var scale = Scale?.GetValueAt(animationTrack, time);
            var rotation = Rotation?.GetValueAt(animationTrack, time);
            var translation = Translation?.GetValueAt(animationTrack, time);

            return new TRANSFORM(scale, rotation, translation);
        }

        public MATRIX GetWorldMatrix(string animationTrack, float time)
        {
            if (animationTrack == null) return this.WorldMatrix;

            var vs = Parent;
            var lm = GetLocalTransform(animationTrack, time).Matrix;
            return vs == null ? lm : Transforms.Matrix4x4Factory.LocalToWorld(vs.GetWorldMatrix(animationTrack, time), lm);
        }

        public MATRIX GetInverseBindMatrix(MATRIX? meshWorldMatrix = null)
        {
            Transforms.Matrix4x4Double mwx = meshWorldMatrix ?? MATRIX.Identity;

            return (MATRIX)Transforms.SkinnedTransform.CalculateInverseBinding(mwx, this.WorldMatrixPrecise);
        }

        #endregion

        #region With* API

        public NodeBuilder WithLocalTranslation(Vector3 translation)
        {
            this.UseTranslation().Value = translation;
            return this;
        }

        public NodeBuilder WithLocalScale(Vector3 scale)
        {
            this.UseScale().Value = scale;
            return this;
        }

        public NodeBuilder WithLocalRotation(Quaternion rotation)
        {
            this.UseRotation().Value = rotation;
            return this;
        }

        public NodeBuilder WithLocalTranslation(string animTrack, IReadOnlyDictionary<float, Vector3> keyframes)
        {
            Guard.NotNull(keyframes, nameof(keyframes));

            var items = keyframes
                .OrderBy(item => item.Key)
                .Select(item => (item.Key, item.Value));
                // no need to collapse, since SetTrack already clones the curve.

            this.UseTranslation().SetTrack(animTrack, Animations.CurveSampler.CreateSampler(items));

            return this;
        }

        public NodeBuilder WithLocalRotation(string animTrack, IReadOnlyDictionary<float, Quaternion> keyframes)
        {
            Guard.NotNull(keyframes, nameof(keyframes));

            var items = keyframes
                .OrderBy(item => item.Key)
                .Select(item => (item.Key, item.Value));
            // no need to collapse, since SetTrack already clones the curve.

            this.UseRotation().SetTrack(animTrack, Animations.CurveSampler.CreateSampler(items));

            return this;
        }

        public NodeBuilder WithLocalScale(string animTrack, IReadOnlyDictionary<float, Vector3> keyframes)
        {
            Guard.NotNull(keyframes, nameof(keyframes));

            var items = keyframes
                .OrderBy(item => item.Key)
                .Select(item => (item.Key, item.Value));
            // no need to collapse, since SetTrack already clones the curve.

            this.UseScale().SetTrack(animTrack, Animations.CurveSampler.CreateSampler(items));

            return this;
        }

        #endregion
    }
}
