﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Entities.Units;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.IAura;
using Forged.MapServer.Spells.Auras;
using Framework.Constants;

namespace Scripts.Spells.Warlock;

[Script] // 77220 - Mastery: Chaotic Energies
internal class SpellWarlChaoticEnergies : AuraScript, IHasAuraEffects
{
    public List<IAuraEffectHandler> AuraEffects { get; } = new();

    public override void Register()
    {
        AuraEffects.Add(new AuraEffectAbsorbHandler(HandleAbsorb, 2, false, AuraScriptHookType.EffectAbsorb));
    }

    private double HandleAbsorb(AuraEffect aurEff, DamageInfo dmgInfo, double absorbAmount)
    {
        var auraEffect = GetEffect(1);

        if (auraEffect == null ||
            !TargetApplication.HasEffect(1))
        {
            PreventDefaultAction();

            return absorbAmount;
        }

        // You take ${$s2/3}% reduced Damage
        var damageReductionPct = (double)auraEffect.Amount / 3;
        // plus a random amount of up to ${$s2/3}% additional reduced Damage
        damageReductionPct += RandomHelper.FRand(0.0f, damageReductionPct);

        return MathFunctions.CalculatePct(dmgInfo.Damage, damageReductionPct);
    }
}