﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Entities.Units;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.IAura;
using Forged.MapServer.Spells.Auras;
using Framework.Constants;

namespace Scripts.Spells.Warlock;

[SpellScript(755)] // 755 - Health Funnel
internal class SpellWarlHealthFunnel : AuraScript, IHasAuraEffects
{
    public List<IAuraEffectHandler> AuraEffects { get; } = new();

    public override void Register()
    {
        AuraEffects.Add(new AuraEffectApplyHandler(ApplyEffect, 0, AuraType.ObsModHealth, AuraEffectHandleModes.Real, AuraScriptHookType.EffectApply));
        AuraEffects.Add(new AuraEffectApplyHandler(RemoveEffect, 0, AuraType.ObsModHealth, AuraEffectHandleModes.Real, AuraScriptHookType.EffectRemove));
        AuraEffects.Add(new AuraEffectPeriodicHandler(OnPeriodic, 0, AuraType.ObsModHealth));
    }

    private void ApplyEffect(AuraEffect aurEff, AuraEffectHandleModes mode)
    {
        var caster = Caster;

        if (!caster)
            return;

        var target = Target;

        if (caster.HasAura(WarlockSpells.IMPROVED_HEALTH_FUNNEL_R2))
            target.SpellFactory.CastSpell(target, WarlockSpells.IMPROVED_HEALTH_FUNNEL_BUFF_R2, true);
        else if (caster.HasAura(WarlockSpells.IMPROVED_HEALTH_FUNNEL_R1))
            target.SpellFactory.CastSpell(target, WarlockSpells.IMPROVED_HEALTH_FUNNEL_BUFF_R1, true);
    }

    private void RemoveEffect(AuraEffect aurEff, AuraEffectHandleModes mode)
    {
        var target = Target;
        target.RemoveAura(WarlockSpells.IMPROVED_HEALTH_FUNNEL_BUFF_R1);
        target.RemoveAura(WarlockSpells.IMPROVED_HEALTH_FUNNEL_BUFF_R2);
    }

    private void OnPeriodic(AuraEffect aurEff)
    {
        var caster = Caster;

        if (!caster)
            return;

        //! HACK for self Damage, is not blizz :/
        var damage = (uint)caster.CountPctFromMaxHealth(aurEff.BaseAmount);

        var modOwner = caster.SpellModOwner;

        if (modOwner)
            modOwner.ApplySpellMod(SpellInfo, SpellModOp.PowerCost0, ref damage);

        SpellNonMeleeDamage damageInfo = new(caster, caster, SpellInfo, Aura.SpellVisual, SpellInfo.SchoolMask, Aura.CastId);
        damageInfo.PeriodicLog = true;
        damageInfo.Damage = damage;
        caster.DealSpellDamage(damageInfo, false);
        caster.SendSpellNonMeleeDamageLog(damageInfo);
    }
}