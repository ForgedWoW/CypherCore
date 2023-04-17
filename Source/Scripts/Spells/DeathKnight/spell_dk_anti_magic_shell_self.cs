// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Entities.Units;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.IAura;
using Forged.MapServer.Spells;
using Forged.MapServer.Spells.Auras;
using Framework.Constants;
using Framework.Models;

namespace Scripts.Spells.DeathKnight;

public class SpellDkAntiMagicShellSelf : AuraScript, IHasAuraEffects
{
    private double _absorbPct;
    public List<IAuraEffectHandler> AuraEffects { get; } = new();

    public override bool Load()
    {
        _absorbPct = SpellInfo.GetEffect(0).CalcValue(Caster);

        return true;
    }

    public override void Register()
    {
        AuraEffects.Add(new AuraEffectAbsorbHandler(Absorb, 0));
        AuraEffects.Add(new AuraEffectAbsorbHandler(Trigger, 0, false, AuraScriptHookType.EffectAfterAbsorb));
        AuraEffects.Add(new AuraEffectCalcAmountHandler(CalculateAmount, 0, AuraType.SchoolAbsorb));
    }

    private void CalculateAmount(AuraEffect unnamedParameter, BoxedValue<double> amount, BoxedValue<bool> canBeRecalculated)
    {
        amount.Value = OwnerAsUnit.CountPctFromMaxHealth(40);
    }

    private double Absorb(AuraEffect unnamedParameter, DamageInfo dmgInfo, double absorbAmount)
    {
        return MathFunctions.CalculatePct(dmgInfo.Damage, _absorbPct);
    }

    private double Trigger(AuraEffect aurEff, DamageInfo unnamedParameter, double absorbAmount)
    {
        var target = Target;
        // Patch 6.0.2 (October 14, 2014): Anti-Magic Shell now restores 2 Runic Power per 1% of max health absorbed.
        var damagePerRp = target.CountPctFromMaxHealth(1) / 2.0f;
        var energizeAmount = (absorbAmount / damagePerRp) * 10.0f;
        var args = new CastSpellExtraArgs();
        args.AddSpellMod(SpellValueMod.BasePoint0, (int)energizeAmount);
        args.SetTriggerFlags(TriggerCastFlags.FullMask);
        args.SetTriggeringAura(aurEff);
        target.SpellFactory.CastSpell(target, DeathKnightSpells.RUNIC_POWER_ENERGIZE, args);

        return absorbAmount;
    }
}