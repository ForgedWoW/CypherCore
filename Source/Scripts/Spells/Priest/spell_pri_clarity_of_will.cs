// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.IAura;
using Forged.MapServer.Spells.Auras;
using Framework.Constants;
using Framework.Models;

namespace Scripts.Spells.Priest;

[SpellScript(152118)]
public class SpellPriClarityOfWill : AuraScript, IHasAuraEffects
{
    public List<IAuraEffectHandler> AuraEffects { get; } = new();

    public override void Register()
    {
        AuraEffects.Add(new AuraEffectCalcAmountHandler(CalculateAmount, 0, AuraType.SchoolAbsorb));
    }

    private void CalculateAmount(AuraEffect aurEff, BoxedValue<double> amount, BoxedValue<bool> canBeRecalculated)
    {
        var caster = aurEff.Caster;

        if (caster != null)
        {
            var player = caster.AsPlayer;

            if (player != null)
            {
                var absorbamount = 9.0f * player.SpellBaseHealingBonusDone(SpellInfo.SchoolMask);
                amount.Value += absorbamount;
            }
        }
    }
}