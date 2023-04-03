// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Forged.MapServer.BattleGrounds.Zones;

internal enum SAGateState
{
    // alliance is defender
    AllianceGateOk = 1,
    AllianceGateDamaged = 2,
    AllianceGateDestroyed = 3,

    // horde is defender
    HordeGateOk = 4,
    HordeGateDamaged = 5,
    HordeGateDestroyed = 6,
}