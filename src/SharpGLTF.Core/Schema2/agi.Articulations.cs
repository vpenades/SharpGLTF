using System;
using System.Collections.Generic;
using System.Numerics;
using System.Linq;
using SharpGLTF.Collections;

namespace SharpGLTF.Schema2
{
    public sealed partial class AgiRootArticulations
    {
        internal AgiRootArticulations(ModelRoot root)
        {
            _articulations = new ChildrenCollection<AgiArticulation, AgiRootArticulations>(this);
        }

        protected override IEnumerable<ExtraProperties> GetLogicalChildren()
        {
            return base.GetLogicalChildren().Concat(_articulations);
        }

        public IReadOnlyList<AgiArticulation> Articulations => _articulations;

        public AgiArticulation CreateArticulation(string name)
        {
            var articulation = new AgiArticulation();
            articulation.Name = name;

            _articulations.Add(articulation);

            return articulation;
        }
    }

    public sealed partial class AgiNodeArticulations
    {
        internal AgiNodeArticulations(Node node) { }

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

    public sealed partial class AgiArticulation : IChildOf<AgiRootArticulations>
    {
        internal AgiArticulation()
        {
            _stages = new ChildrenCollection<AgiArticulationStage, AgiArticulation>(this);
        }

        protected override IEnumerable<ExtraProperties> GetLogicalChildren()
        {
            return base.GetLogicalChildren().Concat(_stages);
        }

        public IReadOnlyList<AgiArticulationStage> Stages => _stages;

        public AgiArticulationStage CreateArticulationStage(string name)
        {
            var stage = new AgiArticulationStage();
            stage.Name = name;

            _stages.Add(stage);

            return stage;
        }

        public String Name
        {
            get => _name;
            set => _name = value;
        }

        public Vector3? PointingVector
        {
            get => _pointingVector;
            set => _pointingVector = value;
        }

        public int LogicalIndex { get; private set; } = -1;

        public AgiRootArticulations LogicalParent { get; private set; }

        public void _SetLogicalParent(AgiRootArticulations parent, int index)
        {
            LogicalParent = parent;
            LogicalIndex = index;
        }
    }

    public sealed partial class AgiArticulationStage : IChildOf<AgiArticulation>
    {
        public int LogicalIndex { get; private set; } = -1;

        public AgiArticulation LogicalParent { get; private set; }

        public void _SetLogicalParent(AgiArticulation parent, int index)
        {
            LogicalParent = parent;
            LogicalIndex = index;
        }

        public String Name
        {
            get => _name;
            set => _name = value;
        }

        public AgiArticulationTransformType TransformType
        {
            get => _type;
            set => _type = value;
        }

        public Double MinimumValue
        {
            get => _minimumValue;
            set => _minimumValue = value;
        }

        public Double InitialValue
        {
            get => _initialValue;
            set => _initialValue = value;
        }

        public Double MaximumValue
        {
            get => _maximumValue;
            set => _maximumValue = value;
        }
    }
}
