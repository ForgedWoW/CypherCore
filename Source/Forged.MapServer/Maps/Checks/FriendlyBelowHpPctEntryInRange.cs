// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Entities.Units;

namespace Forged.MapServer.Maps.Checks;

public class FriendlyBelowHpPctEntryInRange : ICheck<Unit>
{
    private readonly uint _entry;
    private readonly bool _excludeSelf;
    private readonly Unit _obj;
    private readonly byte _pct;
    private readonly float _range;

    public FriendlyBelowHpPctEntryInRange(Unit obj, uint entry, float range, byte pct, bool excludeSelf)
    {
        _obj = obj;
        _entry = entry;
        _range = range;
        _pct = pct;
        _excludeSelf = excludeSelf;
    }

    public bool Invoke(Unit u)
    {
        if (_excludeSelf && _obj.GUID == u.GUID)
            return false;

        return u.Entry == _entry && u.IsAlive && u.IsInCombat && !_obj.WorldObjectCombat.IsHostileTo(u) && _obj.Location.IsWithinDist(u, _range) && u.HealthBelowPct(_pct);
    }
}