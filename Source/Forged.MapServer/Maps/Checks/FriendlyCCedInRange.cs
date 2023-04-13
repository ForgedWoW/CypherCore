// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Entities.Creatures;
using Forged.MapServer.Entities.Units;
using Framework.Constants;

namespace Forged.MapServer.Maps.Checks;

public class FriendlyCCedInRange : ICheck<Creature>
{
    private readonly Unit _obj;
    private readonly float _range;

    public FriendlyCCedInRange(Unit obj, float range)
    {
        _obj = obj;
        _range = range;
    }

    public bool Invoke(Creature u)
    {
        return u.IsAlive &&
               u.IsInCombat &&
               !_obj.WorldObjectCombat.IsHostileTo(u) &&
               _obj.Location.IsWithinDist(u, _range) &&
               (u.IsFeared || u.IsCharmed || u.HasRootAura || u.HasUnitState(UnitState.Stunned) || u.HasUnitState(UnitState.Confused));
    }
}