// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Game.Common.DataStorage.Structs.I;

public sealed class ItemAppearanceRecord
{
	public uint Id;
	public int DisplayType;
	public uint ItemDisplayInfoID;
	public int DefaultIconFileDataID;
	public int UiOrder;
	public int PlayerConditionID;
}
