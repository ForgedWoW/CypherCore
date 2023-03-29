// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using Forged.MapServer.Conditions;
using Forged.MapServer.Entities.Objects;
using Framework.Constants;

namespace Forged.MapServer.Spells;

public class WorldObjectSpellAreaTargetCheck : WorldObjectSpellTargetCheck
{
    private readonly float _range;
    private readonly Position _position;

    public WorldObjectSpellAreaTargetCheck(float range, Position position, WorldObject caster, WorldObject referer, SpellInfo spellInfo, SpellTargetCheckTypes selectionType, List<Condition> condList, SpellTargetObjectTypes objectType)
        : base(caster, referer, spellInfo, selectionType, condList, objectType)
    {
        _range = range;
        _position = position;
    }

    public override bool Invoke(WorldObject target)
    {
        if (target.AsGameObject)
        {
            // isInRange including the dimension of the GO
            var isInRange = target.AsGameObject.IsInRange(_position.X, _position.Y, _position.Z, _range);

            if (!isInRange)
                return false;
        }
        else
        {
            var isInsideCylinder = target.Location.IsWithinDist2d(_position, _range) && Math.Abs(target.Location.Z - _position.Z) <= _range;

            if (!isInsideCylinder)
                return false;
        }

        return base.Invoke(target);
    }
}