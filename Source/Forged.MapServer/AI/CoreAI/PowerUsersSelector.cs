// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Entities.Units;
using Framework.Constants;

namespace Forged.MapServer.AI.CoreAI;

internal class PowerUsersSelector : ICheck<Unit>
{
    private readonly float _dist;
    private readonly Unit _me;
    private readonly bool _playerOnly;
    private readonly PowerType _power;

    public PowerUsersSelector(Unit unit, PowerType power, float dist, bool playerOnly)
    {
        _me = unit;
        _power = power;
        _dist = dist;
        _playerOnly = playerOnly;
    }

    public bool Invoke(Unit target)
    {
        if (_me == null || target == null)
            return false;

        if (target.DisplayPowerType != _power)
            return false;

        if (_playerOnly && target.TypeId != TypeId.Player)
            return false;

        return _dist switch
        {
            > 0.0f when !_me.IsWithinCombatRange(target, _dist) => false,
            < 0.0f when _me.IsWithinCombatRange(target, -_dist) => false,
            _                                                   => true
        };
    }
}