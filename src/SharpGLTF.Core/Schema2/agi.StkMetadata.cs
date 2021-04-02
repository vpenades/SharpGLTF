using System;
using System.Collections.Generic;
using System.Linq;
using SharpGLTF.Collections;

namespace SharpGLTF.Schema2
{
    public sealed partial class AgiRootStkMetadata
    {
        internal AgiRootStkMetadata(ModelRoot root)
        {
            _solarPanelGroups = new ChildrenCollection<AgiStkSolarPanelGroup, AgiRootStkMetadata>(this);
        }

        protected override IEnumerable<ExtraProperties> GetLogicalChildren()
        {
            return base.GetLogicalChildren().Concat(_solarPanelGroups);
        }

        public IReadOnlyList<AgiStkSolarPanelGroup> SolarPanelGroups => _solarPanelGroups;

        public AgiStkSolarPanelGroup CreateSolarPanelGroup(string name)
        {
            var group = new AgiStkSolarPanelGroup();
            group.Name = name;

            _solarPanelGroups.Add(group);

            return group;
        }
    }

    public sealed partial class AgiNodeStkMetadata
    {
        internal AgiNodeStkMetadata(Node node) { }

        private const Boolean _NoObscurationDefault = false;

        public String SolarPanelGroupName
        {
            get => _solarPanelGroupName;
            set => _solarPanelGroupName = value;
        }

        public Boolean NoObscuration
        {
            get => _noObscuration.AsValue(_NoObscurationDefault);
            set => _noObscuration = value.AsNullable(_NoObscurationDefault);
        }
    }

    public sealed partial class AgiStkSolarPanelGroup : IChildOf<AgiRootStkMetadata>
    {
        public int LogicalIndex { get; private set; } = -1;

        public AgiRootStkMetadata LogicalParent { get; private set; }

        public void _SetLogicalParent(AgiRootStkMetadata parent, int index)
        {
            LogicalParent = parent;
            LogicalIndex = index;
        }

        public String Name
        {
            get => _name;
            set => _name = value;
        }

        public Double Efficiency
        {
            get => _efficiency;
            set => _efficiency = value;
        }
    }
}
