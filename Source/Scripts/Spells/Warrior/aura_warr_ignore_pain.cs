// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using Forged.MapServer.Entities.Units;
using Forged.MapServer.Networking.Packets.Spell;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.IAura;
using Forged.MapServer.Spells.Auras;
using Framework.Constants;
using Framework.Models;

namespace Scripts.Spells.Warrior;

//190456 - Ignore Pain
[SpellScript(190456)]
public class AuraWarrIgnorePain : AuraScript, IHasAuraEffects
{
    private int _mExtraSpellCost;

    public List<IAuraEffectHandler> AuraEffects { get; } = new();

    public override bool Load()
    {
        var caster = Caster;
        // In this phase the initial 20 Rage cost is removed already
        // We just check for bonus.
        _mExtraSpellCost = Math.Min(caster.GetPower(PowerType.Rage), 400);

        return true;
    }

    public override void Register()
    {
        AuraEffects.Add(new AuraEffectCalcAmountHandler(CalcAmount, 0, AuraType.SchoolAbsorb));
        AuraEffects.Add(new AuraEffectAbsorbHandler(OnAbsorb, 0));
    }

    private void CalcAmount(AuraEffect unnamedParameter, BoxedValue<double> amount, BoxedValue<bool> canBeRecalculated)
    {
        var caster = Caster;

        if (caster != null)
        {
            amount.Value = (22.3f * caster.GetTotalAttackPowerValue(WeaponAttackType.BaseAttack)) * ((_mExtraSpellCost + 200) / 600.0f);
            var mNewRage = caster.GetPower(PowerType.Rage) - _mExtraSpellCost;

            if (mNewRage < 0)
                mNewRage = 0;

            caster.SetPower(PowerType.Rage, mNewRage);
            /*if (Player* player = caster->ToPlayer())
                player->SendPowerUpdate(PowerType.Rage, m_newRage);*/
        }
    }

    private double OnAbsorb(AuraEffect unnamedParameter, DamageInfo dmgInfo, double unnamedParameter2)
    {
        var caster = Caster;

        if (caster != null)
        {
            var spell = new SpellNonMeleeDamage(caster, caster, SpellInfo, new SpellCastVisual(0, 0), SpellSchoolMask.Normal);
            spell.Damage = dmgInfo.Damage - dmgInfo.Damage * 0.9f;
            spell.CleanDamage = spell.Damage;
            caster.DealSpellDamage(spell, false);
            caster.SendSpellNonMeleeDamageLog(spell);
        }

        return unnamedParameter2;
    }
}