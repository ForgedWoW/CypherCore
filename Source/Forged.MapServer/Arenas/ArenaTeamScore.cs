// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Forged.MapServer.Arenas;

public class ArenaTeamScore
{
    public uint PostMatchMmr { get; set; }
    public uint PostMatchRating { get; set; }
    public uint PreMatchMmr { get; set; }
    public uint PreMatchRating { get; set; }

    public void Assign(uint preMatchRating, uint postMatchRating, uint preMatchMmr, uint postMatchMmr)
    {
        PreMatchRating = preMatchRating;
        PostMatchRating = postMatchRating;
        PreMatchMmr = preMatchMmr;
        PostMatchMmr = postMatchMmr;
    }
}