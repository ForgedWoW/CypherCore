using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Spells;

namespace Game.Maps;

public class AnyAoETargetUnitInObjectRangeCheck : ICheck<Unit>
{
    public AnyAoETargetUnitInObjectRangeCheck(WorldObject obj, Unit funit, float range, SpellInfo spellInfo = null, bool incOwnRadius = true, bool incTargetRadius = true)
    {
        i_obj = obj;
        i_funit = funit;
        _spellInfo = spellInfo;
        i_range = range;
        i_incOwnRadius = incOwnRadius;
        i_incTargetRadius = incTargetRadius;
    }

    public bool Invoke(Unit u)
    {
        // Check contains checks for: live, uninteractible, non-attackable flags, flight check and GM check, ignore totems
        if (u.IsTypeId(TypeId.Unit) && u.IsTotem())
            return false;

        if (_spellInfo != null)
        {
            if (!u.IsPlayer())
            {
                if (_spellInfo.HasAttribute(SpellAttr3.OnlyOnPlayer))
                    return false;

                if (_spellInfo.HasAttribute(SpellAttr5.NotOnPlayerControlledNpc) && u.IsControlledByPlayer())
                    return false;
            }
            else if (_spellInfo.HasAttribute(SpellAttr5.NotOnPlayer))
                return false;
        }

        if (!i_funit.IsValidAttackTarget(u, _spellInfo))
            return false;

        float searchRadius = i_range;
        if (i_incOwnRadius)
            searchRadius += i_obj.GetCombatReach();
        if (i_incTargetRadius)
            searchRadius += u.GetCombatReach();

        return u.IsInMap(i_obj) && u.InSamePhase(i_obj) && u.IsWithinDoubleVerticalCylinder(i_obj, searchRadius, searchRadius);
    }

    WorldObject i_obj;
    Unit i_funit;
    SpellInfo _spellInfo;
    float i_range;
    bool i_incOwnRadius;
    bool i_incTargetRadius;
}