// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Conditions;
using Forged.MapServer.Entities.Objects;
using Framework.Constants;

namespace Forged.MapServer.Spells;

public class WorldObjectSpellNearbyTargetCheck : WorldObjectSpellTargetCheck
{
    private readonly Position _position;
    private float _range;

    public WorldObjectSpellNearbyTargetCheck(float range, WorldObject caster, SpellInfo spellInfo, SpellTargetCheckTypes selectionType, List<Condition> condList, SpellTargetObjectTypes objectType)
        : base(caster, caster, spellInfo, selectionType, condList, objectType)
    {
        _range = range;
        _position = caster.Location;
    }

    public override bool Invoke(WorldObject target)
    {
        var dist = target.Location.GetDistance(_position);

        if (!(dist < _range) || !base.Invoke(target))
            return false;

        _range = dist;

        return true;
    }
}