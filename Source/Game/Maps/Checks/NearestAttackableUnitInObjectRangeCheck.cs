using System.Collections.Generic;
using Game.Entities;

namespace Game.Maps;

public class NearestAttackableUnitInObjectRangeCheck : ICheck<Unit>
{
    public NearestAttackableUnitInObjectRangeCheck(WorldObject obj, Unit funit, float range)
    {
        i_obj = obj;
        i_funit = funit;
        i_range = range;
    }

    public bool Invoke(Unit u)
    {
        if (u.IsTargetableForAttack() && i_obj.IsWithinDist(u, i_range) &&
            (i_funit.IsInCombatWith(u) || i_funit.IsHostileTo(u)) && i_obj.CanSeeOrDetect(u))
        {
            i_range = i_obj.GetDistance(u);        // use found unit range as new range limit for next check
            return true;
        }

        return false;
    }

    WorldObject i_obj;
    Unit i_funit;
    float i_range;
}