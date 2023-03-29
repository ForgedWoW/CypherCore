﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Framework.Models;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Warlock;

// 108416 - Dark Pact
[SpellScript(108416)]
public class spell_warlock_dark_pact : AuraScript, IHasAuraEffects
{
    public List<IAuraEffectHandler> AuraEffects { get; } = new();

    public override void Register()
    {
        AuraEffects.Add(new AuraEffectCalcAmountHandler(CalculateAmount, 0, AuraType.SchoolAbsorb));
    }

    private void CalculateAmount(AuraEffect UnnamedParameter, BoxedValue<double> amount, BoxedValue<bool> canBeRecalculated)
    {
        var sacrifiedHealth = Caster.CountPctFromCurHealth(SpellInfo.GetEffect(1).BasePoints);
        Caster.ModifyHealth((long)sacrifiedHealth * -1);
        amount.Value = MathFunctions.CalculatePct(sacrifiedHealth, SpellInfo.GetEffect(2).BasePoints);
    }
}