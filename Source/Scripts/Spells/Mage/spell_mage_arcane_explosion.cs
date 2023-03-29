﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Mage;

[Script] // 1449 - Arcane Explosion
internal class spell_mage_arcane_explosion : SpellScript, IHasSpellEffects
{
    public List<ISpellEffect> SpellEffects { get; } = new();

    public override void Register()
    {
        SpellEffects.Add(new EffectHandler(CheckRequiredAuraForBaselineEnergize, 0, SpellEffectName.Energize, SpellScriptHookType.EffectHitTarget));
        SpellEffects.Add(new EffectHandler(HandleReverberate, 2, SpellEffectName.Energize, SpellScriptHookType.EffectHitTarget));
    }

    private void CheckRequiredAuraForBaselineEnergize(int effIndex)
    {
        if (GetUnitTargetCountForEffect(1) == 0 ||
            !Caster.HasAura(MageSpells.ArcaneMage))
            PreventHitDefaultEffect(effIndex);
    }

    private void HandleReverberate(int effIndex)
    {
        var procTriggered = false;

        var caster = Caster;
        var triggerChance = caster.GetAuraEffect(MageSpells.Reverberate, 0);

        if (triggerChance != null)
        {
            var requiredTargets = caster.GetAuraEffect(MageSpells.Reverberate, 1);

            if (requiredTargets != null)
                procTriggered = GetUnitTargetCountForEffect(1) >= requiredTargets.Amount && RandomHelper.randChance(triggerChance.Amount);
        }

        if (!procTriggered)
            PreventHitDefaultEffect(effIndex);
    }
}