using System.Collections.Generic;
using Game.Entities;

namespace Game.Maps;

public class AllCreaturesOfEntryInRange : ICheck<Creature>
{
    public AllCreaturesOfEntryInRange(WorldObject obj, uint entry, float maxRange = 0f)
    {
        m_pObject = obj;
        m_uiEntry = entry;
        m_fRange = maxRange;
    }

    public bool Invoke(Creature creature)
    {
        if (m_uiEntry != 0)
        {
            if (creature.GetEntry() != m_uiEntry)
                return false;
        }

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
    readonly uint m_uiEntry;
    readonly float m_fRange;
}