// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.IAura;
using Forged.MapServer.Spells.Auras;
using Framework.Constants;
using Framework.Models;

namespace Scripts.Spells.Warlock;

// 108416 - Dark Pact
[SpellScript(108416)]
public class SpellWarlockDarkPact : AuraScript, IHasAuraEffects
{
    public List<IAuraEffectHandler> AuraEffects { get; } = new();

    public override void Register()
    {
        AuraEffects.Add(new AuraEffectCalcAmountHandler(CalculateAmount, 0, AuraType.SchoolAbsorb));
    }

    private void CalculateAmount(AuraEffect unnamedParameter, BoxedValue<double> amount, BoxedValue<bool> canBeRecalculated)
    {
        var sacrifiedHealth = Caster.CountPctFromCurHealth(SpellInfo.GetEffect(1).BasePoints);
        Caster.ModifyHealth((long)sacrifiedHealth * -1);
        amount.Value = MathFunctions.CalculatePct(sacrifiedHealth, SpellInfo.GetEffect(2).BasePoints);
    }
}