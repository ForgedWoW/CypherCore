// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Game.Common.Networking.Packets.Spell;

public struct SpellLogPowerData
{
	public SpellLogPowerData(int powerType, int amount, int cost)
	{
		PowerType = powerType;
		Amount = amount;
		Cost = cost;
	}

	public int PowerType;
	public int Amount;
	public int Cost;
}
