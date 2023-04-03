// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Forged.MapServer.BattleGrounds.Zones;

internal enum ABNodeStatus
{
    Neutral = 0,
    Contested = 1,
    AllyContested = 1,
    HordeContested = 2,
    Occupied = 3,
    AllyOccupied = 3,
    HordeOccupied = 4
}