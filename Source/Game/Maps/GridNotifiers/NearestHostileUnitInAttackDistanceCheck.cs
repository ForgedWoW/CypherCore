using System.Collections.Generic;
using Game.Entities;

namespace Game.Maps;

class NearestHostileUnitInAttackDistanceCheck : ICheck<Unit>
{
    public NearestHostileUnitInAttackDistanceCheck(Creature creature, float dist = 0)
    {
        me = creature;
        m_range = (dist == 0 ? 9999 : dist);
        m_force = (dist != 0);
    }

    public bool Invoke(Unit u)
    {
        if (!me.IsWithinDist(u, m_range))
            return false;

        if (!me.CanSeeOrDetect(u))
            return false;

        if (m_force)
        {
            if (!me.IsValidAttackTarget(u))
                return false;
        }
        else if (!me.CanStartAttack(u, false))
            return false;

        m_range = me.GetDistance(u);   // use found unit range as new range limit for next check
        return true;
    }

    readonly Creature me;
    float m_range;
    readonly bool m_force;
}