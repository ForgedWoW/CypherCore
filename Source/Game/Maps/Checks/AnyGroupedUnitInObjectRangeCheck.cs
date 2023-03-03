using System.Collections.Generic;
using Game.Entities;

namespace Game.Maps;

public class AnyGroupedUnitInObjectRangeCheck : ICheck<Unit>
{
    public AnyGroupedUnitInObjectRangeCheck(WorldObject obj, Unit funit, float range, bool raid, bool playerOnly = false, bool incOwnRadius = true, bool incTargetRadius = true)
    {
        _source = obj;
        _refUnit = funit;
        _range = range;
        _raid = raid;
        _playerOnly = playerOnly;
        i_incOwnRadius = incOwnRadius;
        i_incTargetRadius = incTargetRadius;
    }

    public bool Invoke(Unit u)
    {
        if (_playerOnly && !u.IsPlayer())
            return false;

        if (_raid)
        {
            if (!_refUnit.IsInRaidWith(u))
                return false;
        }
        else if (!_refUnit.IsInPartyWith(u))
            return false;

        if (_refUnit.IsHostileTo(u))
            return false;

        if (!u.IsAlive())
            return false;

        float searchRadius = _range;
        if (i_incOwnRadius)
            searchRadius += _source.GetCombatReach();
        if (i_incTargetRadius)
            searchRadius += u.GetCombatReach();

        return u.IsInMap(_source) && u.InSamePhase(_source) && u.IsWithinDoubleVerticalCylinder(_source, searchRadius, searchRadius);
    }

    readonly WorldObject _source;
    readonly Unit _refUnit;
    readonly float _range;
    readonly bool _raid;
    readonly bool _playerOnly;
    readonly bool i_incOwnRadius;
    readonly bool i_incTargetRadius;
}