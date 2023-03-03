using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;

namespace Game.Maps;

class NearestCreatureEntryWithLiveStateInObjectRangeCheck : ICheck<Creature>
{
    public NearestCreatureEntryWithLiveStateInObjectRangeCheck(WorldObject obj, uint entry, bool alive, float range)
    {
        i_obj = obj;
        i_entry = entry;
        i_alive = alive;
        i_range = range;
    }

    public bool Invoke(Creature u)
    {
        if (u.GetDeathState() != DeathState.Dead && u.GetEntry() == i_entry && u.IsAlive() == i_alive && u.GetGUID() != i_obj.GetGUID() && i_obj.IsWithinDist(u, i_range) && u.CheckPrivateObjectOwnerVisibility(i_obj))
        {
            i_range = i_obj.GetDistance(u);         // use found unit range as new range limit for next check
            return true;
        }
        return false;
    }

    readonly WorldObject i_obj;
    readonly uint i_entry;
    readonly bool i_alive;
    float i_range;
}