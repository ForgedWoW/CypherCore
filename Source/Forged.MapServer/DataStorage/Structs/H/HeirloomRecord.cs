// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

namespace Forged.MapServer.DataStorage.Structs.H
{
    public sealed class HeirloomRecord
    {
        public string SourceText;
        public uint Id;
        public uint ItemID;
        public int LegacyUpgradedItemID;
        public uint StaticUpgradedItemID;
        public sbyte SourceTypeEnum;
        public byte Flags;
        public int LegacyItemID;
        public int[] UpgradeItemID = new int[6];
        public ushort[] UpgradeItemBonusListID = new ushort[6];
    }
}
