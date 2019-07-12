using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace SharpGLTF.Scenes
{
    /// <summary>
    /// Defines a node object within an armature.
    /// </summary>
    public class NodeBuilder
    {
        #region lifecycle

        public NodeBuilder() { }

        private NodeBuilder(NodeBuilder parent)
        {
            _Parent = parent;
        }

        #endregion

        #region data

        private readonly NodeBuilder _Parent;

        private readonly List<NodeBuilder> _Children = new List<NodeBuilder>();

        private Matrix4x4? _Matrix;

        #endregion

        #region properties - hierarchy

        public String Name { get; set; }

        public NodeBuilder Parent => _Parent;

        public NodeBuilder Root => _Parent == null ? this : _Parent.Root;

        public IReadOnlyList<NodeBuilder> Children => _Children;

        #endregion

        #region properties - transform

        public bool HasAnimations => Scale?.Tracks.Count > 0 || Rotation?.Tracks.Count > 0 || Translation?.Tracks.Count > 0;

        public Animations.AnimatableProperty<Vector3> Scale { get; private set; }

        public Animations.AnimatableProperty<Quaternion> Rotation { get; private set; }

        public Animations.AnimatableProperty<Vector3> Translation { get; private set; }

        /// <summary>
        /// Gets or sets the local transform <see cref="Matrix4x4"/> of this <see cref="NodeBuilder"/>.
        /// </summary>
        public Matrix4x4 LocalMatrix
        {
            get => Transforms.AffineTransform.Evaluate(_Matrix, Scale?.Value, Rotation?.Value, Translation?.Value);
            set
            {
                if (value == Matrix4x4.Identity)
                {
                    _Matrix = null;
                }
                else
                {
                    _Matrix = value;
                }

                Scale = null;
                Rotation = null;
                Translation = null;
            }
        }

        /// <summary>
        /// Gets or sets the local Scale, Rotation and Translation of this <see cref="NodeBuilder"/>.
        /// </summary>
        public Transforms.AffineTransform LocalTransform
        {
            get => _Matrix.HasValue
                ?
                Transforms.AffineTransform.Create(_Matrix.Value)
                :
                Transforms.AffineTransform.Create(Translation?.Value, Rotation?.Value, Scale?.Value);
            set
            {
                Guard.IsTrue(value.IsValid, nameof(value));

                _Matrix = null;

                if (value.Scale != Vector3.One)
                {
                    if (Scale == null) Scale = new Animations.AnimatableProperty<Vector3>();
                    Scale.Value = value.Scale;
                }

                if (value.Rotation != Quaternion.Identity)
                {
                    if (Rotation == null) Rotation = new Animations.AnimatableProperty<Quaternion>();
                    Rotation.Value = value.Rotation;
                }

                if (value.Translation != Vector3.Zero)
                {
                    if (Translation == null) Translation = new Animations.AnimatableProperty<Vector3>();
                    Translation.Value = value.Scale;
                }
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

        #endregion

        #region API

        public NodeBuilder AddNode(string name = null)
        {
            var c = new NodeBuilder(this);
            _Children.Add(c);
            c.Name = name;
            return c;
        }

        public Animations.AnimatableProperty<Vector3> UseScale()
        {
            if (Scale == null)
            {
                Scale = new Animations.AnimatableProperty<Vector3>();
                Scale.Value = Vector3.One;
            }

            return Scale;
        }

        public Animations.CurveBuilder<Vector3> UseScale(string animationTrack)
        {
            return UseScale().UseTrackBuilder(animationTrack);
        }

        public Animations.AnimatableProperty<Quaternion> UseRotation()
        {
            if (Rotation == null)
            {
                Rotation = new Animations.AnimatableProperty<Quaternion>();
                Rotation.Value = Quaternion.Identity;
            }

            return Rotation;
        }

        public Animations.CurveBuilder<Quaternion> UseRotation(string animationTrack)
        {
            return UseRotation().UseTrackBuilder(animationTrack);
        }

        public Animations.AnimatableProperty<Vector3> UseTranslation()
        {
            if (Translation == null)
            {
                Translation = new Animations.AnimatableProperty<Vector3>();
                Translation.Value = Vector3.One;
            }

            return Translation;
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

            return Transforms.AffineTransform.Create(translation, rotation, scale);
        }

        public Matrix4x4 GetWorldMatrix(string animationTrack, float time)
        {
            if (animationTrack == null) return this.WorldMatrix;

            var vs = Parent;
            var lm = GetLocalTransform(animationTrack, time).Matrix;
            return vs == null ? lm : Transforms.AffineTransform.LocalToWorld(vs.GetWorldMatrix(animationTrack, time), lm);
        }

        #endregion
    }
}
