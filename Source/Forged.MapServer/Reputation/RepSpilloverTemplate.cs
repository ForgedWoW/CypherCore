// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Forged.MapServer.Reputation;

public class RepSpilloverTemplate
{
    public uint[] Faction { get; set; } = new uint[5];
    public uint[] FactionRank { get; set; } = new uint[5];
    public float[] FactionRate { get; set; } = new float[5];
}