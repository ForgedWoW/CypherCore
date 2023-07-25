// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Constants;

namespace Forged.MapServer.DataStorage.Structs.A
{
    public sealed class AchievementRecord
    {
        public string Description;
        public string Title;
        public string Reward;
        public uint Id;
        public short InstanceID;
        public AchievementFaction Faction;
        public ushort Supercedes;
        public ushort Category;
        public byte MinimumCriteria;
        public byte Points;
        public AchievementFlags Flags;
        public ushort UiOrder;
        public uint IconFileID;
        public int RewardItemID;
        public uint CriteriaTree;
        public ushort SharesCriteria;
        public int CovenantID;
    }
}
