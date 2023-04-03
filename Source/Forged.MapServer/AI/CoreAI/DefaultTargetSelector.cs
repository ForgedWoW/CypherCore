// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Entities.Units;
using Framework.Constants;

namespace Forged.MapServer.AI.CoreAI;

// default predicate function to select target based on distance, player and/or aura criteria
public class DefaultTargetSelector : ICheck<Unit>
{
    private readonly int _aura;
    private readonly float _dist;
    private readonly Unit _exception;
    private readonly Unit _me;
    private readonly bool _playerOnly;

    /// <param name="unit"> the reference unit </param>
    /// <param name="dist"> if 0: ignored, if > 0: maximum distance to the reference unit, if < 0: minimum distance to the reference unit </param>
    /// <param name="playerOnly"> self explaining </param>
    /// <param name="withTank"> allow current tank to be selected </param>
    /// <param name="aura"> if 0: ignored, if > 0: the target shall have the aura, if < 0, the target shall NOT have the aura </param>
    public DefaultTargetSelector(Unit unit, float dist, bool playerOnly, bool withTank, int aura)
    {
        _me = unit;
        _dist = dist;
        _playerOnly = playerOnly;
        _exception = !withTank ? unit.GetThreatManager().LastVictim : null;
        _aura = aura;
    }

    public bool Invoke(Unit target)
    {
        if (_me == null)
            return false;

        if (target == null)
            return false;

        if (_exception != null && target == _exception)
            return false;

        if (_playerOnly && !target.IsTypeId(TypeId.Player))
            return false;

        switch (_dist)
        {
            case > 0.0f when !_me.IsWithinCombatRange(target, _dist):
            case < 0.0f when _me.IsWithinCombatRange(target, -_dist):
                return false;
        }

        switch (_aura)
        {
            case 0:
                return false;

            case > 0:
            {
                if (!target.HasAura((uint)_aura))
                    return false;

                break;
            }
            default:
            {
                if (target.HasAura((uint)-_aura))
                    return false;

                break;
            }
        }

        return false;
    }
}