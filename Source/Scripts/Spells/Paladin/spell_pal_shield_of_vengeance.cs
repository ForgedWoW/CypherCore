﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Framework.Models;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Paladin;

// 184662 - Shield of Vengeance
[SpellScript(184662)]
public class spell_pal_shield_of_vengeance : AuraScript, IHasAuraEffects
{
    private int absorb;
    private int currentAbsorb;
    public List<IAuraEffectHandler> AuraEffects { get; } = new();

    public override void Register()
    {
        AuraEffects.Add(new AuraEffectCalcAmountHandler(CalculateAmount, 0, AuraType.SchoolAbsorb));
        AuraEffects.Add(new AuraEffectApplyHandler(OnRemove, 0, AuraType.SchoolAbsorb, AuraEffectHandleModes.Real, AuraScriptHookType.EffectRemove));
        AuraEffects.Add(new AuraEffectAbsorbHandler(Absorb, 0));
    }

    private void CalculateAmount(AuraEffect UnnamedParameter, BoxedValue<double> amount, BoxedValue<bool> canBeRecalculated)
    {
        var caster = Caster;

        if (caster != null)
        {
            canBeRecalculated.Value = false;

            var ap = caster.GetTotalAttackPowerValue(WeaponAttackType.BaseAttack);
            absorb = (int)(ap * 20);
            amount.Value += absorb;
        }
    }

    private double Absorb(AuraEffect aura, DamageInfo damageInfo, double absorbAmount)
    {
        var caster = Caster;

        if (caster == null)
            return absorbAmount;

        currentAbsorb += (int)damageInfo.Damage;

        return absorbAmount;
    }

    private void OnRemove(AuraEffect UnnamedParameter, AuraEffectHandleModes UnnamedParameter2)
    {
        var caster = Caster;

        if (caster == null)
            return;

        if (currentAbsorb < absorb)
            return;

        var targets = new List<Unit>();
        caster.GetAttackableUnitListInRange(targets, 8.0f);

        var targetSize = (uint)targets.Count;

        if (targets.Count != 0)
            absorb /= (int)targetSize;

        caster.CastSpell(caster, PaladinSpells.SHIELD_OF_VENGEANCE_DAMAGE, new CastSpellExtraArgs(TriggerCastFlags.FullMask).AddSpellMod(SpellValueMod.BasePoint0, (int)absorb));
    }
}