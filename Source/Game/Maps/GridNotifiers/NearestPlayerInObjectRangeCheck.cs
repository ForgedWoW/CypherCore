using System.Collections.Generic;
using Game.Entities;

namespace Game.Maps;

class NearestPlayerInObjectRangeCheck : ICheck<Player>
{
    public NearestPlayerInObjectRangeCheck(WorldObject obj, float range)
    {
        i_obj = obj;
        i_range = range;

    }

    public bool Invoke(Player pl)
    {
        if (pl.IsAlive() && i_obj.IsWithinDist(pl, i_range))
        {
            i_range = i_obj.GetDistance(pl);
            return true;
        }

        return false;
    }

    readonly WorldObject i_obj;
    float i_range;
}