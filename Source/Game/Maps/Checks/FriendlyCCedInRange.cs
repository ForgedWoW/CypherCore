using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;

namespace Game.Maps;

public class FriendlyCCedInRange : ICheck<Creature>
{
    public FriendlyCCedInRange(Unit obj, float range)
    {
        i_obj = obj;
        i_range = range;
    }

    public bool Invoke(Creature u)
    {
        if (u.IsAlive() && u.IsInCombat() && !i_obj.IsHostileTo(u) && i_obj.IsWithinDist(u, i_range) &&
            (u.IsFeared() || u.IsCharmed() || u.HasRootAura() || u.HasUnitState(UnitState.Stunned) || u.HasUnitState(UnitState.Confused)))
            return true;
        return false;
    }

    Unit i_obj;
    float i_range;
}