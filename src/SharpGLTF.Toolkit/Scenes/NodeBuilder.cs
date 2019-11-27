using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;

namespace SharpGLTF.Scenes
{
    /// <summary>
    /// Defines a node object within an armature.
    /// </summary>
    [System.Diagnostics.DebuggerDisplay("{_GetDebuggerDisplay(),nq}")]
    public class NodeBuilder
    {
        #region debug

        private string _GetDebuggerDisplay()
        {
            var txt = $"NodeBuilder";

            if (!string.IsNullOrWhiteSpace(this.Name)) txt += $" {this.Name}";

            if (_Matrix.HasValue)
            {
                if (_Matrix.Value != Matrix4x4.Identity)
                {
                    var xform = this.LocalTransform;
                    if (xform.Scale != Vector3.One) txt += $" 𝐒:{xform.Scale}";
                    if (xform.Rotation != Quaternion.Identity) txt += $" 𝐑:{xform.Rotation}";
                    if (xform.Translation != Vector3.Zero) txt += $" 𝚻:{xform.Translation}";
                }
            }
            else
            {
                if (_Scale != null) txt += $" 𝐒:{_Scale.Value}";
                if (_Rotation != null) txt += $" 𝐑:{_Rotation.Value}";
                if (_Translation != null) txt += $" 𝚻:{_Translation.Value}";
            }

            if (this.VisualChildren.Any())
            {
                txt += $" | Children[{this.VisualChildren.Count}]";
            }

            return txt;
        }

        #endregion

        #region lifecycle

        public NodeBuilder() { }

        public NodeBuilder(string name) { Name = name; }

        private NodeBuilder(NodeBuilder parent)
        {
            _Parent = parent;
        }

        #endregion

        #region data

        private readonly NodeBuilder _Parent;

        private readonly List<NodeBuilder> _Children = new List<NodeBuilder>();

        private Matrix4x4? _Matrix;
        private Animations.AnimatableProperty<Vector3> _Scale;
        private Animations.AnimatableProperty<Quaternion> _Rotation;
        private Animations.AnimatableProperty<Vector3> _Translation;

        #endregion

        #region properties - hierarchy

        public String Name { get; set; }

        public NodeBuilder Parent => _Parent;

        public NodeBuilder Root => _Parent == null ? this : _Parent.Root;

        public IReadOnlyList<NodeBuilder> VisualChildren => _Children;

        #endregion

        #region properties - transform

        /// <summary>
        /// Gets a value indicating whether this <see cref="NodeBuilder"/> has animations.
        /// </summary>
        public bool HasAnimations => (_Scale?.IsAnimated ?? false) || (_Rotation?.IsAnimated ?? false) || (_Translation?.IsAnimated ?? false);

        public Animations.AnimatableProperty<Vector3> Scale => _Scale;

        public Animations.AnimatableProperty<Quaternion> Rotation => _Rotation;

        public Animations.AnimatableProperty<Vector3> Translation => _Translation;

        /// <summary>
        /// Gets or sets the local transform <see cref="Matrix4x4"/> of this <see cref="NodeBuilder"/>.
        /// </summary>
        public Matrix4x4 LocalMatrix
        {
            get => Transforms.AffineTransform.Evaluate(_Matrix, Scale?.Value, Rotation?.Value, Translation?.Value);
            set
            {
                if (HasAnimations) { _DecomposeMatrix(value); return; }

                _Matrix = value != Matrix4x4.Identity ? value : (Matrix4x4?)null;
                _Scale = null;
                _Rotation = null;
                _Translation = null;
            }
        }

        #pragma warning disable CA1721 // Property names should not match get methods

        /// <summary>
        /// Gets or sets the local Scale, Rotation and Translation of this <see cref="NodeBuilder"/>.
        /// </summary>
        public Transforms.AffineTransform LocalTransform
        {
            get => _Matrix.HasValue
                ?
                Transforms.AffineTransform.Create(_Matrix.Value)
                :
                Transforms.AffineTransform.Create(Scale?.Value, Rotation?.Value, Translation?.Value);
            set
            {
                Guard.IsTrue(value.IsValid, nameof(value));

                _Matrix = null;

                if (value.Scale != Vector3.One) UseScale().Value = value.Scale;
                if (value.Rotation != Quaternion.Identity) UseRotation().Value = value.Rotation;
                if (value.Translation != Vector3.Zero) UseTranslation().Value = value.Translation;
            }
        }

        /// <summary>
        /// Gets or sets the world transform <see cref="Matrix4x4"/> of this <see cref="NodeBuilder"/>.
        /// </summary>
        public Matrix4x4 WorldMatrix
        {
            get
            {
                var vs = this.Parent;
                return vs == null ? LocalMatrix : Transforms.AffineTransform.LocalToWorld(vs.WorldMatrix, LocalMatrix);
            }
            set
            {
                var vs = this.Parent;
                LocalMatrix = vs == null ? value : Transforms.AffineTransform.WorldToLocal(vs.WorldMatrix, value);
            }
        }

        #pragma warning restore CA1721 // Property names should not match get methods

        #endregion

        #region API - hierarchy

        public NodeBuilder CreateNode(string name = null)
        {
            var c = new NodeBuilder(this);
            _Children.Add(c);
            c.Name = name;
            return c;
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

        /// <summary>
        /// Rename all the <see cref="NodeBuilder"/> elements in <paramref name="collection"/>
        /// so every node has a valid, unique name.
        /// </summary>
        /// <param name="collection">A collection of <see cref="NodeBuilder"/> elements.</param>
        /// <param name="namePrefix">The name prefix.</param>
        public static void Rename(IEnumerable<NodeBuilder> collection, string namePrefix)
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

        #region API - transform

        private void _DecomposeMatrix()
        {
            if (!_Matrix.HasValue) return;
            if (_Matrix.Value == Matrix4x4.Identity) return;
            _DecomposeMatrix(_Matrix.Value);
            _Matrix = null;
        }

        private void _DecomposeMatrix(Matrix4x4 matrix)
        {
            var affine = Transforms.AffineTransform.Create(matrix);

            UseScale().Value = affine.Scale;
            UseRotation().Value = affine.Rotation;
            UseTranslation().Value = affine.Translation;
        }

        public Animations.AnimatableProperty<Vector3> UseScale()
        {
            _DecomposeMatrix();

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
            _DecomposeMatrix();

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
            _DecomposeMatrix();

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

        public void SetScaleTrack(string track, Animations.ICurveSampler<Vector3> curve) { UseScale().SetTrack(track, curve); }

        public void SetTranslationTrack(string track, Animations.ICurveSampler<Vector3> curve) { UseTranslation().SetTrack(track, curve); }

        public void SetRotationTrack(string track, Animations.ICurveSampler<Quaternion> curve) { UseRotation().SetTrack(track, curve); }

        public Transforms.AffineTransform GetLocalTransform(string animationTrack, float time)
        {
            if (animationTrack == null) return this.LocalTransform;

            var scale = Scale?.GetValueAt(animationTrack, time);
            var rotation = Rotation?.GetValueAt(animationTrack, time);
            var translation = Translation?.GetValueAt(animationTrack, time);

            return Transforms.AffineTransform.Create(scale, rotation, translation);
        }

        public Matrix4x4 GetWorldMatrix(string animationTrack, float time)
        {
            if (animationTrack == null) return this.WorldMatrix;

            var vs = Parent;
            var lm = GetLocalTransform(animationTrack, time).Matrix;
            return vs == null ? lm : Transforms.AffineTransform.LocalToWorld(vs.GetWorldMatrix(animationTrack, time), lm);
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

        #endregion
    }
}
