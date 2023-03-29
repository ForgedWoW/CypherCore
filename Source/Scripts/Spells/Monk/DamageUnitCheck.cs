// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Game.Entities;

namespace Scripts.Spells.Monk;

public class DamageUnitCheck : ICheck<WorldObject>
{
    private readonly Unit m_source;
    private float m_range;

    public DamageUnitCheck(Unit source, float range)
    {
        m_source = source;
        m_range = range;
    }

    public bool Invoke(WorldObject @object)
    {
        var unit = @object.AsUnit;

        if (unit == null)
            return true;

        if (m_source.IsValidAttackTarget(unit) && unit.IsTargetableForAttack() && m_source.IsWithinDistInMap(unit, m_range))
        {
            m_range = m_source.GetDistance(unit);

            return false;
        }

        return true;
    }
}