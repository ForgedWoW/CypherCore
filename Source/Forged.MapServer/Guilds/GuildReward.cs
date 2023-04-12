// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;

namespace Forged.MapServer.Guilds;

public class GuildReward
{
    public List<uint> AchievementsRequired { get; set; } = new();
    public ulong Cost { get; set; }
    public uint ItemID { get; set; }
    public byte MinGuildRep { get; set; }
    public ulong RaceMask { get; set; }
}