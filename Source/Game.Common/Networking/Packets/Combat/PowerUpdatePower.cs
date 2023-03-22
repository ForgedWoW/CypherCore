// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Game.Networking.Packets;

public struct PowerUpdatePower
{
	public PowerUpdatePower(int power, byte powerType)
	{
		Power = power;
		PowerType = powerType;
	}

	public int Power;
	public byte PowerType;
}