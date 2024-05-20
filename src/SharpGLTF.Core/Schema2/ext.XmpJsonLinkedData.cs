using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;

using SharpGLTF.Collections;

using XMPJSONLD = System.Text.Json.Nodes.JsonNode;

namespace SharpGLTF.Schema2
{
    public partial class XmpPackets
    {
        internal XmpPackets(ModelRoot root)
        {
            _packets = new List<XMPJSONLD>();
        }

        
        public IReadOnlyList<XMPJSONLD> JsonPackets => _packets;

        #if NET6_0_OR_GREATER
        [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(System.Text.Json.Nodes.JsonArray))]
        [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(System.Text.Json.Nodes.JsonValue))]
        [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(System.Text.Json.Nodes.JsonObject))]
        #endif
        public int AddPacket(XMPJSONLD packet)
        {
            if (_packets.Contains(packet)) throw new ArgumentException("Already exists", nameof(packet));
            _packets.Add(packet);
            return _packets.Count -1;
        }
    }

    public partial class XmpPacketReference
    {
        internal XmpPacketReference(ExtraProperties owner)
        {
            _Owner = owner;
        }

        private ExtraProperties _Owner;

        private ModelRoot _FindRoot()
        {
            if (_Owner is LogicalChildOfRoot logicalChild) return logicalChild.LogicalParent;
            return null;
        }

        private XmpPackets _GetPackets()
        {
            return _FindRoot()?.GetExtension<XmpPackets>();
        }

        public int PacketLogicalIndex => _packet;

        public XMPJSONLD JsonPacket => _GetPackets()?.JsonPackets[_packet];

        public void SetPacket(int logicalIndex)
        {
            if (logicalIndex < 0) throw new ArgumentOutOfRangeException(nameof(logicalIndex));

            var packets = _GetPackets();
            if (packets != null && logicalIndex >= packets.JsonPackets.Count) throw new ArgumentOutOfRangeException(nameof(logicalIndex));

            _packet = logicalIndex;
        }
    }

    
}
