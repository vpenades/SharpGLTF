using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
using System.Xml.Linq;

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

            foreach (var srcChannel in srcMaterial.Channels)
            {
                var key = srcChannel.Key;

                var txform = srcChannel.TextureTransform;                
                var scale = txform == null ? null : new AnimatableProperty<Vector2>(txform.Scale);
                var rotation = txform == null ? null : new AnimatableProperty<float>(txform.Rotation);
                var offset = txform == null ? null : new AnimatableProperty<Vector2>(txform.Offset);                

                foreach (var srcAnim in srcMaterial.LogicalParent.LogicalAnimations)
                {
                    var lidx = srcAnim.LogicalIndex;

                    // paths look like:
                    // "/materials/14/pbrMetallicRoughness/metallicRoughnessTexture/extensions/KHR_texture_transform/scale"

                    var pointerPath = srcChannel.GetAnimationPointer();

                    foreach (var c in srcAnim.FindChannels(pointerPath))
                    {
                        System.Diagnostics.Trace.WriteLine(c.TargetPointerPath);

                        if (c.TargetPointerPath.EndsWith("/extensions/KHR_texture_transform/scale"))
                        {
                            if (scale == null) throw new InvalidOperationException("found pointer to a TextureTransform, but target was not found");
                            var sampler = c.GetSamplerOrNull<Vector2>().CreateCurveSampler(isolateMemory);
                            scale.SetCurve(lidx, sampler);
                        }

                        if (c.TargetPointerPath.EndsWith("/extensions/KHR_texture_transform/rotation"))
                        {
                            if (rotation == null) throw new InvalidOperationException("found pointer to a TextureTransform, but target was not found");
                            var sampler = c.GetSamplerOrNull<float>().CreateCurveSampler(isolateMemory);
                            rotation.SetCurve(lidx, sampler);
                        }

                        if (c.TargetPointerPath.EndsWith("/extensions/KHR_texture_transform/offset"))
                        {
                            if (offset == null) throw new InvalidOperationException("found pointer to a TextureTransform, but target was not found");
                            var sampler = c.GetSamplerOrNull<Vector2>().CreateCurveSampler(isolateMemory);
                            offset.SetCurve(lidx, sampler);
                        }
                    }
                }
            }
        }

        #endregion

        #region data
        
        private readonly int _LogicalSourceIndex;       

        #endregion

        #region properties

        public string Name { get; set; }

        public Object Extras { get; set; }

        /// <summary>
        /// Gets the index of the source <see cref="Schema2.Material"/> in <see cref="Schema2.ModelRoot.LogicalMaterials"/>
        /// </summary>
        public int LogicalNodeIndex => _LogicalSourceIndex;

        #endregion

        #region API

        public Matrix3x2 GetTextureTransform(int trackLogicalIndex, float time)
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}
