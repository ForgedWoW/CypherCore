using System.Collections.Generic;
using Game.Entities;

namespace Game.Maps;

public class AllCreaturesWithinRange : ICheck<Creature>
{
    public AllCreaturesWithinRange(WorldObject obj, float maxRange = 0f)
    {
        m_pObject = obj;
        m_fRange = maxRange;
    }

    public bool Invoke(Creature creature)
    {
        if (m_fRange != 0f)
        {
            if (m_fRange > 0.0f && !m_pObject.IsWithinDist(creature, m_fRange, false))
                return false;
            if (m_fRange < 0.0f && m_pObject.IsWithinDist(creature, m_fRange, false))
                return false;
        }

        return true;
    }

    readonly WorldObject m_pObject;
    readonly float m_fRange;
}