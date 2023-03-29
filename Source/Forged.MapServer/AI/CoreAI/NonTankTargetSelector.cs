// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Entities.Units;
using Framework.Constants;

namespace Forged.MapServer.AI.CoreAI;

public class NonTankTargetSelector : ICheck<Unit>
{
    private readonly Unit _source;
    private readonly bool _playerOnly;

    public NonTankTargetSelector(Unit source, bool playerOnly = true)
    {
        _source = source;
        _playerOnly = playerOnly;
    }

    public bool Invoke(Unit target)
    {
        if (target == null)
            return false;

        if (_playerOnly && !target.IsTypeId(TypeId.Player))
            return false;

        var currentVictim = _source.GetThreatManager().CurrentVictim;

        if (currentVictim != null)
            return target != currentVictim;

        return target != _source.Victim;
    }
}