// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Entities.Objects;
using Forged.MapServer.Entities.Units;
using Framework.Constants;

namespace Forged.MapServer.Maps.Checks;

public class NearestAttackableNoTotemUnitInObjectRangeCheck : ICheck<Unit>
{
    private readonly WorldObject _obj;
    private float _range;

    public NearestAttackableNoTotemUnitInObjectRangeCheck(WorldObject obj, float range)
    {
        _obj = obj;
        _range = range;
    }

    public bool Invoke(Unit u)
    {
        if (!u.IsAlive)
            return false;

        if (u.CreatureType == CreatureType.NonCombatPet)
            return false;

        if (u.IsTypeId(TypeId.Unit) && u.IsTotem)
            return false;

        if (!u.IsTargetableForAttack(false))
            return false;

        if (!_obj.IsWithinDist(u, _range) || _obj.IsValidAttackTarget(u))
            return false;

        _range = _obj.GetDistance(u);

        return true;
    }
}