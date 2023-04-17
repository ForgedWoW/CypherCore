// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Entities.Objects;
using Forged.MapServer.Entities.Units;

namespace Forged.MapServer.Maps.Checks;

public class NearestAttackableUnitInObjectRangeCheck : ICheck<Unit>
{
    private readonly Unit _funit;
    private readonly WorldObject _obj;
    private float _range;

    public NearestAttackableUnitInObjectRangeCheck(WorldObject obj, Unit funit, float range)
    {
        _obj = obj;
        _funit = funit;
        _range = range;
    }

    public bool Invoke(Unit u)
    {
        if (!u.IsTargetableForAttack() ||
            !_obj.Location.IsWithinDist(u, _range) ||
            (!_funit.IsInCombatWith(u) && !_funit.WorldObjectCombat.IsHostileTo(u)) ||
            !_obj.Visibility.CanSeeOrDetect(u))
            return false;

        _range = _obj.Location.GetDistance(u); // use found unit range as new range limit for next check

        return true;
    }
}