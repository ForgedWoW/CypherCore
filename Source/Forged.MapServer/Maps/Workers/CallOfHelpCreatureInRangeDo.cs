// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Entities.Creatures;
using Forged.MapServer.Entities.Units;

namespace Forged.MapServer.Maps.Workers;

public class CallOfHelpCreatureInRangeDo : IDoWork<Creature>
{
    private readonly Unit _funit;
    private readonly Unit _enemy;
    private readonly float _range;

    public CallOfHelpCreatureInRangeDo(Unit funit, Unit enemy, float range)
    {
        _funit = funit;
        _enemy = enemy;
        _range = range;
    }

    public void Invoke(Creature u)
    {
        if (u == _funit)
            return;

        if (!u.CanAssistTo(_funit, _enemy, false))
            return;

        // too far
        // Don't use combat reach distance, range must be an absolute value, otherwise the chain aggro range will be too big
        if (!u.IsWithinDist(_funit, _range, true, false, false))
            return;

        // only if see assisted creature's enemy
        if (!u.IsWithinLOSInMap(_enemy))
            return;

        u.EngageWithTarget(_enemy);
    }
}