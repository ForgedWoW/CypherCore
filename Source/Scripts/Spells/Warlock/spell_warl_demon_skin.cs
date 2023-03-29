﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Warlock;

//219272 - Demon Skin
[SpellScript(219272)]
public class spell_warl_demon_skin : AuraScript, IHasAuraEffects
{
    public List<IAuraEffectHandler> AuraEffects { get; } = new();

    public override void Register()
    {
        AuraEffects.Add(new AuraEffectApplyHandler(OnRemove, 0, AuraType.PeriodicDummy, AuraEffectHandleModes.Real, AuraScriptHookType.EffectRemove));
        AuraEffects.Add(new AuraEffectPeriodicHandler(PeriodicTick, 0, AuraType.PeriodicDummy));
    }

    private void PeriodicTick(AuraEffect aurEff)
    {
        var caster = Caster;

        if (caster == null)
            return;

        var absorb = (aurEff.Amount / 10.0f) * caster.MaxHealth / 100.0f;

        // Add remaining amount if already applied
        var soulLeechShield = caster.GetAuraEffect(WarlockSpells.SOUL_LEECH_SHIELD, 0);

        if (soulLeechShield != null)
            absorb += soulLeechShield.Amount;

        MathFunctions.AddPct(ref absorb, caster.GetAuraEffectAmount(WarlockSpells.ARENA_DAMPENING, 0));

        double threshold = caster.CountPctFromMaxHealth(GetEffect(1).Amount);
        absorb = Math.Min(absorb, threshold);

        if (soulLeechShield != null)
            soulLeechShield.SetAmount((int)absorb);
        else
            caster.CastSpell(caster, WarlockSpells.SOUL_LEECH_SHIELD, new CastSpellExtraArgs(TriggerCastFlags.FullMask).AddSpellMod(SpellValueMod.BasePoint0, (int)absorb));
    }

    private void OnRemove(AuraEffect UnnamedParameter, AuraEffectHandleModes UnnamedParameter2)
    {
        var caster = Caster;

        if (caster == null)
            return;

        var aur = caster.GetAura(WarlockSpells.SOUL_LEECH_SHIELD);

        if (aur != null)
        {
            aur.SetMaxDuration(15000);
            aur.RefreshDuration();
        }
    }
}