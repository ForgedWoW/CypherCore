// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;

namespace Forged.MapServer.DataStorage.Structs.I;

public sealed class ItemBonusRecord
{
	public uint Id;
	public int[] Value = new int[4];
	public ushort ParentItemBonusListID;
	public ItemBonusType BonusType;
	public byte OrderIndex;
}