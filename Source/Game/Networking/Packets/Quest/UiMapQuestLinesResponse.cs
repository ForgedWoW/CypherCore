using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Framework.Constants;

namespace Game.Networking.Packets.Quest
{
    public class UiMapQuestLinesResponse : ServerPacket
    {
        public int UiMapID;
        public List<uint> QuestLineXQuestIDs = new List<uint>();

        public UiMapQuestLinesResponse() : base(ServerOpcodes.UiMapQuestLinesResponse)
        {
        }

        public override void Write()
        {
            _worldPacket.Write(UiMapID);
            _worldPacket.WriteUInt32((uint)QuestLineXQuestIDs.Count);

            foreach (var item in QuestLineXQuestIDs)
                _worldPacket.Write(item);
        }
    }
}
