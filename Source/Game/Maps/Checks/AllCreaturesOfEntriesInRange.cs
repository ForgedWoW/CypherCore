using System.Collections.Generic;
using Game.Entities;

namespace Game.Maps;

public class AllCreaturesOfEntriesInRange : ICheck<Creature>
{
    public AllCreaturesOfEntriesInRange(WorldObject obj, uint[] entry, float maxRange = 0f)
    {
        m_pObject = obj;
        m_uiEntry = entry;
        m_fRange = maxRange;
    }

    public bool Invoke(Creature creature)
    {
        if (m_uiEntry != null)
        {
            bool match = false;

            foreach (var entry in m_uiEntry)
                if (entry != 0 && creature.GetEntry() == entry)
                    match = true;

            if (!match)
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

    WorldObject m_pObject;
    uint[] m_uiEntry;
    float m_fRange;
}