// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Game.Networking.Packets;

public struct SubDamage
{
	public int SchoolMask;
	public float FDamage; // Float damage (Most of the time equals to Damage)
	public int Damage;
	public int Absorbed;
	public int Resisted;
}