using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Game.DataStorage.Structs.Q
{
    public class QuestPOIBlobEntry
    {
        public int ID;
        public short MapID;
        public int UiMapID;
        public byte NumPoints;
        public uint QuestID;
        public int ObjectiveIndex;
        public int ObjectiveID;
        public uint PlayerConditionID;
        public uint UNK_9_0_1;
    }

}
