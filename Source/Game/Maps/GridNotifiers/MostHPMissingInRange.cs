using System.Collections.Generic;
using Game.Entities;

namespace Game.Maps;

public class MostHPMissingInRange<T> : ICheck<T> where T : Unit
{
    public MostHPMissingInRange(Unit obj, float range, uint hp)
    {
        i_obj = obj;
        i_range = range;
        i_hp = hp;
    }

    public bool Invoke(T u)
    {
        if (u.IsAlive() && u.IsInCombat() && !i_obj.IsHostileTo(u) && i_obj.IsWithinDist(u, i_range) && u.GetMaxHealth() - u.GetHealth() > i_hp)
        {
            i_hp = (uint)(u.GetMaxHealth() - u.GetHealth());
            return true;
        }
        return false;
    }

    Unit i_obj;
    float i_range;
    long i_hp;
}