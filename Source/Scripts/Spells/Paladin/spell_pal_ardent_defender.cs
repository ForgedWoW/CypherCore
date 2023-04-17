// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Entities.Units;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.IAura;
using Forged.MapServer.Spells.Auras;
using Framework.Constants;
using Framework.Models;

namespace Scripts.Spells.Paladin;

// 31850 - ardent defender
[SpellScript(31850)]
public class SpellPalArdentDefender : AuraScript, IHasAuraEffects
{
    private double _absorbPct;
    private double _healPct;

    public SpellPalArdentDefender()
    {
        _absorbPct = 0;
        _healPct = 0;
    }

    public List<IAuraEffectHandler> AuraEffects { get; } = new();


    public override bool Load()
    {
        _absorbPct = SpellInfo.GetEffect(0).CalcValue();
        _healPct = SpellInfo.GetEffect(1).CalcValue();

        return OwnerAsUnit.IsPlayer;
    }

    public void CalculateAmount(AuraEffect unnamedParameter, BoxedValue<double> amount, BoxedValue<bool> canBeRecalculated)
    {
        amount.Value = -1;
    }

    public double Absorb(AuraEffect aurEff, DamageInfo dmgInfo, double absorbAmount)
    {
        absorbAmount = MathFunctions.CalculatePct(dmgInfo.Damage, _absorbPct);

        var target = Target;

        if (dmgInfo.Damage < target.Health)
            return absorbAmount;

        double healAmount = target.CountPctFromMaxHealth(_healPct);
        target.SpellFactory.CastSpell(target, PaladinSpells.ARDENT_DEFENDER_HEAL, (int)healAmount);
        aurEff.Base.Remove();

        return absorbAmount;
    }

    public override void Register()
    {
        AuraEffects.Add(new AuraEffectCalcAmountHandler(CalculateAmount, 1, AuraType.SchoolAbsorb));
        AuraEffects.Add(new AuraEffectAbsorbHandler(Absorb, 1));
    }
}