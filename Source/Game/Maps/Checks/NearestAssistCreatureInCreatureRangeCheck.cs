using System.Collections.Generic;
using Game.Entities;

namespace Game.Maps;

class NearestAssistCreatureInCreatureRangeCheck : ICheck<Creature>
{
    public NearestAssistCreatureInCreatureRangeCheck(Creature obj, Unit enemy, float range)
    {
        i_obj = obj;
        i_enemy = enemy;
        i_range = range;
    }

    public bool Invoke(Creature u)
    {
        if (u == i_obj)
            return false;

        if (!u.CanAssistTo(i_obj, i_enemy))
            return false;

        // Don't use combat reach distance, range must be an absolute value, otherwise the chain aggro range will be too big
        if (!i_obj.IsWithinDist(u, i_range, true, false, false))
            return false;

        if (!i_obj.IsWithinLOSInMap(u))
            return false;

        i_range = i_obj.GetDistance(u);            // use found unit range as new range limit for next check
        return true;
    }

    Creature i_obj;
    Unit i_enemy;
    float i_range;
}