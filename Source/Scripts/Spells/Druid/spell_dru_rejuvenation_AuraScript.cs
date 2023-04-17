// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.IAura;
using Forged.MapServer.Spells.Auras;
using Framework.Constants;
using Framework.Models;

namespace Scripts.Spells.Druid;

[SpellScript(774)]
public class SpellDruRejuvenationAuraScript : AuraScript, IHasAuraEffects
{
    public List<IAuraEffectHandler> AuraEffects { get; } = new();

    public override void Register()
    {
        // Posible Fixed
        AuraEffects.Add(new AuraEffectApplyHandler(OnApply, 0, AuraType.PeriodicHeal, AuraEffectHandleModes.Real));
        AuraEffects.Add(new AuraEffectApplyHandler(OnRemove, 0, AuraType.PeriodicHeal, AuraEffectHandleModes.Real, AuraScriptHookType.EffectRemove));
        AuraEffects.Add(new AuraEffectCalcAmountHandler(HandleCalculateAmount, 0, AuraType.PeriodicHeal));

        //  OnEffectPeriodic += AuraEffectPeriodicFn(spell_dru_rejuvenation::OnPeriodic, 0, AuraType.PeriodicHeal);
        //  DoEffectCalcAmount += AuraEffectCalcAmountFn(spell_dru_rejuvenation::CalculateAmount, 0, AuraType.PeriodicHeal);
        //  AfterEffectRemove += AuraEffectRemoveFn(spell_dru_rejuvenation::AfterRemove, 0, AuraType.PeriodicHeal, AuraEffectHandleModes.Real);
    }

    private void HandleCalculateAmount(AuraEffect unnamedParameter, BoxedValue<double> amount, BoxedValue<bool> canBeRecalculated)
    {
        var lCaster = Caster;

        if (lCaster != null)
            ///If soul of the forest is activated we increase the heal by 100%
            if (lCaster.HasAura(SoulOfTheForestSpells.SoulOfTheForestResto) && !lCaster.HasAura(DruidSpells.Rejuvenation))
            {
                amount.Value *= 2;
                lCaster.RemoveAura(SoulOfTheForestSpells.SoulOfTheForestResto);
            }
    }

    private void OnApply(AuraEffect unnamedParameter, AuraEffectHandleModes unnamedParameter2)
    {
        var caster = Caster;

        if (caster == null)
            return;

        var glyphOfRejuvenation = caster.GetAuraEffect(Spells.GlyphofRejuvenation, 0);

        if (glyphOfRejuvenation != null)
        {
            glyphOfRejuvenation.SetAmount(glyphOfRejuvenation.Amount + 1);

            if (glyphOfRejuvenation.Amount >= 3)
                caster.SpellFactory.CastSpell(caster, Spells.GlyphofRejuvenationEffect, true);
        }
    }

    private void OnRemove(AuraEffect unnamedParameter, AuraEffectHandleModes unnamedParameter2)
    {
        var caster = Caster;

        if (caster == null)
            return;

        var lGlyphOfRejuvenation = caster.GetAuraEffect(Spells.GlyphofRejuvenation, 0);

        if (lGlyphOfRejuvenation != null)
        {
            lGlyphOfRejuvenation.SetAmount(lGlyphOfRejuvenation.Amount - 1);

            if (lGlyphOfRejuvenation.Amount < 3)
                caster.RemoveAura(Spells.GlyphofRejuvenationEffect);
        }
    }


    //Posible Fixed


    // bool Validate(SpellInfo const* spellInfo) override
    // {
    //     return ValidateSpellInfo(
    //         {
    //             CULTIVATION,
    //             CULTIVATION_HOT,
    //             ABUNDANCE,
    //             ABUNDANCE_BUFF,
    //         });
    // }
    //
    // void AfterRemove(AuraEffect const* aurEff, AuraEffectHandleModes mode)
    // {
    //     if (Unit* caster = GetCaster())
    //         if (caster->HasAura(ABUNDANCE))
    //             if (Aura* abundanceBuff = caster->GetAura(ABUNDANCE_BUFF))
    //                 abundanceBuff->ModStackAmount(-1);
    // }
    //
    // void OnPeriodic(AuraEffect const* aurEff)
    // {
    //     if (Unit* target = GetTarget())
    //         if (GetCaster()->HasAura(CULTIVATION) && !target->HasAura(CULTIVATION_HOT) && target->HealthBelowPct(Global.SpellMgr->GetSpellInfo//(CULTIVATION)->GetEffect(0).BasePoints))
    //             GetCaster()->CastSpell(target, CULTIVATION_HOT, true);
    // }
    //
    // void CalculateAmount(AuraEffect const* aurEff, int32& amount, bool& canBeRecalculated)
    // {
    //     if (!GetCaster())
    //         return;
    //
    //     amount = MathFunctions.CalculatePct(GetCaster()->SpellBaseHealingBonusDone(SpellSchoolMask.Nature), 60);
    // }

    //Posible Fixed

    private struct Spells
    {
        public static readonly uint GlyphofRejuvenation = 17076;
        public static readonly uint GlyphofRejuvenationEffect = 96206;
    }
}