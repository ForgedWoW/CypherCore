// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.IAura;
using Forged.MapServer.Spells.Auras;
using Framework.Constants;
using Framework.Models;

namespace Scripts.Spells.Evoker;

[SpellScript(EvokerSpells.GREEN_EMERALD_COMMUNION)]
internal class AuraEvokerEmeraldCommunion : AuraScript, IHasAuraEffects
{
    public List<IAuraEffectHandler> AuraEffects { get; } = new();

    public override void Register()
    {
        AuraEffects.Add(new AuraEffectCalcAmountHandler(CalcAmount, 7, AuraType.PeriodicHeal));
    }

    private void CalcAmount(AuraEffect aurEff, BoxedValue<double> amount, BoxedValue<bool> recalculate)
    {
        recalculate.Value = false;

        amount.Value = (Caster.MaxHealth * (GetEffect(4).BaseAmount * 0.01)) / (Aura.Duration / aurEff.Period);
    }
}