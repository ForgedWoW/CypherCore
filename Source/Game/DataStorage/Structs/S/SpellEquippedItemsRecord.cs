// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Game.DataStorage;

public sealed class SpellEquippedItemsRecord
{
	public uint Id;
	public uint SpellID;
	public sbyte EquippedItemClass;
	public int EquippedItemInvTypes;
	public int EquippedItemSubclass;
}