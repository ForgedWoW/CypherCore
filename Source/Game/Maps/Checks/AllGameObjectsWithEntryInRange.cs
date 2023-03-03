using System.Collections.Generic;
using Game.Entities;

namespace Game.Maps;

class AllGameObjectsWithEntryInRange : ICheck<GameObject>
{
    public AllGameObjectsWithEntryInRange(WorldObject obj, uint entry, float maxRange)
    {
        m_pObject = obj;
        m_uiEntry = entry;
        m_fRange = maxRange;
    }

    public bool Invoke(GameObject go)
    {
        if (m_uiEntry == 0 || go.GetEntry() == m_uiEntry && m_pObject.IsWithinDist(go, m_fRange, false))
            return true;

        return false;
    }

    WorldObject m_pObject;
    uint m_uiEntry;
    float m_fRange;
}