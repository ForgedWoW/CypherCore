// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Entities.Objects;
using Forged.MapServer.Entities.Units;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces;
using Forged.MapServer.Scripting.Interfaces.ISpell;
using Framework.Constants;

namespace Scripts.Spells.DemonHunter;

[SpellScript(198030)]
public class SpellDemonHunterEyeBeamDamage : SpellScript, IHasSpellEffects
{
    public List<ISpellEffect> SpellEffects { get; } = new();

    public override void Register()
    {
        SpellEffects.Add(new ObjectAreaTargetSelectHandler(FilterTargets, 0, Targets.UnitRectCasterEnemy));
    }

    private void FilterTargets(List<WorldObject> unitList)
    {
        var caster = Caster;

        if (caster == null)
            return;

        unitList.Clear();
        var units = new List<Unit>();
        caster.GetAttackableUnitListInRange(units, 25.0f);


        units.RemoveIf((Unit unit) => { return !caster.Location.HasInLine(unit.Location, 5.0f, caster.ObjectScale); });

        foreach (var unit in units)
            unitList.Add(unit);
    }
}