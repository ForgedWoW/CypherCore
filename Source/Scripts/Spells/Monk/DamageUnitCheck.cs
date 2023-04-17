// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Entities.Objects;
using Forged.MapServer.Entities.Units;

namespace Scripts.Spells.Monk;

public class DamageUnitCheck : ICheck<WorldObject>
{
    private readonly Unit _mSource;
    private float _mRange;

    public DamageUnitCheck(Unit source, float range)
    {
        _mSource = source;
        _mRange = range;
    }

    public bool Invoke(WorldObject @object)
    {
        var unit = @object.AsUnit;

        if (unit == null)
            return true;

        if (_mSource.IsValidAttackTarget(unit) && unit.IsTargetableForAttack() && _mSource.IsWithinDistInMap(unit, _mRange))
        {
            _mRange = _mSource.GetDistance(unit);

            return false;
        }

        return true;
    }
}