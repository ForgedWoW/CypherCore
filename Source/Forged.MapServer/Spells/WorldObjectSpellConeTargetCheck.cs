// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using Forged.MapServer.Conditions;
using Forged.MapServer.Entities.Objects;
using Framework.Constants;

namespace Forged.MapServer.Spells;

public class WorldObjectSpellConeTargetCheck : WorldObjectSpellAreaTargetCheck
{
    private readonly Position _coneSrc;
    private readonly float _coneAngle;
    private readonly float _lineWidth;

    public WorldObjectSpellConeTargetCheck(Position coneSrc, float coneAngle, float lineWidth, float range, WorldObject caster, SpellInfo spellInfo, SpellTargetCheckTypes selectionType, List<Condition> condList, SpellTargetObjectTypes objectType)
        : base(range, caster.Location, caster, caster, spellInfo, selectionType, condList, objectType)
    {
        _coneSrc = coneSrc;
        _coneAngle = coneAngle;
        _lineWidth = lineWidth;
    }

    public override bool Invoke(WorldObject target)
    {
        if (SpellInfo.HasAttribute(SpellCustomAttributes.ConeBack))
        {
            if (_coneSrc.HasInArc(-Math.Abs(_coneAngle), target.Location))
                return false;
        }
        else if (SpellInfo.HasAttribute(SpellCustomAttributes.ConeLine))
        {
            if (!_coneSrc.HasInLine(target.Location, target.CombatReach, _lineWidth))
                return false;
        }
        else
        {
            if (!Caster.IsUnit || !Caster.AsUnit.IsWithinBoundaryRadius(target.AsUnit))
                // ConeAngle > 0 . select targets in front
                // ConeAngle < 0 . select targets in back
                if (_coneSrc.HasInArc(_coneAngle, target.Location) != MathFunctions.fuzzyGe(_coneAngle, 0.0f))
                    return false;
        }

        return base.Invoke(target);
    }
}