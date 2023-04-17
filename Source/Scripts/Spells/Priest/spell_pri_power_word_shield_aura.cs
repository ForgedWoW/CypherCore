// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.IAura;
using Forged.MapServer.Spells.Auras;
using Framework.Constants;
using Framework.Models;

namespace Scripts.Spells.Priest;

[SpellScript(17)] // 17 - Power Word: Shield Aura
internal class SpellPriPowerWordShieldAura : AuraScript, IHasAuraEffects
{
    public List<IAuraEffectHandler> AuraEffects { get; } = new();

    public override void Register()
    {
        AuraEffects.Add(new AuraEffectCalcAmountHandler(CalculateAmount, 0, AuraType.SchoolAbsorb));
        AuraEffects.Add(new AuraEffectApplyHandler(HandleOnApply, 0, AuraType.SchoolAbsorb, AuraEffectHandleModes.RealOrReapplyMask, AuraScriptHookType.EffectAfterApply));
        AuraEffects.Add(new AuraEffectApplyHandler(HandleOnRemove, 0, AuraType.SchoolAbsorb, AuraEffectHandleModes.Real, AuraScriptHookType.EffectAfterRemove));
    }

    private void CalculateAmount(AuraEffect auraEffect, BoxedValue<double> amount, BoxedValue<bool> canBeRecalculated)
    {
        canBeRecalculated.Value = false;

        var caster = Caster;

        if (caster != null)
        {
            var amountF = caster.SpellBaseDamageBonusDone(SpellInfo.GetSchoolMask()) * 1.65f;

            var player = caster.AsPlayer;

            if (player != null)
            {
                MathFunctions.AddPct(ref amountF, player.GetRatingBonusValue(CombatRating.VersatilityDamageDone));

                var mastery = caster.GetAuraEffect(PriestSpells.MASTERY_GRACE, 0);

                if (mastery != null)
                    if (OwnerAsUnit.HasAura(PriestSpells.ATONEMENT_TRIGGERED) ||
                        OwnerAsUnit.HasAura(PriestSpells.ATONEMENT_TRIGGERED_POWER_TRINITY))
                        MathFunctions.AddPct(ref amountF, mastery.Amount);
            }

            var rapture = caster.GetAuraEffect(PriestSpells.RAPTURE, 1);

            if (rapture != null)
                MathFunctions.AddPct(ref amountF, rapture.Amount);

            amount.Value = amountF;
        }
    }

    private void HandleOnApply(AuraEffect aurEff, AuraEffectHandleModes mode)
    {
        var caster = Caster;
        var target = Target;

        if (!caster)
            return;

        if (caster.HasAura(PriestSpells.BODY_AND_SOUL))
            caster.SpellFactory.CastSpell(target, PriestSpells.BODY_AND_SOUL_SPEED, true);

        if (caster.HasAura(PriestSpells.STRENGTH_OF_SOUL))
            caster.SpellFactory.CastSpell(target, PriestSpells.STRENGTH_OF_SOUL_EFFECT, true);

        if (caster.HasAura(PriestSpells.RENEWED_HOPE))
            caster.SpellFactory.CastSpell(target, PriestSpells.RENEWED_HOPE_EFFECT, true);

        if (caster.HasAura(PriestSpells.VOID_SHIELD) &&
            caster == target)
            caster.SpellFactory.CastSpell(target, PriestSpells.VOID_SHIELD_EFFECT, true);

        if (caster.HasAura(PriestSpells.ATONEMENT))
            caster.SpellFactory.CastSpell(target, caster.HasAura(PriestSpells.TRINITY) ? PriestSpells.ATONEMENT_TRIGGERED_POWER_TRINITY : PriestSpells.ATONEMENT_TRIGGERED, true);
    }

    private void HandleOnRemove(AuraEffect aurEff, AuraEffectHandleModes mode)
    {
        Target.RemoveAura(PriestSpells.STRENGTH_OF_SOUL_EFFECT);
        var caster = Caster;

        if (caster)
            if (TargetApplication.RemoveMode == AuraRemoveMode.EnemySpell &&
                caster.HasAura(PriestSpells.SHIELD_DISCIPLINE_PASSIVE))
                caster.SpellFactory.CastSpell(caster, PriestSpells.SHIELD_DISCIPLINE_ENERGIZE, true);
    }
}