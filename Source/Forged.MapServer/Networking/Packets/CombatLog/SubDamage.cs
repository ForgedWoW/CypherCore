// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Forged.MapServer.Networking.Packets.CombatLog;

public struct SubDamage
{
    public int Absorbed;
    public int Damage;
    public float FDamage;
    // Float damage (Most of the time equals to Damage)
    public int Resisted;

    public int SchoolMask;
}