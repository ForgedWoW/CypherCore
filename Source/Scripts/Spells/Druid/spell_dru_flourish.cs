// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Entities.Objects;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces;
using Forged.MapServer.Scripting.Interfaces.ISpell;
using Framework.Constants;

namespace Scripts.Spells.Druid;

[SpellScript(197721)]
public class SpellDruFlourish : SpellScript, IHasSpellEffects
{
    public List<ISpellEffect> SpellEffects { get; } = new();

    public override void Register()
    {
        SpellEffects.Add(new EffectHandler(HandleHit, 0, SpellEffectName.ScriptEffect, SpellScriptHookType.EffectHitTarget));
        SpellEffects.Add(new ObjectAreaTargetSelectHandler(FilterTargets, 0, Targets.UnitDestAreaAlly));
    }

    private void HandleHit(int effIndex)
    {
        if (!Caster || !HitUnit)
            return;

        var auraEffects = HitUnit.GetAuraEffectsByType(AuraType.PeriodicHeal);

        foreach (var auraEffect in auraEffects)
            if (auraEffect.CasterGuid == Caster.GUID)
            {
                var healAura = auraEffect.Base;

                if (healAura != null)
                    healAura.SetDuration(healAura.Duration + EffectValue * Time.IN_MILLISECONDS);
            }
    }

    private void FilterTargets(List<WorldObject> targets)
    {
        var tempTargets = new List<WorldObject>();

        foreach (var target in targets)
            if (target.IsPlayer)
                if (target.AsUnit.HasAuraTypeWithCaster(AuraType.PeriodicHeal, Caster.GUID))
                    tempTargets.Add(target);

        if (tempTargets.Count > 0)
        {
            targets.Clear();

            foreach (var target in tempTargets)
                targets.Add(target);
        }
    }
}