using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Game.Entities;

namespace Game.Networking.Packets.Quest
{
    public class QuestItemUsability : ClientPacket
    {
        public ObjectGuid CreatureGUID;

        public QuestItemUsability(WorldPacket worldPacket) : base(worldPacket)
        {
        }

        public override void Read()
        {
//            CreatureGUID = _worldPacket.ReadPackedGuid128("CreatureGUID");
//var itemGuidCount = _worldPacket.ReadUInt32("ItemGuidCount");
//            for (var i = 0; i < itemGuidCount; ++i)
//                _worldPacket.ReadPackedGuid128("ItemGUID", i);
        }
    }
}
