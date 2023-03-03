using System.Collections.Generic;
using Game.Entities;

namespace Game.Maps;

class NearestHostileUnitInAggroRangeCheck : ICheck<Unit>
{
    public NearestHostileUnitInAggroRangeCheck(Creature creature, bool useLOS = false, bool ignoreCivilians = false)
    {
        _me = creature;
        _useLOS = useLOS;
        _ignoreCivilians = ignoreCivilians;
    }

    public bool Invoke(Unit u)
    {
        if (!u.IsHostileTo(_me))
            return false;

        if (!u.IsWithinDist(_me, _me.GetAggroRange(u)))
            return false;

        if (!_me.IsValidAttackTarget(u))
            return false;

        if (_useLOS && !u.IsWithinLOSInMap(_me))
            return false;

        // pets in aggressive do not attack civilians
        if (_ignoreCivilians)
        {
            Creature c = u.ToCreature();
            if (c != null)
                if (c.IsCivilian())
                    return false;
        }

        return true;
    }

    Creature _me;
    bool _useLOS;
    bool _ignoreCivilians;
}