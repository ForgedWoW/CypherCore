// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Forged.MapServer.Arenas;

public class ArenaTeamScore
{
    public uint PostMatchMMR;
    public uint PostMatchRating;
    public uint PreMatchMMR;
    public uint PreMatchRating;

    public void Assign(uint preMatchRating, uint postMatchRating, uint preMatchMMR, uint postMatchMMR)
    {
        PreMatchRating = preMatchRating;
        PostMatchRating = postMatchRating;
        PreMatchMMR = preMatchMMR;
        PostMatchMMR = postMatchMMR;
    }
}