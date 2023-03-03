using System.Collections.Generic;
using Game.Entities;

namespace Game.Maps;

class AnyAssistCreatureInRangeCheck : ICheck<Creature>
{
    public AnyAssistCreatureInRangeCheck(Unit funit, Unit enemy, float range)
    {
        i_funit = funit;
        i_enemy = enemy;
        i_range = range;

    }

    public bool Invoke(Creature u)
    {
        if (u == i_funit)
            return false;

        if (!u.CanAssistTo(i_funit, i_enemy))
            return false;

        // too far
        // Don't use combat reach distance, range must be an absolute value, otherwise the chain aggro range will be too big
        if (!i_funit.IsWithinDist(u, i_range, true, false, false))
            return false;

        // only if see assisted creature
        if (!i_funit.IsWithinLOSInMap(u))
            return false;

        return true;
    }

    Unit i_funit;
    Unit i_enemy;
    float i_range;
}