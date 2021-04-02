using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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

        private const Boolean _IsAttachPointDefault = false;

        public String ArticulationName
        {
            get => _articulationName;
            set => _articulationName = value;
        }

        public Boolean IsAttachPoint
        {
            get => _isAttachPoint.AsValue(_IsAttachPointDefault);
            set => _isAttachPoint = value.AsNullable(_IsAttachPointDefault);
        }
    }

    public sealed partial class AgiArticulation : IChildOf<AgiRootArticulations>
    {
        public static readonly ReadOnlyCollection<AgiArticulationTransformType> AgiRotationTypes =
            new ReadOnlyCollection<AgiArticulationTransformType>(new List<AgiArticulationTransformType>
            {
                AgiArticulationTransformType.xRotate,
                AgiArticulationTransformType.yRotate,
                AgiArticulationTransformType.zRotate
            });

        internal AgiArticulation()
        {
            _stages = new ChildrenCollection<AgiArticulationStage, AgiArticulation>(this);
        }

        protected override IEnumerable<ExtraProperties> GetLogicalChildren()
        {
            return base.GetLogicalChildren().Concat(_stages);
        }

        public IReadOnlyList<AgiArticulationStage> Stages => _stages;

        public AgiArticulationStage CreateArticulationStage(string name, AgiArticulationTransformType transformType)
        {
            var stage = new AgiArticulationStage(name, transformType);

            if (_pointingVector.HasValue && AgiRotationTypes.Contains(transformType))
            {
                // If one rotation stage exists, we may add a second rotation stage, but that's the limit
                // for pointing to be enabled. Additional non-rotation stages are always allowed.
                var numRotationStages = _stages.Where(s => AgiRotationTypes.Contains(s.TransformType)).Count();
                Guard.IsTrue(numRotationStages <= 1, "transformType",
                    "Cannot add more than 2 rotation stages when a PointingVector is in use.");
            }

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
            set
            {
                if (!value.HasValue)
                {
                    // Pointing is turned off.
                    _pointingVector = null;
                }
                else
                {
                    Guard.IsTrue(value.Value.IsNormalized(), "PointingVector", "PointingVector must be a unit-length vector.");

                    var numRotationStages = _stages.Where(s => AgiRotationTypes.Contains(s.TransformType)).Count();
                    Guard.IsTrue(numRotationStages == 1 || numRotationStages == 2, "PointingVector",
                        "PointingVector requires exactly 1 or exactly 2 rotation stages.");

                    _pointingVector = value;
                }
            }
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
        internal AgiArticulationStage()
        {
            _type = _TransformTypeDefault;
        }

        internal AgiArticulationStage(string name, AgiArticulationTransformType transformType)
        {
            _name = name;
            _type = transformType;
        }

        // The default transform type is not one of the rotation types, so is always safe to add.
        private const AgiArticulationTransformType _TransformTypeDefault = AgiArticulationTransformType.xTranslate;

        public void SetValues(Double minValue, Double initial, Double maxValue)
        {
            // Use "IsTrue" (not "MustBeLessThanOrEqual") so the message can mention 2 different parameters by name.
            Guard.IsTrue(minValue <= initial, "minValue", "Minimm value must be less than or equal to initial value.");
            Guard.IsTrue(initial <= maxValue, "initial", "Initial value must be less than or equal to maximum value.");

            _minimumValue = minValue;
            _initialValue = initial;
            _maximumValue = maxValue;
        }

        public int LogicalIndex { get; private set; } = -1;

        public AgiArticulation LogicalParent { get; private set; }

        public void _SetLogicalParent(AgiArticulation parent, int index)
        {
            LogicalParent = parent;
            LogicalIndex = index;
        }

        public String Name => _name;

        public AgiArticulationTransformType TransformType => _type;

        public Double MinimumValue => _minimumValue;

        public Double InitialValue => _initialValue;

        public Double MaximumValue => _maximumValue;
    }
}
