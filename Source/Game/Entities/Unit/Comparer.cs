// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using Framework.Constants;

namespace Game.Entities
{
    public class PowerPctOrderPred : IComparer<WorldObject>
    {
        public PowerPctOrderPred(PowerType power, bool ascending = true)
        {
            m_power = power;
            m_ascending = ascending;
        }

        public int Compare(WorldObject objA, WorldObject objB)
        {
            Unit a = objA.ToUnit();
            Unit b = objB.ToUnit();
            float rA = a != null ? a.GetPowerPct(m_power) : 0.0f;
            float rB = b != null ? b.GetPowerPct(m_power) : 0.0f;
            return Convert.ToInt32(m_ascending ? rA < rB : rA > rB);
        }

        readonly PowerType m_power;
        readonly bool m_ascending;
    }

    public class HealthPctOrderPred : IComparer<WorldObject>
    {
        public HealthPctOrderPred(bool ascending = true)
        {
            m_ascending = ascending;
        }

        public int Compare(WorldObject objA, WorldObject objB)
        {
            Unit a = objA.ToUnit();
            Unit b = objB.ToUnit();
            float rA = a.GetMaxHealth() != 0 ? a.GetHealth() / (float)a.GetMaxHealth() : 0.0f;
            float rB = b.GetMaxHealth() != 0 ? b.GetHealth() / (float)b.GetMaxHealth() : 0.0f;
            return Convert.ToInt32(m_ascending ? rA < rB : rA > rB);
        }

        readonly bool m_ascending;
    }

    public class ObjectDistanceOrderPred : IComparer<WorldObject>
    {
        public ObjectDistanceOrderPred(WorldObject pRefObj, bool ascending = true)
        {
            m_refObj = pRefObj;
            m_ascending = ascending;
        }

        public int Compare(WorldObject pLeft, WorldObject pRight)
        {
            return (m_ascending ? m_refObj.GetDistanceOrder(pLeft, pRight) : !m_refObj.GetDistanceOrder(pLeft, pRight)) ? 1 : 0;
        }

        readonly WorldObject m_refObj;
        readonly bool m_ascending;
    }
}
