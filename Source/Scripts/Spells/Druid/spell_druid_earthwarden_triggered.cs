// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Entities.Units;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.IAura;
using Forged.MapServer.Spells.Auras;
using Framework.Constants;
using Framework.Models;

namespace Scripts.Spells.Druid;

[SpellScript(203975)]
public class SpellDruidEarthwardenTriggered : AuraScript, IHasAuraEffects
{
    public List<IAuraEffectHandler> AuraEffects { get; } = new();


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
        if (dmgInfo.DamageType == DamageEffectType.Direct)
        {
            var earthwarden = Global.SpellMgr.AssertSpellInfo(Spells.Earthwarden, Difficulty.None);

            absorbAmount = MathFunctions.CalculatePct(dmgInfo.Damage, earthwarden.GetEffect(0).BasePoints);
            Caster.RemoveAura(Spells.EarthwardenTriggered);
        }

        return absorbAmount;
    }

    private struct Spells
    {
        public static readonly uint Earthwarden = 203974;
        public static readonly uint EarthwardenTriggered = 203975;
    }
}