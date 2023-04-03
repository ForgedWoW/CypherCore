// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Forged.MapServer.Arenas;

public struct ArenaTeamStats
{
    public uint Rank { get; set; }
    public ushort Rating { get; set; }
    public ushort SeasonGames { get; set; }
    public ushort SeasonWins { get; set; }
    public ushort WeekGames { get; set; }
    public ushort WeekWins { get; set; }
}