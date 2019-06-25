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

        internal NodeBuilder(NodeBuilder parent)
        {
            _Parent = parent;
        }

        #endregion

        #region data

        private readonly NodeBuilder _Parent;

        private readonly List<NodeBuilder> _Children = new List<NodeBuilder>();

        private Matrix4x4? _Matrix;
        private Animations.Animatable<Vector3> _Scale;
        private Animations.Animatable<Quaternion> _Rotation;
        private Animations.Animatable<Vector3> _Translation;

        #endregion

        #region properties - hierarchy

        public String Name { get; set; }

        public NodeBuilder Parent => _Parent;

        public IReadOnlyList<NodeBuilder> Children => _Children;

        #endregion

        #region properties - transform

        /// <summary>
        /// Gets or sets the local transform <see cref="Matrix4x4"/> of this <see cref="NodeBuilder"/>.
        /// </summary>
        public Matrix4x4 LocalMatrix
        {
            get => Transforms.AffineTransform.Evaluate(_Matrix, _Scale?.Default, _Rotation?.Default, _Translation?.Default);
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

                _Scale = null;
                _Rotation = null;
                _Translation = null;
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
                Transforms.AffineTransform.Create(_Scale?.Default, _Rotation?.Default, _Translation?.Default);
            set
            {
                Guard.IsTrue(value.IsValid, nameof(value));

                _Matrix = null;

                if (value.Scale != Vector3.One)
                {
                    if (_Scale == null) _Scale = new Animations.Animatable<Vector3>();
                    _Scale.Default = value.Scale;
                }

                if (value.Rotation != Quaternion.Identity)
                {
                    if (_Rotation == null) _Rotation = new Animations.Animatable<Quaternion>();
                    _Rotation.Default = value.Rotation;
                }

                if (value.Translation != Vector3.Zero)
                {
                    if (_Translation == null) _Translation = new Animations.Animatable<Vector3>();
                    _Translation.Default = value.Scale;
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

        public Transforms.AffineTransform GetLocalTransform(string animationTrack, float time)
        {
            if (animationTrack == null) return this.LocalTransform;

            var scale = _Scale?.GetValueAt(animationTrack, time);
            var rotation = _Rotation?.GetValueAt(animationTrack, time);
            var translation = _Translation?.GetValueAt(animationTrack, time);

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
    }
}
