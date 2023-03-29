// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Framework.Models;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Generic;

[Script] // 72623 Drink
internal class spell_gen_arena_drink : AuraScript, IHasAuraEffects
{
    public List<IAuraEffectHandler> AuraEffects { get; } = new();

    public override bool Load()
    {
        return Caster && Caster.IsTypeId(TypeId.Player);
    }

    public override void Register()
    {
        AuraEffects.Add(new AuraEffectCalcPeriodicHandler(CalcPeriodic, 1, AuraType.PeriodicDummy));
        AuraEffects.Add(new AuraEffectCalcAmountHandler(CalcAmount, 1, AuraType.PeriodicDummy));
        AuraEffects.Add(new AuraEffectUpdatePeriodicHandler(UpdatePeriodic, 1, AuraType.PeriodicDummy));
    }

    private void CalcPeriodic(AuraEffect aurEff, BoxedValue<bool> isPeriodic, BoxedValue<int> amplitude)
    {
        // Get AURA_MOD_POWER_REGEN aura from spell
        var regen = Aura.GetEffect(0);

        if (regen == null)
            return;

        // default case - not in arena
        if (!Caster.AsPlayer.InArena)
            isPeriodic.Value = false;
    }

    private void CalcAmount(AuraEffect aurEff, BoxedValue<double> amount, BoxedValue<bool> canBeRecalculated)
    {
        var regen = Aura.GetEffect(0);

        if (regen == null)
            return;

        // default case - not in arena
        if (!Caster.AsPlayer.InArena)
            regen.ChangeAmount(amount);
    }

    private void UpdatePeriodic(AuraEffect aurEff)
    {
        var regen = Aura.GetEffect(0);

        if (regen == null)
            return;

        // **********************************************
        // This feature used only in arenas
        // **********************************************
        // Here need increase mana regen per tick (6 second rule)
        // on 0 tick -   0  (handled in 2 second)
        // on 1 tick - 166% (handled in 4 second)
        // on 2 tick - 133% (handled in 6 second)

        // Apply bonus for 1 - 4 tick
        switch (aurEff.GetTickNumber())
        {
            case 1: // 0%
                regen.ChangeAmount(0);

                break;
            case 2: // 166%
                regen.ChangeAmount(aurEff.Amount * 5 / 3);

                break;
            case 3: // 133%
                regen.ChangeAmount(aurEff.Amount * 4 / 3);

                break;
            default: // 100% - normal regen
                regen.ChangeAmount(aurEff.Amount);
                // No need to update after 4th tick
                aurEff.SetPeriodic(false);

                break;
        }
    }
}