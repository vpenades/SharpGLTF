using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace SharpGLTF.Runtime
{
    /// <summary>
    /// Defines a hierarchical transform node of a scene graph tree.
    /// </summary>
    [System.Diagnostics.DebuggerDisplay("[{LogicalNodeIndex}] {Name}")]
    class NodeTemplate
    {
        #region lifecycle

        internal NodeTemplate(Schema2.Node srcNode, int parentIdx, int[] childIndices, bool isolateMemory, IDictionary<string, float> animTracks)
        {
            _LogicalSourceIndex = srcNode.LogicalIndex;

            _ParentIndex = parentIdx;
            _ChildIndices = childIndices;

            Name = srcNode.Name;

            _LocalMatrix = srcNode.LocalMatrix;

            var localXform = srcNode.LocalTransform;
            _Rotation = new AnimatableProperty<Quaternion>(localXform.Rotation);
            _Scale = new AnimatableProperty<Vector3>(localXform.Scale);
            _Translation = new AnimatableProperty<Vector3>(localXform.Translation);

            var mw = Transforms.SparseWeight8.Create(srcNode.MorphWeights);
            _Morphing = new AnimatableProperty<Transforms.SparseWeight8>(mw);

            foreach (var anim in srcNode.LogicalParent.LogicalAnimations)
            {
                var name = anim.Name;
                if (string.IsNullOrWhiteSpace(name)) name = anim.LogicalIndex.ToString(System.Globalization.CultureInfo.InvariantCulture);

                if (animTracks.TryGetValue(name, out float duration)) duration = Math.Max(duration, anim.Duration);
                else duration = anim.Duration;

                animTracks[name] = duration;

                var scaAnim = anim.FindScaleSampler(srcNode)?.CreateCurveSampler(isolateMemory);
                if (scaAnim != null) _Scale.AddCurve(name, scaAnim);

                var rotAnim = anim.FindRotationSampler(srcNode)?.CreateCurveSampler(isolateMemory);
                if (rotAnim != null) _Rotation.AddCurve(name, rotAnim);

                var traAnim = anim.FindTranslationSampler(srcNode)?.CreateCurveSampler(isolateMemory);
                if (traAnim != null) _Translation.AddCurve(name, traAnim);

                var mrpAnim = anim.FindSparseMorphSampler(srcNode)?.CreateCurveSampler(isolateMemory);
                if (mrpAnim != null) _Morphing.AddCurve(name, mrpAnim);
            }
        }

        #endregion

        #region data

        private readonly int _LogicalSourceIndex;

        private readonly int _ParentIndex;
        private readonly int[] _ChildIndices;

        private readonly Matrix4x4 _LocalMatrix;

        private readonly AnimatableProperty<Vector3> _Scale;
        private readonly AnimatableProperty<Quaternion> _Rotation;
        private readonly AnimatableProperty<Vector3> _Translation;
        private readonly AnimatableProperty<Transforms.SparseWeight8> _Morphing;

        #endregion

        #region properties

        public string Name { get; set; }

        /// <summary>
        /// Gets the index of the source <see cref="Schema2.Node"/> in <see cref="Schema2.ModelRoot.LogicalNodes"/>
        /// </summary>
        public int LogicalNodeIndex => _LogicalSourceIndex;

        /// <summary>
        /// Gets the index of the parent <see cref="NodeTemplate"/> in <see cref="SceneTemplate._NodeTemplates"/>
        /// </summary>
        public int ParentIndex => _ParentIndex;

        /// <summary>
        /// Gets the list of indices of the children <see cref="NodeTemplate"/> in <see cref="SceneTemplate._NodeTemplates"/>
        /// </summary>
        public IReadOnlyList<int> ChildIndices => _ChildIndices;

        public Matrix4x4 LocalMatrix => _LocalMatrix;

        #endregion

        #region API

        public Transforms.SparseWeight8 GetMorphWeights(string trackName, float time)
        {
            return _Morphing.GetValueAt(trackName, time);
        }

        public Transforms.AffineTransform GetLocalTransform(string trackName, float time)
        {
            if (string.IsNullOrEmpty(trackName)) return Transforms.AffineTransform.Create(_LocalMatrix);

            var s = _Scale?.GetValueAt(trackName, time);
            var r = _Rotation?.GetValueAt(trackName, time);
            var t = _Translation?.GetValueAt(trackName, time);

            return Transforms.AffineTransform.Create(t, r, s);
        }

        public Matrix4x4 GetLocalMatrix(string trackName, float time)
        {
            if (string.IsNullOrEmpty(trackName)) return _LocalMatrix;

            return GetLocalTransform(trackName, time).Matrix;
        }

        #endregion
    }

    /// <summary>
    /// Defines a reference to a drawable mesh
    /// </summary>
    abstract class DrawableReference
    {
        #region lifecycle

        protected DrawableReference(Schema2.Node node)
        {
            _LogicalMeshIndex = node.Mesh.LogicalIndex;
        }

        #endregion

        #region data

        private readonly int _LogicalMeshIndex;

        #endregion

        #region properties

        /// <summary>
        /// Gets the index of a <see cref="Schema2.Mesh"/> in <see cref="Schema2.ModelRoot.LogicalMeshes"/>
        /// </summary>
        public int LogicalMeshIndex => _LogicalMeshIndex;

        #endregion

        #region API

        public abstract Transforms.IGeometryTransform CreateGeometryTransform();

        public abstract void UpdateGeometryTransform(Transforms.IGeometryTransform geoxform, IReadOnlyList<NodeInstance> instances);

        #endregion
    }

    /// <summary>
    /// Defines a reference to a drawable static mesh
    /// </summary>
    sealed class StaticDrawableReference : DrawableReference
    {
        #region lifecycle

        internal StaticDrawableReference(Schema2.Node node, Func<Schema2.Node,int> indexFunc) : base(node)
        {
            _NodeIndex = indexFunc(node);
        }

        #endregion

        #region data

        private readonly int _NodeIndex;

        #endregion

        #region API

        public override Transforms.IGeometryTransform CreateGeometryTransform() { return new Transforms.StaticTransform(); }

        public override void UpdateGeometryTransform(Transforms.IGeometryTransform geoxform, IReadOnlyList<NodeInstance> instances)
        {
            var node = instances[_NodeIndex];

            var statxform = (Transforms.StaticTransform)geoxform;
            statxform.Update(node.WorldMatrix);
            statxform.Update(node.MorphWeights, false);
        }

        #endregion
    }

    /// <summary>
    /// Defines a reference to a drawable skinned mesh
    /// </summary>
    sealed class SkinnedDrawableReference : DrawableReference
    {
        #region lifecycle

        internal SkinnedDrawableReference(Schema2.Node node, Func<Schema2.Node, int> indexFunc) : base(node)
        {
            var skin = node.Skin;

            _MorphNodeIndex = indexFunc(node);

            _JointsNodeIndices = new int[skin.JointsCount];
            _BindMatrices = new System.Numerics.Matrix4x4[skin.JointsCount];            

            for(int i=0; i < _JointsNodeIndices.Length; ++i)
            {
                var jm = skin.GetJoint(i);

                _JointsNodeIndices[i] = indexFunc(jm.Item1);
                _BindMatrices[i] = jm.Item2;
            }
        }

        #endregion

        #region data

        private readonly int _MorphNodeIndex;
        private readonly int[] _JointsNodeIndices;
        private readonly Matrix4x4[] _BindMatrices;

        #endregion

        #region API

        public override Transforms.IGeometryTransform CreateGeometryTransform() { return new Transforms.SkinTransform(); }

        public override void UpdateGeometryTransform(Transforms.IGeometryTransform geoxform, IReadOnlyList<NodeInstance> instances)
        {
            var skinxform = (Transforms.SkinTransform)geoxform;
            skinxform.Update(_JointsNodeIndices.Length, idx => _BindMatrices[idx], idx => instances[_JointsNodeIndices[idx]].WorldMatrix);
            skinxform.Update(instances[_MorphNodeIndex].MorphWeights, false);
        }

        #endregion
    }

    /// <summary>
    /// Defines a templatized representation of a <see cref="Schema2.Scene"/> that can be used
    /// to create <see cref="SceneInstance"/>, which can help render a scene on a client application.
    /// </summary>
    public class SceneTemplate
    {
        #region lifecycle

        /// <summary>
        /// Creates a new <see cref="SceneTemplate"/> from a given <see cref="Schema2.Scene"/>.
        /// </summary>
        /// <param name="srcScene">The source <see cref="Schema2.Scene"/> to templateize.</param>
        /// <param name="isolateMemory">True if we want to copy data instead of sharing it.</param>
        /// <returns>A new <see cref="SceneTemplate"/> instance.</returns>
        public static SceneTemplate Create(Schema2.Scene srcScene, bool isolateMemory)
        {
            Guard.NotNull(srcScene, nameof(srcScene));

            var srcNodes = Schema2.Node.Flatten(srcScene)
                .Select((key, idx) => (key, idx))
                .ToDictionary(pair => pair.key, pair => pair.idx);

            int indexSolver(Schema2.Node srcNode)
            {
                if (srcNode == null) return -1;
                return srcNodes[srcNode];
            }

            var dstTracks = new Dictionary<string, float>();

            var dstNodes = new NodeTemplate[srcNodes.Count];

            foreach (var srcNode in srcNodes)
            {
                var nidx = srcNode.Value;

                // parent index
                var pidx = indexSolver(srcNode.Key.VisualParent);
                if (pidx >= nidx) throw new InvalidOperationException("parent indices should be below child indices");

                // child indices
                var cidx = srcNode.Key.VisualChildren
                    .Select(n => indexSolver(n))
                    .ToArray();

                dstNodes[nidx] = new NodeTemplate(srcNode.Key, pidx, cidx, isolateMemory, dstTracks);
            }

            var instances = srcNodes.Keys
                .Where(item => item.Mesh != null)
                .ToList();

            var drawables = new DrawableReference[instances.Count];

            for (int i = 0; i < drawables.Length; ++i)
            {
                var srcInstance = instances[i];

                drawables[i] = srcInstance.Skin != null
                    ?
                    (DrawableReference)new SkinnedDrawableReference(srcInstance, indexSolver)
                    :
                    (DrawableReference)new StaticDrawableReference(srcInstance, indexSolver);
            }

            return new SceneTemplate(dstNodes, drawables, dstTracks);
        }

        private SceneTemplate(NodeTemplate[] nodes, DrawableReference[] drawables, IReadOnlyDictionary<string, float> animTracks)
        {
            _NodeTemplates = nodes;
            _DrawableReferences = drawables;
            _AnimationTracks = animTracks;
        }

        #endregion

        #region data

        private readonly NodeTemplate[] _NodeTemplates;
        private readonly DrawableReference[] _DrawableReferences;

        private readonly IReadOnlyDictionary<string, float> _AnimationTracks;

        #endregion

        #region properties

        /// <summary>
        /// Gets the unique indices of <see cref="Schema2.Mesh"/> instances in <see cref="Schema2.ModelRoot.LogicalMeshes"/>
        /// </summary>
        public IEnumerable<int> LogicalMeshIds => _DrawableReferences.Select(item => item.LogicalMeshIndex).Distinct();

        /// <summary>
        /// Gets A collection of animation track names, and the time duration of each track, in seconds.
        /// </summary>
        public IReadOnlyDictionary<string, float> AnimationTracks => _AnimationTracks;

        #endregion

        #region API

        /// <summary>
        /// Creates a new <see cref="SceneInstance"/> of this <see cref="SceneTemplate"/>
        /// that can be used to render the scene.
        /// </summary>
        /// <returns>A new <see cref="SceneInstance"/> object.</returns>
        public SceneInstance CreateInstance()
        {
            var inst = new SceneInstance(_NodeTemplates, _DrawableReferences, _AnimationTracks);

            inst.SetPoseTransforms();

            return inst;
        }

        #endregion
    }
}
