// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Forged.MapServer.DataStorage.Structs.A;

public sealed class AzeriteUnlockMappingRecord
{
	public uint Id;
	public int ItemLevel;
	public uint ItemBonusListHead;
	public uint ItemBonusListShoulders;
	public uint ItemBonusListChest;
	public uint AzeriteUnlockMappingSetID;
}