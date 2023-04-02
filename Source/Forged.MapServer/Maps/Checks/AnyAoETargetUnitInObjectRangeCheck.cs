// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Entities.Objects;
using Forged.MapServer.Entities.Units;
using Forged.MapServer.Spells;
using Framework.Constants;

namespace Forged.MapServer.Maps.Checks;

public class AnyAoETargetUnitInObjectRangeCheck : ICheck<Unit>
{
    private readonly Unit _funit;
    private readonly bool _incOwnRadius;
    private readonly bool _incTargetRadius;
    private readonly WorldObject _obj;
    private readonly float _range;
    private readonly SpellInfo _spellInfo;
    public AnyAoETargetUnitInObjectRangeCheck(WorldObject obj, Unit funit, float range, SpellInfo spellInfo = null, bool incOwnRadius = true, bool incTargetRadius = true)
    {
        _obj = obj;
        _funit = funit;
        _spellInfo = spellInfo;
        _range = range;
        _incOwnRadius = incOwnRadius;
        _incTargetRadius = incTargetRadius;
    }

    public bool Invoke(Unit u)
    {
        // Check contains checks for: live, uninteractible, non-attackable flags, flight check and GM check, ignore totems
        if (u.IsTypeId(TypeId.Unit) && u.IsTotem)
            return false;

        if (_spellInfo != null)
        {
            if (!u.IsPlayer)
            {
                if (_spellInfo.HasAttribute(SpellAttr3.OnlyOnPlayer))
                    return false;

                if (_spellInfo.HasAttribute(SpellAttr5.NotOnPlayerControlledNpc) && u.ControlledByPlayer)
                    return false;
            }
            else if (_spellInfo.HasAttribute(SpellAttr5.NotOnPlayer))
            {
                return false;
            }
        }

        if (!_funit.WorldObjectCombat.IsValidAttackTarget(u, _spellInfo))
            return false;

        var searchRadius = _range;

        if (_incOwnRadius)
            searchRadius += _obj.CombatReach;

        if (_incTargetRadius)
            searchRadius += u.CombatReach;

        return u.Location.IsInMap(_obj) && u.Location.InSamePhase(_obj) && u.Location.IsWithinDoubleVerticalCylinder(_obj.Location, searchRadius, searchRadius);
    }
}