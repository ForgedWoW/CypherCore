// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Game.Common.DataStorage.Structs.H;

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
