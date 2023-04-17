// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Entities.Creatures;
using Forged.MapServer.Entities.Units;

namespace Forged.MapServer.Maps.GridNotifiers;

internal class NearestHostileUnitInAggroRangeCheck : ICheck<Unit>
{
    private readonly bool _ignoreCivilians;
    private readonly Creature _me;
    private readonly bool _useLOS;

    public NearestHostileUnitInAggroRangeCheck(Creature creature, bool useLOS = false, bool ignoreCivilians = false)
    {
        _me = creature;
        _useLOS = useLOS;
        _ignoreCivilians = ignoreCivilians;
    }

    public bool Invoke(Unit u)
    {
        if (!u.WorldObjectCombat.IsHostileTo(_me))
            return false;

        if (!u.Location.IsWithinDist(_me, _me.GetAggroRange(u)))
            return false;

        if (!_me.WorldObjectCombat.IsValidAttackTarget(u))
            return false;

        if (_useLOS && !u.Location.IsWithinLOSInMap(_me))
            return false;

        // pets in aggressive do not attack civilians
        if (!_ignoreCivilians)
            return true;

        var c = u.AsCreature;

        return c is not { IsCivilian: true };
    }
}