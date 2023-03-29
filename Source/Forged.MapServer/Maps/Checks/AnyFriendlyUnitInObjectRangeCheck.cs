// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Entities.Objects;
using Forged.MapServer.Entities.Units;
using Framework.Constants;

namespace Forged.MapServer.Maps.Checks;

public class AnyFriendlyUnitInObjectRangeCheck : ICheck<Unit>
{
    private readonly WorldObject _obj;
    private readonly Unit _funit;
    private readonly float _range;
    private readonly bool _playerOnly;
    private readonly bool _incOwnRadius;
    private readonly bool _incTargetRadius;

    public AnyFriendlyUnitInObjectRangeCheck(WorldObject obj, Unit funit, float range, bool playerOnly = false, bool incOwnRadius = true, bool incTargetRadius = true)
    {
        _obj = obj;
        _funit = funit;
        _range = range;
        _playerOnly = playerOnly;
        _incOwnRadius = incOwnRadius;
        _incTargetRadius = incTargetRadius;
    }

    public bool Invoke(Unit u)
    {
        if (!u.IsAlive)
            return false;

        var searchRadius = _range;

        if (_incOwnRadius)
            searchRadius += _obj.CombatReach;

        if (_incTargetRadius)
            searchRadius += u.CombatReach;

        if (!u.IsInMap(_obj) || !u.InSamePhase(_obj) || !u.Location.IsWithinDoubleVerticalCylinder(_obj.Location, searchRadius, searchRadius))
            return false;

        if (!_funit.IsFriendlyTo(u))
            return false;

        return !_playerOnly || u.TypeId == TypeId.Player;
    }
}