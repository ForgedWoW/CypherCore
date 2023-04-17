// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Entities.Units;

namespace Scripts.Spells.Generic;

internal class CorruptingPlagueSearcher : ICheck<Unit>
{
    private readonly double _distance;

    private readonly Unit _unit;

    public CorruptingPlagueSearcher(Unit obj, double distance)
    {
        _unit = obj;
        _distance = distance;
    }

    public bool Invoke(Unit u)
    {
        if (_unit.GetDistance2d(u) < _distance &&
            (u.Entry == CreatureIds.APEXIS_FLAYER || u.Entry == CreatureIds.SHARD_HIDE_BOAR || u.Entry == CreatureIds.AETHER_RAY) &&
            !u.HasAura(GenericSpellIds.CORRUPTING_PLAGUE))
            return true;

        return false;
    }
}