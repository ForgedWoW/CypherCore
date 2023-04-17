// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using Forged.MapServer.Entities.Units;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces;
using Forged.MapServer.Scripting.Interfaces.ISpell;
using Framework.Constants;

namespace Scripts.Spells.Warlock;

// 17962 - Conflagrate
[SpellScript(17962)]
public class SpellWarlConflagrate : SpellScript, IHasSpellEffects
{
    public List<ISpellEffect> SpellEffects { get; } = new();


    public override void Register()
    {
        SpellEffects.Add(new EffectHandler(HandleHit, 0, SpellEffectName.SchoolDamage, SpellScriptHookType.EffectHitTarget));
    }

    private void HandleHit(int unnamedParameter)
    {
        var caster = Caster;
        var target = HitUnit;

        if (caster == null || target == null)
            return;

        caster.ModifyPower(PowerType.SoulShards, 75);

        ConflagrationOfChaos(caster, target);
        Backdraft(caster);
        RoaringBlaze(caster, target);
        Decimation(caster, target);
    }

    private void ConflagrationOfChaos(Unit caster, Unit target)
    {
        caster.RemoveAura(WarlockSpells.CONFLAGRATION_OF_CHAOS_CONFLAGRATE);

        if (caster.TryGetAura(WarlockSpells.CONFLAGRATION_OF_CHAOS, out var conflagrate))
            if (RandomHelper.randChance(conflagrate.GetEffect(0).BaseAmount))
                caster.SpellFactory.CastSpell(WarlockSpells.CONFLAGRATION_OF_CHAOS_CONFLAGRATE, true);
    }

    private void Decimation(Unit caster, Unit target)
    {
        if (caster.TryGetAura(WarlockSpells.DECIMATION, out var dec) && target.HealthBelowPct(dec.GetEffect(1).BaseAmount))
            caster.SpellHistory.ModifyCooldown(WarlockSpells.SOUL_FIRE, TimeSpan.FromMilliseconds(dec.GetEffect(0).BaseAmount));
    }

    private void Backdraft(Unit caster)
    {
        if (caster.HasAura(WarlockSpells.BACKDRAFT_AURA))
            caster.SpellFactory.CastSpell(caster, WarlockSpells.BACKDRAFT, true);
    }

    private void RoaringBlaze(Unit caster, Unit target)
    {
        if (caster.HasAura(WarlockSpells.ROARING_BLAZE))
        {
            var aur = target.GetAura(WarlockSpells.IMMOLATE_DOT, caster.GUID);

            if (aur != null)
            {
                var aurEff = aur.GetEffect(0);
                var dmgEff = Global.SpellMgr.GetSpellInfo(WarlockSpells.ROARING_BLASE_DMG_PCT, Difficulty.None)?.GetEffect(0);

                if (aurEff != null && dmgEff != null)
                {
                    var damage = aurEff.Amount;
                    aurEff.SetAmount(MathFunctions.AddPct(ref damage, dmgEff.BasePoints));
                    aur.SetNeedClientUpdateForTargets();
                }
            }
        }
    }
}