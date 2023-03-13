using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Game.Entities;

namespace Game.Networking.Packets.Quest
{
    public class QueryQuestItemUsability : ClientPacket
    {
        public ObjectGuid CreatureGUID;
        public List<ObjectGuid> ItemGUIDs = new List<ObjectGuid>();
        public QueryQuestItemUsability(WorldPacket worldPacket) : base(worldPacket)
        {
        }

        public override void Read()
        {
            CreatureGUID = _worldPacket.ReadPackedGuid();
            var itemGuidCount = _worldPacket.ReadUInt32();

            for (var i = 0; i < itemGuidCount; ++i)
                ItemGUIDs.Add(_worldPacket.ReadPackedGuid());
        }
    }
}
