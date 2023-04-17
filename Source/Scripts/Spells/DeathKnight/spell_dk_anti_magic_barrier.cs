// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.IAura;
using Forged.MapServer.Spells.Auras;
using Framework.Constants;
using Framework.Models;

namespace Scripts.Spells.DeathKnight;

[SpellScript(205725)]
public class SpellDkAntiMagicBarrier : AuraScript, IHasAuraEffects
{
    public List<IAuraEffectHandler> AuraEffects { get; } = new();

    public override void Register()
    {
        AuraEffects.Add(new AuraEffectCalcAmountHandler(CalcAmount, 0, AuraType.ModIncreaseHealth2));
    }

    private void CalcAmount(AuraEffect aurEff, BoxedValue<double> amount, BoxedValue<bool> unnamedParameter)
    {
        var caster = Caster;

        if (caster != null)
            amount.Value = ((caster.MaxHealth * 25.0f) / 100.0f);
    }
}