// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Game.DataStorage;

public sealed class ItemBonusTreeNodeRecord
{
	public uint Id;
	public byte ItemContext;
	public ushort ChildItemBonusTreeID;
	public ushort ChildItemBonusListID;
	public ushort ChildItemLevelSelectorID;
	public uint ChildItemBonusListGroupID;
	public uint IblGroupPointsModSetID;
	public uint ParentItemBonusTreeID;
}