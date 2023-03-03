using System.Collections.Generic;
using Game.Entities;

namespace Game.Maps;

public class GetAllAlliesOfTargetCreaturesWithinRange : ICheck<Creature>
{
    public GetAllAlliesOfTargetCreaturesWithinRange(Unit obj, float maxRange = 0f)
    {
        m_pObject = obj;
        m_fRange = maxRange;
    }

    public bool Invoke(Creature creature)
    {
        if (creature.IsHostileTo(m_pObject))
            return false;

        if (m_fRange != 0f)
        {
            if (m_fRange > 0.0f && !m_pObject.IsWithinDist(creature, m_fRange, false))
                return false;
            if (m_fRange < 0.0f && m_pObject.IsWithinDist(creature, m_fRange, false))
                return false;
        }

        return true;
    }

    readonly Unit m_pObject;
    readonly float m_fRange;
}