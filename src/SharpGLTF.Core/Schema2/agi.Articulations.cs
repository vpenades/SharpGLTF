using System;
using System.Collections.Generic;
using System.Text;

namespace SharpGLTF.Schema2
{
    public sealed partial class ArticulationStage
    {
    }

    public sealed partial class Articulation
    {
    }

    public sealed partial class AGI_articulationsglTFextension
    {
    }

    public sealed partial class AGI_articulationsglTFNodeextension
    {
        public String ArticulationName => _articulationName;
        public Boolean? IsAttachPoint => _isAttachPoint;
    }
}
