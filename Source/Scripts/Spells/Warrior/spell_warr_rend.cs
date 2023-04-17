// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.IAura;
using Forged.MapServer.Spells.Auras;
using Framework.Constants;
using Framework.Models;

namespace Scripts.Spells.Warrior;

// 94009 - Rend
[SpellScript(94009)]
public class SpellWarrRend : AuraScript, IHasAuraEffects
{
    public List<IAuraEffectHandler> AuraEffects { get; } = new();

    public override void Register()
    {
        AuraEffects.Add(new AuraEffectCalcAmountHandler(CalculateAmount, 1, AuraType.PeriodicDamage));
    }

    private void CalculateAmount(AuraEffect aurEff, BoxedValue<double> amount, BoxedValue<bool> canBeRecalculated)
    {
        var caster = Caster;

        if (caster != null)
        {
            canBeRecalculated.Value = false;

            // $0.25 * (($MWB + $mwb) / 2 + $AP / 14 * $MWS) bonus per tick
            var ap = caster.GetTotalAttackPowerValue(WeaponAttackType.BaseAttack);
            var mws = caster.GetAttackTimer(WeaponAttackType.BaseAttack);
            var mwbMin = caster.GetWeaponDamageRange(WeaponAttackType.BaseAttack, WeaponDamageRange.MinDamage);
            var mwbMax = caster.GetWeaponDamageRange(WeaponAttackType.BaseAttack, WeaponDamageRange.MaxDamage);
            var mwb = ((mwbMin + mwbMax) / 2 + ap * mws / 14000) * 0.266f;
            amount.Value += caster.ApplyEffectModifiers(SpellInfo, aurEff.EffIndex, mwb);
        }
    }
}