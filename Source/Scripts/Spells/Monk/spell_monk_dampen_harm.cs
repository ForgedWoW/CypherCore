// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Entities.Units;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.IAura;
using Forged.MapServer.Spells.Auras;
using Framework.Constants;
using Framework.Models;

namespace Scripts.Spells.Monk;

[SpellScript(122278)]
public class SpellMonkDampenHarm : AuraScript, IHasAuraEffects
{
    private double _healthPct;
    public List<IAuraEffectHandler> AuraEffects { get; } = new();

    public override bool Load()
    {
        _healthPct = SpellInfo.GetEffect(0).CalcValue(Caster);

        return OwnerAsUnit.AsPlayer;
    }

    public override void Register()
    {
        AuraEffects.Add(new AuraEffectCalcAmountHandler(CalculateAmount, 0, AuraType.SchoolAbsorb));
        AuraEffects.Add(new AuraEffectAbsorbHandler(Absorb, 0));
    }

    private void CalculateAmount(AuraEffect unnamedParameter, BoxedValue<double> amount, BoxedValue<bool> canBeRecalculated)
    {
        amount.Value = -1;
    }

    private double Absorb(AuraEffect auraEffect, DamageInfo dmgInfo, double absorbAmount)
    {
        var target = Target;
        var health = target.CountPctFromMaxHealth(_healthPct);

        if (dmgInfo.Damage < health)
            return absorbAmount;

        absorbAmount = dmgInfo.Damage * (SpellInfo.GetEffect(0).CalcValue(Caster) / 100);
        auraEffect.Base.DropCharge();

        return absorbAmount;
    }
}