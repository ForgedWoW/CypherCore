using System.Collections.Generic;
using Game.Entities;

namespace Game.Maps;

public class FriendlyBelowHpPctEntryInRange : ICheck<Unit>
{
    public FriendlyBelowHpPctEntryInRange(Unit obj, uint entry, float range, byte pct, bool excludeSelf)
    {
        i_obj = obj;
        i_entry = entry;
        i_range = range;
        i_pct = pct;
        i_excludeSelf = excludeSelf;
    }

    public bool Invoke(Unit u)
    {
        if (i_excludeSelf && i_obj.GetGUID() == u.GetGUID())
            return false;
        if (u.GetEntry() == i_entry && u.IsAlive() && u.IsInCombat() && !i_obj.IsHostileTo(u) && i_obj.IsWithinDist(u, i_range) && u.HealthBelowPct(i_pct))
            return true;
        return false;
    }

    readonly Unit i_obj;
    readonly uint i_entry;
    readonly float i_range;
    readonly byte i_pct;
    readonly bool i_excludeSelf;
}