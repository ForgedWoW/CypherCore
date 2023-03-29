// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Warlock;

// Incinerate - 29722
[SpellScript(29722)]
public class spell_warl_incinerate : SpellScript, IHasSpellEffects
{
    double _brimstoneDamage = 0;
    public List<ISpellEffect> SpellEffects { get; } = new();

    public override void Register()
    {
        SpellEffects.Add(new EffectHandler(HandleOnHitMainTarget, 0, SpellEffectName.Dummy, SpellScriptHookType.EffectHitTarget));
        SpellEffects.Add(new EffectHandler(HandleOnHitTarget, 1, SpellEffectName.SchoolDamage, SpellScriptHookType.EffectHitTarget));
    }

    private void HandleOnHitMainTarget(int UnnamedParameter)
    {
        Caster.CastSpell(WarlockSpells.INCINERATE_ENERGIZE, true);

        if (IsHitCrit)
            Caster.ModifyPower(PowerType.SoulShards, 10);
    }

    private void HandleOnHitTarget(int UnnamedParameter)
    {
        var target = HitUnit;
        var caster = Caster;

        if (target == null || caster == null)
            return;

        DiabolicEmbers(target);
        FireAndBrimstone(target, caster);
        RoaringBlaze(target, caster);
    }

    private void DiabolicEmbers(Unit caster)
    {
        if (caster.HasAura(WarlockSpells.DIABOLIC_EMBERS))
            caster.CastSpell(WarlockSpells.INCINERATE_ENERGIZE, true);
    }

    private void FireAndBrimstone(Unit target, Unit caster)
    {
        if (!caster.TryGetAura(WarlockSpells.FIRE_AND_BRIMSTONE, out var fab))
        {
            if (target != ExplTargetUnit)
            {
                PreventHitDamage();

                return;
            }
        }
        else
        {
            if (target != ExplTargetUnit)
            {
                if (_brimstoneDamage == 0)
                    _brimstoneDamage = MathFunctions.CalculatePct(HitDamage, fab.GetEffect(0).BaseAmount);

                HitDamage = _brimstoneDamage;
            }
        }
    }

    private void RoaringBlaze(Unit target, Unit caster)
    {
        if (caster.HasAura(WarlockSpells.ROARING_BLAZE) && ExplTargetUnit == target)
        {
            var aur = target.GetAura(WarlockSpells.IMMOLATE_DOT, caster.GUID);
            var dmgEff = Global.SpellMgr.GetSpellInfo(WarlockSpells.ROARING_BLASE_DMG_PCT, Difficulty.None)?.GetEffect(0);

            if (aur != null && dmgEff != null)
            {
                var dmg = HitDamage;
                HitDamage = MathFunctions.AddPct(ref dmg, dmgEff.BasePoints);
            }
        }
    }
}