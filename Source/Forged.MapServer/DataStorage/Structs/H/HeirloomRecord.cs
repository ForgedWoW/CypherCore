// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Forged.MapServer.DataStorage.Structs.H;

public sealed record HeirloomRecord
{
    public byte Flags;
    public uint Id;
    public uint ItemID;
    public int LegacyItemID;
    public int LegacyUpgradedItemID;
    public string SourceText;
    public sbyte SourceTypeEnum;
    public uint StaticUpgradedItemID;
    public ushort[] UpgradeItemBonusListID = new ushort[6];
    public int[] UpgradeItemID = new int[6];
}