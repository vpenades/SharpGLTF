using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using NUnit.Framework;

namespace glTF2Sharp.Schema2
{
    [TestFixture]
    public class AccessorSparseTests
    {
        [Test]
        public void LoadMorphCubeModel()
        {
            foreach (var path in TestFiles.GetGeneratedFilePaths())
            {
                var model = ModelRoot.Load(path);
                Assert.NotNull(model);

                var primitives = model.LogicalMeshes
                    .SelectMany(item => item.Primitives)
                    .Where(item => item.MorpthTargets > 0);
                
                foreach (var primitive in primitives)
                {
                    var basePositions = primitive.GetVertexAccessor("POSITION").CastToVector3Accessor();

                    for (int i = 0; i < primitive.MorpthTargets; ++i)
                    {
                        var morphs = primitive.GetMorphTargetAccessors(i);
                        Assert.NotNull(morphs);

                        var morphPositions = morphs["POSITION"].CastToVector3Accessor();

                        // Assert.AreEqual(basePositions.Count, morphPositions.Count);

                        if (morphs["POSITION"].IsSparse)
                        {
                            TestContext.WriteLine($"{path}");
                        }
                    }
                }                
            }

            
        }
    }
}
