// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Entities.Units;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.IAura;
using Forged.MapServer.Spells.Auras;
using Framework.Constants;
using Framework.Models;

namespace Scripts.Spells.DemonHunter;

[SpellScript(209426)]
public class SpellDhDarknessAbsorb : AuraScript, IHasAuraEffects
{
    public List<IAuraEffectHandler> AuraEffects { get; } = new();

    public override void Register()
    {
        AuraEffects.Add(new AuraEffectAbsorbHandler(OnAbsorb, 0));
        AuraEffects.Add(new AuraEffectCalcAmountHandler(CalculateAmount, 0, AuraType.SchoolAbsorb));
    }


    private void CalculateAmount(AuraEffect unnamedParameter, BoxedValue<double> amount, BoxedValue<bool> canBeRecalculated)
    {
        amount.Value = -1;
    }

    private double OnAbsorb(AuraEffect unnamedParameter, DamageInfo dmgInfo, double absorbAmount)
    {
        var caster = Caster;

        if (caster == null)
            return absorbAmount;

        var chance = SpellInfo.GetEffect(1).BasePoints + caster.GetAuraEffectAmount(ShatteredSoulsSpells.COVER_OF_DARKNESS, 0);

        if (RandomHelper.randChance(chance))
            absorbAmount = dmgInfo.Damage;

        return absorbAmount;
    }
}