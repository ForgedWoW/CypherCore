﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.IAura;
using Forged.MapServer.Spells.Auras;
using Framework.Constants;
using Framework.Models;

namespace Scripts.Spells.Rogue;

[Script] // 1943 - Rupture
internal class SpellRogRuptureAuraScript : AuraScript, IHasAuraEffects
{
    public List<IAuraEffectHandler> AuraEffects { get; } = new();


    public override void Register()
    {
        AuraEffects.Add(new AuraEffectCalcAmountHandler(CalculateAmount, 0, AuraType.PeriodicDummy));
        AuraEffects.Add(new AuraEffectApplyHandler(OnEffectRemoved, 0, AuraType.PeriodicDummy, AuraEffectHandleModes.Real, AuraScriptHookType.EffectRemove));
    }

    private void CalculateAmount(AuraEffect aurEff, BoxedValue<double> amount, BoxedValue<bool> canBeRecalculated)
    {
        var caster = Caster;

        if (caster != null)
        {
            canBeRecalculated.Value = false;

            double[] attackpowerPerCombo =
            {
                0.0f, 0.015f, // 1 point:  ${($m1 + $b1*1 + 0.015 * $AP) * 4} Damage over 8 secs
                0.024f,       // 2 points: ${($m1 + $b1*2 + 0.024 * $AP) * 5} Damage over 10 secs
                0.03f,        // 3 points: ${($m1 + $b1*3 + 0.03 * $AP) * 6} Damage over 12 secs
                0.03428571f,  // 4 points: ${($m1 + $b1*4 + 0.03428571 * $AP) * 7} Damage over 14 secs
                0.0375f       // 5 points: ${($m1 + $b1*5 + 0.0375 * $AP) * 8} Damage over 16 secs
            };

            var cp = caster.GetComboPoints();

            if (cp > 5)
                cp = 5;

            amount.Value += (caster.GetTotalAttackPowerValue(WeaponAttackType.BaseAttack) * attackpowerPerCombo[cp]);
        }
    }

    private void OnEffectRemoved(AuraEffect aurEff, AuraEffectHandleModes mode)
    {
        if (TargetApplication.RemoveMode != AuraRemoveMode.Death)
            return;

        var aura = Aura;
        var caster = aura.Caster;

        if (!caster)
            return;

        var auraVenomousWounds = caster.GetAura(RogueSpells.VenomousWounds);

        if (auraVenomousWounds == null)
            return;

        // Venomous Wounds: if unit dies while being affected by rupture, regain energy based on remaining duration
        var cost = SpellInfo.CalcPowerCost(PowerType.Energy, false, caster, SpellInfo.SchoolMask, null);

        if (cost == null)
            return;

        var pct = (double)aura.Duration / (double)aura.MaxDuration;
        var extraAmount = (int)((double)cost.Amount * pct);
        caster.ModifyPower(PowerType.Energy, extraAmount);
    }
}