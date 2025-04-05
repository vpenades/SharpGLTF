using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
using System.Xml.Linq;

using SharpGLTF.Schema2;

namespace SharpGLTF.Runtime
{
    class MaterialTemplate
    {
        #region lifecycle

        internal MaterialTemplate(Schema2.Material srcMaterial, RuntimeOptions options)
        {
            _LogicalSourceIndex = srcMaterial.LogicalIndex;            

            Name = srcMaterial.Name;
            Extras = RuntimeOptions.ConvertExtras(srcMaterial, options);

            var isolateMemory = options?.IsolateMemory ?? false;

            // gather animation channels targeting this material

            _PointerPrefix = $"/materials/{srcMaterial.LogicalIndex}/";
            _IsAnimated = false;

            foreach (var srcChannel in srcMaterial.Channels)
            {
                // loop over animations to find if any animation points
                // to a property of this material and connect the dots.

                foreach (var srcTrack in srcMaterial.LogicalParent.LogicalAnimations)
                {
                    var trackIdx = srcTrack.LogicalIndex;

                    var channels = srcTrack.FindChannels(_PointerPrefix);                    

                    foreach(var channel in channels)
                    {
                        var pointerPath = channel.TargetPointerPath;
                        System.Diagnostics.Debug.Assert(pointerPath.StartsWith(_PointerPrefix));
                        pointerPath = pointerPath.Substring(_PointerPrefix.Length -1);

                        var fieldInfo = Reflection.FieldInfo.From(srcMaterial, pointerPath);

                        if (fieldInfo.IsEmpty)
                        {
                            System.Diagnostics.Debug.Fail("If it reaches this point it's probably because it's animating a property of an unregistered extension.");
                            continue;
                        }                        

                        if (fieldInfo.Value is float defaultSingle)
                        {
                            _ScalarAnimatables ??= new Dictionary<string, AnimatableProperty<float>>();
                            _AddAnimatableProperty(_ScalarAnimatables, trackIdx, channel, pointerPath, defaultSingle, isolateMemory);
                        }

                        if (fieldInfo.Value is Vector2 defaultVector2)
                        {
                            _Vector2Animatables ??= new Dictionary<string, AnimatableProperty<Vector2>>();
                            _AddAnimatableProperty(_Vector2Animatables, trackIdx, channel, pointerPath, defaultVector2, isolateMemory);
                        }

                        if (fieldInfo.Value is Vector3 defaultVector3)
                        {
                            _Vector3Animatables ??= new Dictionary<string, AnimatableProperty<Vector3>>();
                            _AddAnimatableProperty(_Vector3Animatables, trackIdx, channel, pointerPath, defaultVector3, isolateMemory);
                        }

                        if (fieldInfo.Value is Vector4 defaultVector4)
                        {
                            _Vector4Animatables ??= new Dictionary<string, AnimatableProperty<Vector4>>();
                            _AddAnimatableProperty(_Vector4Animatables, trackIdx, channel, pointerPath, defaultVector4, isolateMemory);
                        }
                    }                    
                }
            }
        }

        private void _AddAnimatableProperty<T>(Dictionary<string, AnimatableProperty<T>> dict, int trackIdx, AnimationChannel channel, string pointerPath, T defaultSingle, bool isolateMemory)
            where T : struct
        {
            if (!dict.TryGetValue(pointerPath, out var target))
            {
                target = new AnimatableProperty<T>(defaultSingle);
                dict[pointerPath] = target;
            }

            var sampler = channel.GetSamplerOrNull<T>().CreateCurveSampler(isolateMemory);
            target.SetCurve(trackIdx, sampler);

            _IsAnimated = true;
        }

        #endregion

        #region data

        private readonly int _LogicalSourceIndex;

        private readonly string _PointerPrefix;

        private readonly Dictionary<string, AnimatableProperty<float>> _ScalarAnimatables;
        private readonly Dictionary<string, AnimatableProperty<Vector2>> _Vector2Animatables;
        private readonly Dictionary<string, AnimatableProperty<Vector3>> _Vector3Animatables;
        private readonly Dictionary<string, AnimatableProperty<Vector4>> _Vector4Animatables;

        private bool _IsAnimated;

        #endregion

        #region properties

        /// <summary>
        /// If true, it means this material is animated.
        /// </summary>
        /// <remarks>
        /// Before rendering a model using this material, you check this property and then call
        /// <see cref="UpdateRuntimeMaterial(int, float, Action{string, float})"/> to update your
        /// engine's material properties
        /// </remarks>
        public bool IsAnimated => _IsAnimated;

        public string Name { get; set; }

        public Object Extras { get; set; }

        /// <summary>
        /// Gets the index of the source <see cref="Schema2.Material"/> in <see cref="Schema2.ModelRoot.LogicalMaterials"/>
        /// </summary>
        public int LogicalNodeIndex => _LogicalSourceIndex;

        #endregion

        #region API


        public void UpdateRuntimeMaterial(int trackLogicalIndex, float time, Action<string, float> target)
        {
            foreach(var kvp in _ScalarAnimatables)
            {
                var val = kvp.Value.GetValueAt(trackLogicalIndex, time);
                target.Invoke(kvp.Key, val);                
            }
        }

        public void UpdateRuntimeMaterial(int trackLogicalIndex, float time, Action<string, Vector2> target)
        {
            foreach (var kvp in _Vector2Animatables)
            {
                var val = kvp.Value.GetValueAt(trackLogicalIndex, time);
                target.Invoke(kvp.Key, val);
            }
        }

        public void UpdateRuntimeMaterial(int trackLogicalIndex, float time, Action<string, Vector3> target)
        {
            foreach (var kvp in _Vector3Animatables)
            {
                var val = kvp.Value.GetValueAt(trackLogicalIndex, time);
                target.Invoke(kvp.Key, val);
            }
        }

        public void UpdateRuntimeMaterial(int trackLogicalIndex, float time, Action<string, Vector4> target)
        {
            foreach (var kvp in _Vector4Animatables)
            {
                var val = kvp.Value.GetValueAt(trackLogicalIndex, time);
                target.Invoke(kvp.Key, val);
            }
        }

        #endregion
    }
}
