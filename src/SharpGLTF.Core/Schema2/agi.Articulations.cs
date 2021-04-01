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

    public sealed partial class RootAgiArticulations
    {
    }

    public sealed partial class NodeAgiArticulations
    {
        internal NodeAgiArticulations(Node node) { }

        public String ArticulationName
        {
            get => _articulationName;
            set => _articulationName = value;
        }

        public Boolean? IsAttachPoint
        {
            get => _isAttachPoint;
            set => _isAttachPoint = value;
        }
    }
}
