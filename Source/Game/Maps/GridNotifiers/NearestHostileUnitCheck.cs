using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;

namespace Game.Maps;

public class NearestHostileUnitCheck : ICheck<Unit>
{
    public NearestHostileUnitCheck(Creature creature, float dist = 0, bool playerOnly = false)
    {
        me = creature;
        i_playerOnly = playerOnly;

        m_range = (dist == 0 ? 9999 : dist);
    }

    public bool Invoke(Unit u)
    {
        if (!me.IsWithinDist(u, m_range))
            return false;

        if (!me.IsValidAttackTarget(u))
            return false;

        if (i_playerOnly && !u.IsTypeId(TypeId.Player))
            return false;

        m_range = me.GetDistance(u);   // use found unit range as new range limit for next check
        return true;
    }

    Creature me;
    float m_range;
    bool i_playerOnly;
}