﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Entities.Objects;
using Forged.MapServer.Maps.Checks;
using Forged.MapServer.Maps.GridNotifiers;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces;
using Forged.MapServer.Scripting.Interfaces.ISpell;
using Framework.Constants;

namespace Scripts.Spells.Warlock;

// Eye Laser - 205231
[SpellScript(205231)]
public class SpellWarlEyeLaser : SpellScript, IHasSpellEffects
{
    public List<ISpellEffect> SpellEffects { get; } = new();

    public void HandleTargets(List<WorldObject> targets)
    {
        var caster = OriginalCaster;

        if (caster == null)
            return;

        var check = new AllWorldObjectsInRange(caster, 100.0f);
        var search = new WorldObjectListSearcher(caster, targets, check);
        Cell.VisitGrid(caster, search, 100.0f);
        targets.RemoveAll(new UnitAuraCheck<WorldObject>(false, WarlockSpells.DOOM, caster.GUID));
    }

    public override void Register()
    {
        SpellEffects.Add(new ObjectAreaTargetSelectHandler(HandleTargets, 0, Targets.UnitTargetEnemy));
    }
}