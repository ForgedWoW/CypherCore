﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Entities.Units;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.IAura;
using Forged.MapServer.Spells;
using Forged.MapServer.Spells.Auras;
using Framework.Constants;

namespace Scripts.Spells.Monk;

[Script] // 115069 - Stagger
internal class SpellMonkStagger : AuraScript, IHasAuraEffects
{
    public List<IAuraEffectHandler> AuraEffects { get; } = new();


    public override void Register()
    {
        AuraEffects.Add(new AuraEffectAbsorbHandler(AbsorbNormal, 1, false, AuraScriptHookType.EffectAbsorb));
        AuraEffects.Add(new AuraEffectAbsorbHandler(AbsorbMagic, 2, false, AuraScriptHookType.EffectAbsorb));
    }

    public static Aura FindExistingStaggerEffect(Unit unit)
    {
        var auraLight = unit.GetAura(MonkSpells.STAGGER_LIGHT);

        if (auraLight != null)
            return auraLight;

        var auraModerate = unit.GetAura(MonkSpells.STAGGER_MODERATE);

        if (auraModerate != null)
            return auraModerate;

        var auraHeavy = unit.GetAura(MonkSpells.STAGGER_HEAVY);

        if (auraHeavy != null)
            return auraHeavy;

        return null;
    }

    private double AbsorbNormal(AuraEffect aurEff, DamageInfo dmgInfo, double absorbAmount)
    {
        Absorb(dmgInfo, 1.0f);

        return absorbAmount;
    }

    private double AbsorbMagic(AuraEffect aurEff, DamageInfo dmgInfo, double absorbAmount)
    {
        var effect = GetEffect(4);

        if (effect == null)
            return absorbAmount;

        Absorb(dmgInfo, effect.Amount / 100.0f);

        return absorbAmount;
    }

    private void Absorb(DamageInfo dmgInfo, double multiplier)
    {
        // Prevent default Action (which would remove the aura)
        PreventDefaultAction();

        // make sure Damage doesn't come from stagger Damage spell STAGGER_DAMAGE_AURA
        var dmgSpellInfo = dmgInfo.SpellInfo;

        if (dmgSpellInfo != null)
            if (dmgSpellInfo.Id == MonkSpells.STAGGER_DAMAGE_AURA)
                return;

        var effect = GetEffect(0);

        if (effect == null)
            return;

        var target = Target;
        var agility = target.GetStat(Stats.Agility);
        var baseAmount = MathFunctions.CalculatePct(agility, effect.Amount);
        var k = Global.DB2Mgr.EvaluateExpectedStat(ExpectedStatType.ArmorConstant, target.Level, -2, 0, target.Class);

        var newAmount = (baseAmount / (baseAmount + k));
        newAmount *= multiplier;

        // Absorb X percentage of the Damage
        var absorbAmount = dmgInfo.Damage * newAmount;

        if (absorbAmount > 0)
        {
            dmgInfo.AbsorbDamage(absorbAmount);

            // Cast stagger and make it tick on each tick
            AddAndRefreshStagger(absorbAmount);
        }
    }

    private void AddAndRefreshStagger(double amount)
    {
        var target = Target;
        var auraStagger = FindExistingStaggerEffect(target);

        if (auraStagger != null)
        {
            var effStaggerRemaining = auraStagger.GetEffect(1);

            if (effStaggerRemaining == null)
                return;

            var newAmount = effStaggerRemaining.Amount + amount;
            var spellId = GetStaggerSpellId(target, newAmount);

            if (spellId == effStaggerRemaining.SpellInfo.Id)
            {
                auraStagger.RefreshDuration();
                effStaggerRemaining.ChangeAmount((int)newAmount, false, true /* reapply */);
            }
            else
            {
                // amount changed the stagger Type so we need to change the stagger amount (e.g. from medium to light)
                Target.RemoveAura(auraStagger);
                AddNewStagger(target, spellId, newAmount);
            }
        }
        else
        {
            AddNewStagger(target, GetStaggerSpellId(target, amount), amount);
        }
    }

    private uint GetStaggerSpellId(Unit unit, double amount)
    {
        const double staggerHeavy = 0.6f;
        const double staggerModerate = 0.3f;

        var staggerPct = amount / unit.MaxHealth;

        return (staggerPct >= staggerHeavy)    ? MonkSpells.STAGGER_HEAVY :
               (staggerPct >= staggerModerate) ? MonkSpells.STAGGER_MODERATE :
                                                 MonkSpells.STAGGER_LIGHT;
    }

    private void AddNewStagger(Unit unit, uint staggerSpellId, double staggerAmount)
    {
        // We only set the total stagger amount. The amount per tick will be set by the stagger spell script
        unit.SpellFactory.CastSpell(unit, staggerSpellId, new CastSpellExtraArgs(SpellValueMod.BasePoint1, (int)staggerAmount).SetTriggerFlags(TriggerCastFlags.FullMask));
    }
}