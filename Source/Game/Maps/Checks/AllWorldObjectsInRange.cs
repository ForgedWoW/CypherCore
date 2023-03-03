using System.Collections.Generic;
using Game.Entities;

namespace Game.Maps;

public class AllWorldObjectsInRange : ICheck<WorldObject>
{
    public AllWorldObjectsInRange(WorldObject obj, float maxRange)
    {
        m_pObject = obj;
        m_fRange = maxRange;
    }

    public bool Invoke(WorldObject go)
    {
        return m_pObject.IsWithinDist(go, m_fRange, false) && m_pObject.InSamePhase(go);
    }

    WorldObject m_pObject;
    float m_fRange;
}