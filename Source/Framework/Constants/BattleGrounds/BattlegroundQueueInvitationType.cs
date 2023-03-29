// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Framework.Constants;

public enum BattlegroundQueueInvitationType
{
    NoBalance = 0, // no balance: N+M vs N players
    Balanced = 1,  // teams balanced: N+1 vs N players
    Even = 2       // teams even: N vs N players
}