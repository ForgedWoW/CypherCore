// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.IAura;
using Forged.MapServer.Spells.Auras;
using Framework.Constants;

namespace Scripts.Spells.Druid;

// 202430 - Nature's Balance
[SpellScript(202430)]
public class SpellDruNaturesBalance : AuraScript, IHasAuraEffects
{
    public List<IAuraEffectHandler> AuraEffects { get; } = new();


    public override void Register()
    {
        AuraEffects.Add(new AuraEffectPeriodicHandler(HandlePeriodic, 0, AuraType.PeriodicEnergize));
    }

    private void HandlePeriodic(AuraEffect aurEff)
    {
        var caster = Caster;

        if (caster == null || !caster.IsAlive || caster.GetMaxPower(PowerType.LunarPower) == 0)
            return;

        if (caster.IsInCombat)
        {
            var amount = Math.Max(caster.GetAuraEffect(Spells.NATURES_BALANCE, 0).Amount, 0);

            // don't regen when permanent aura target has full power
            if (caster.GetPower(PowerType.LunarPower) == caster.GetMaxPower(PowerType.LunarPower))
                return;

            caster.ModifyPower(PowerType.LunarPower, amount);
        }
        else
        {
            if (caster.GetPower(PowerType.LunarPower) > 500)
                return;

            caster.SetPower(PowerType.LunarPower, 500);
        }
    }

    private struct Spells
    {
        public const uint NATURES_BALANCE = 202430;
    }
}