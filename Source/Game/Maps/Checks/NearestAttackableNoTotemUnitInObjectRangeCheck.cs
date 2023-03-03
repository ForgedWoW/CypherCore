using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;

namespace Game.Maps;

public class NearestAttackableNoTotemUnitInObjectRangeCheck : ICheck<Unit>
{
    public NearestAttackableNoTotemUnitInObjectRangeCheck(WorldObject obj, float range)
    {
        i_obj = obj;
        i_range = range;
    }

    public bool Invoke(Unit u)
    {
        if (!u.IsAlive())
            return false;

        if (u.GetCreatureType() == CreatureType.NonCombatPet)
            return false;

        if (u.IsTypeId(TypeId.Unit) && u.IsTotem())
            return false;

        if (!u.IsTargetableForAttack(false))
            return false;

        if (!i_obj.IsWithinDist(u, i_range) || i_obj.IsValidAttackTarget(u))
            return false;

        i_range = i_obj.GetDistance(u);
        return true;
    }

    readonly WorldObject i_obj;
    float i_range;
}