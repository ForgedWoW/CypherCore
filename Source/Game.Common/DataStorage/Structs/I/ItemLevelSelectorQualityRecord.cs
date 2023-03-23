using Game.DataStorage;
// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Game.Common.DataStorage.Structs.I;

public sealed class ItemLevelSelectorQualityRecord
{
	public uint Id;
	public uint QualityItemBonusListID;
	public sbyte Quality;
	public uint ParentILSQualitySetID;
}
