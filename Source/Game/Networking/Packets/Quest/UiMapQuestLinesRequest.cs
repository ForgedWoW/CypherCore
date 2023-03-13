using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Game.Networking.Packets.Quest
{
    public class UiMapQuestLinesRequest : ClientPacket
    {
        public int UiMapID;

        public UiMapQuestLinesRequest(WorldPacket worldPacket) : base(worldPacket)
        {
        }

        public override void Read()
        {
            UiMapID = _worldPacket.ReadInt32();
        }
    }
}
