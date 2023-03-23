using Game.DataStorage;
// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Game.Common.DataStorage.Structs.I;

public sealed class ItemBonusListGroupEntryRecord
{
	public uint Id;
	public uint ItemBonusListGroupID;
	public int ItemBonusListID;
	public int ItemLevelSelectorID;
	public int OrderIndex;
	public int ItemExtendedCostID;
	public int PlayerConditionID;
}
