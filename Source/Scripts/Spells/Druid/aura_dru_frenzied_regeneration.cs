// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.IAura;
using Forged.MapServer.Spells.Auras;
using Framework.Constants;
using Framework.Models;

namespace Scripts.Spells.Druid;

[SpellScript(22842)]
public class AuraDruFrenziedRegeneration : AuraScript, IHasAuraEffects
{
    public List<IAuraEffectHandler> AuraEffects { get; } = new();

    public override void Register()
    {
        AuraEffects.Add(new AuraEffectCalcAmountHandler(CalculateAmount, 0, AuraType.ObsModHealth));
    }

    private void CalculateAmount(AuraEffect unnamedParameter, BoxedValue<double> amount, BoxedValue<bool> canBeRecalculated)
    {
        var frenzied = Caster.GetAura(22842);

        //if (frenzied != null)
        //	frenzied.MaxDuration;

        var healAmount = MathFunctions.CalculatePct(Caster.GetDamageOverLastSeconds(5), 50);
        var minHealAmount = MathFunctions.CalculatePct(Caster.MaxHealth, 5);
        healAmount = Math.Max(healAmount, minHealAmount);
        amount.Value = healAmount;
    }
}