using System;
using System.Collections.Generic;
using System.Linq;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace SharpGLTF.Runtime
{
    public class MonoGameModelInstance
    {
        #region lifecycle

        internal MonoGameModelInstance(MonoGameModelTemplate template, Runtime.SceneInstance instance)
        {
            _Template = template;
            _Instance = instance;
        }

        #endregion

        #region data

        private readonly MonoGameModelTemplate _Template;
        private readonly Runtime.SceneInstance _Instance;

        #endregion

        #region properties

        public MonoGameModelTemplate Template => _Template;

        public Runtime.SceneInstance Controller => _Instance;

        #endregion

        #region API

        public void Draw(Matrix projection, Matrix view, Matrix world)
        {
            foreach (var d in _Instance.DrawableReferences)
            {
                Draw(_Template._Meshes[d.Item1], projection, view, world, d.Item2);
            }
        }

        private void Draw(ModelMesh mesh, Matrix projectionXform, Matrix viewXform, Matrix worldXform, Transforms.IGeometryTransform modelXform)
        {
            if (modelXform is Transforms.SkinTransform skinXform)
            {
                var skinTransforms = skinXform.SkinMatrices.Select(item => item.ToXna()).ToArray();

                foreach (var effect in mesh.Effects)
                {
                    UpdateTransforms(effect, projectionXform, viewXform, worldXform, skinTransforms);
                }
            }

            if (modelXform is Transforms.StaticTransform statXform)
            {
                var statTransform = statXform.WorldMatrix.ToXna();

                worldXform = Matrix.Multiply(statTransform, worldXform);

                foreach (var effect in mesh.Effects)
                {
                    UpdateTransforms(effect, projectionXform, viewXform, worldXform);
                }
            }

            mesh.Draw();
        }

        private static void UpdateTransforms(Effect effect, Matrix projectionXform, Matrix viewXform, Matrix worldXform, Matrix[] skinTransforms = null)
        {
            if (effect is IEffectMatrices matrices)
            {
                matrices.Projection = projectionXform;
                matrices.View = viewXform;
                matrices.World = worldXform;
            }

            if (effect is SkinnedEffect skin && skinTransforms != null)
            {
                var xposed = skinTransforms.Select(item => Matrix.Transpose(item)).ToArray();

                skin.SetBoneTransforms(skinTransforms);
            }
        }

        #endregion
    }
}
