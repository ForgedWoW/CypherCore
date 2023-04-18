// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Conditions;
using Forged.MapServer.Entities.Objects;
using Framework.Constants;

namespace Forged.MapServer.Spells;

public class WorldObjectSpellTrajTargetCheck : WorldObjectSpellTargetCheck
{
    private readonly Position _position;
    private readonly float _range;

    public WorldObjectSpellTrajTargetCheck(float range, Position position, WorldObject caster, SpellInfo spellInfo, SpellTargetCheckTypes selectionType, List<Condition> condList, SpellTargetObjectTypes objectType)
        : base(caster, caster, spellInfo, selectionType, condList, objectType)
    {
        _range = range;
        _position = position;
    }

    public override bool Invoke(WorldObject target)
    {
        // return all targets on missile trajectory (0 - size of a missile)
        if (!Caster.Location.HasInLine(target.Location, target.CombatReach, SpellConst.TrajectoryMissileSize))
            return false;

        return !(target.Location.GetExactDist2d(_position) > _range) && base.Invoke(target);
    }
}