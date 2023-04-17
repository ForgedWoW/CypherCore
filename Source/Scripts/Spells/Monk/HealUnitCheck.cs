// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Entities.Objects;
using Forged.MapServer.Entities.Units;

namespace Scripts.Spells.Monk;

public class HealUnitCheck : ICheck<WorldObject>
{
    private readonly Unit _mSource;

    public HealUnitCheck(Unit source)
    {
        _mSource = source;
    }

    public bool Invoke(WorldObject @object)
    {
        var unit = @object.AsUnit;

        if (unit == null)
            return true;

        if (_mSource.IsFriendlyTo(unit))
            return false;

        return true;
    }
}