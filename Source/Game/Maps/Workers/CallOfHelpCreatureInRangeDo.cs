using System.Collections.Generic;
using Game.Entities;

namespace Game.Maps;

public class CallOfHelpCreatureInRangeDo : IDoWork<Creature>
{
    public CallOfHelpCreatureInRangeDo(Unit funit, Unit enemy, float range)
    {
        i_funit = funit;
        i_enemy = enemy;
        i_range = range;
    }

    public void Invoke(Creature u)
    {
        if (u == i_funit)
            return;

        if (!u.CanAssistTo(i_funit, i_enemy, false))
            return;

        // too far
        // Don't use combat reach distance, range must be an absolute value, otherwise the chain aggro range will be too big
        if (!u.IsWithinDist(i_funit, i_range, true, false, false))
            return;

        // only if see assisted creature's enemy
        if (!u.IsWithinLOSInMap(i_enemy))
            return;

        u.EngageWithTarget(i_enemy);
    }

    Unit i_funit;
    Unit i_enemy;
    float i_range;
}