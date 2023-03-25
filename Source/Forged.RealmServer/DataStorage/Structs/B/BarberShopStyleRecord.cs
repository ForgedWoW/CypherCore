// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Forged.RealmServer.DataStorage;

public sealed class BarberShopStyleRecord
{
	public uint Id;
	public string DisplayName;
	public string Description;
	public byte Type; // value 0 . hair, value 2 . facialhair
	public float CostModifier;
	public byte Race;
	public byte Sex;
	public byte Data; // real ID to hair/facial hair
}