// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Entities.Objects;
using Forged.MapServer.Entities.Units;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces;
using Forged.MapServer.Scripting.Interfaces.ISpell;
using Framework.Constants;

namespace Scripts.Spells.Monk;

[SpellScript(191840)]
public class SpellMonkEssenceFontHeal : SpellScript, IHasSpellEffects
{
    public List<ISpellEffect> SpellEffects { get; } = new();

    public override void Register()
    {
        SpellEffects.Add(new ObjectAreaTargetSelectHandler(FilterTargets, 0, Targets.UnitDestAreaAlly));
        SpellEffects.Add(new ObjectAreaTargetSelectHandler(FilterTargets, 1, Targets.UnitDestAreaAlly));
    }

    private void FilterTargets(List<WorldObject> pTargets)
    {
        var caster = Caster;

        if (caster != null)
        {
            pTargets.RemoveIf((WorldObject @object) =>
            {
                if (@object == null || @object.AsUnit == null)
                    return true;

                var unit = @object.AsUnit;

                if (unit == caster)
                    return true;

                if (unit.HasAura(MonkSpells.ESSENCE_FONT_HEAL) && unit.GetAura(MonkSpells.ESSENCE_FONT_HEAL).Duration > 5 * Time.IN_MILLISECONDS)
                    return true;

                return false;
            });

            if (pTargets.Count > 1)
            {
                pTargets.Sort(new HealthPctOrderPred());
                pTargets.Resize(1);
            }
        }
    }
}