﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Framework.Models;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Monk;

[SpellScript(115080)]
public class spell_monk_touch_of_death : AuraScript, IHasAuraEffects
{
    public List<IAuraEffectHandler> AuraEffects { get; } = new();

    public override void Register()
    {
        AuraEffects.Add(new AuraEffectCalcAmountHandler(CalculateAmount, 0, AuraType.PeriodicDummy));
        AuraEffects.Add(new AuraEffectPeriodicHandler(OnTick, 0, AuraType.PeriodicDummy));
    }

    private void CalculateAmount(AuraEffect aurEff, BoxedValue<double> amount, BoxedValue<bool> canBeRecalculated)
    {
        canBeRecalculated.Value = true;
        var caster = Caster;

        if (caster != null)
        {
            var effInfo = Aura.SpellInfo.GetEffect(1).CalcValue();

            if (effInfo != 0)
            {
                amount.Value = caster.CountPctFromMaxHealth(effInfo);

                aurEff.SetAmount(amount);
            }
        }
    }

    private void OnTick(AuraEffect aurEff)
    {
        var caster = Caster;

        if (caster != null)
        {
            var damage = aurEff.Amount;

            // Damage reduced to Players, need to check reduction value
            if (Target.TypeId == TypeId.Player)
                damage /= 2;

            caster.CastSpell(Target, MonkSpells.TOUCH_OF_DEATH_DAMAGE, new CastSpellExtraArgs().AddSpellMod(SpellValueMod.BasePoint0, (int)damage));
        }
    }
}