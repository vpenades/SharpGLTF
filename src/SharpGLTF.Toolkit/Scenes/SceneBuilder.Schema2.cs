using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SharpGLTF.Scenes
{
    using Schema2;

    public partial class SceneBuilder
    {

        public ModelRoot ToSchema2()
        {
            var dstModel = ModelRoot.CreateModel();

            // gather all meshes and group them by their attribute layout.

            var meshGroups = _Instances
                .Select(item => item.GetMesh())
                .Where(item => item != null)
                .Distinct()
                .ToList()
                .GroupBy(item => item.GetType());

            // create schema2.mesh collections for every gathered group.

            var meshMap = new Dictionary<Geometry.IMeshBuilder<Materials.MaterialBuilder>, Mesh>();

            foreach (var meshGroup in meshGroups)
            {
                var meshArray = meshGroup.ToArray();

                var meshDst = dstModel.CreateMeshes(meshArray);

                for (int i = 0; i < meshArray.Length; ++i)
                {
                    meshMap[meshArray[i]] = meshDst[i];
                }
            }

            return dstModel;
        }
    }
}
