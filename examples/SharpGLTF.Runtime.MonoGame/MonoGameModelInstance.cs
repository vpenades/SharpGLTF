using System;
using System.Collections.Generic;
using System.Linq;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

#if USINGMONOGAMEMODEL
using MODELMESH = Microsoft.Xna.Framework.Graphics.ModelMesh;
using MODELMESHPART = Microsoft.Xna.Framework.Graphics.ModelMeshPart;
#else
using MODELMESH = SharpGLTF.Runtime.ModelMeshReplacement;
using MODELMESHPART = SharpGLTF.Runtime.ModelMeshPartReplacement;
#endif

namespace SharpGLTF.Runtime
{
    public sealed class MonoGameModelInstance
    {
        #region lifecycle

        internal MonoGameModelInstance(MonoGameModelTemplate template, SceneInstance instance)
        {
            _Template = template;
            _Controller = instance;
        }

        #endregion

        #region data

        private readonly MonoGameModelTemplate _Template;
        private readonly SceneInstance _Controller;

        #endregion

        #region properties

        /// <summary>
        /// Gets a reference to the template used to create this <see cref="MonoGameModelInstance"/>.
        /// </summary>
        public MonoGameModelTemplate Template => _Template;

        /// <summary>
        /// Gets a reference to the animation controller of this <see cref="MonoGameModelInstance"/>.
        /// </summary>
        public SceneInstance Controller => _Controller;

        #endregion

        #region API

        /// <summary>
        /// Draws this <see cref="MonoGameModelInstance"/> into the current <see cref="GraphicsDevice"/>.
        /// </summary>
        /// <param name="projection">The projection matrix.</param>
        /// <param name="view">The view matrix.</param>
        /// <param name="world">The world matrix.</param>
        public void Draw(Matrix projection, Matrix view, Matrix world)
        {
            foreach (var d in _Controller.DrawableInstances)
            {
                Draw(_Template._Meshes[d.Template.LogicalMeshIndex], projection, view, world, d.Transform);
            }
        }

        private void Draw(MODELMESH mesh, Matrix projectionXform, Matrix viewXform, Matrix worldXform, Transforms.IGeometryTransform modelXform)
        {
            if (modelXform is Transforms.SkinnedTransform skinXform)
            {
                var skinTransforms = skinXform.SkinMatrices.Select(item => item.ToXna()).ToArray();

                foreach (var effect in mesh.Effects)
                {
                    UpdateTransforms(effect, projectionXform, viewXform, worldXform, skinTransforms);
                }
            }

            if (modelXform is Transforms.RigidTransform statXform)
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
