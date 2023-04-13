// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Entities.Creatures;
using Forged.MapServer.Entities.Units;

namespace Forged.MapServer.Maps.Checks;

public class FriendlyMissingBuffInRange : ICheck<Creature>
{
    private readonly Unit _obj;
    private readonly float _range;
    private readonly uint _spell;

    public FriendlyMissingBuffInRange(Unit obj, float range, uint spellid)
    {
        _obj = obj;
        _range = range;
        _spell = spellid;
    }

    public bool Invoke(Creature u)
    {
        if (u.IsAlive &&
            u.IsInCombat &&
            !_obj.WorldObjectCombat.IsHostileTo(u) &&
            _obj.Location.IsWithinDist(u, _range) &&
            !u.HasAura(_spell))
            return true;

        return false;
    }
}