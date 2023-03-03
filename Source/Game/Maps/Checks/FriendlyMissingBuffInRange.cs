using System.Collections.Generic;
using Game.Entities;

namespace Game.Maps;

public class FriendlyMissingBuffInRange : ICheck<Creature>
{
    public FriendlyMissingBuffInRange(Unit obj, float range, uint spellid)
    {
        i_obj = obj;
        i_range = range;
        i_spell = spellid;
    }

    public bool Invoke(Creature u)
    {
        if (u.IsAlive() && u.IsInCombat() && !i_obj.IsHostileTo(u) && i_obj.IsWithinDist(u, i_range) &&
            !(u.HasAura(i_spell)))
        {
            return true;
        }
        return false;
    }

    readonly Unit i_obj;
    readonly float i_range;
    readonly uint i_spell;
}