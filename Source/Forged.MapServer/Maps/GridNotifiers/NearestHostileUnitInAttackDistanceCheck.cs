// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Entities.Creatures;
using Forged.MapServer.Entities.Units;

namespace Forged.MapServer.Maps.GridNotifiers;

internal class NearestHostileUnitInAttackDistanceCheck : ICheck<Unit>
{
    private readonly bool _force;
    private readonly Creature _me;
    private float _range;

    public NearestHostileUnitInAttackDistanceCheck(Creature creature, float dist = 0)
    {
        _me = creature;
        _range = dist == 0 ? 9999 : dist;
        _force = dist != 0;
    }

    public bool Invoke(Unit u)
    {
        if (!_me.Location.IsWithinDist(u, _range))
            return false;

        if (!_me.Visibility.CanSeeOrDetect(u))
            return false;

        if (_force)
        {
            if (!_me.WorldObjectCombat.IsValidAttackTarget(u))
                return false;
        }
        else if (!_me.CanStartAttack(u, false))
        {
            return false;
        }

        _range = _me.Location.GetDistance(u); // use found unit range as new range limit for next check

        return true;
    }
}