﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using Forged.MapServer.Entities.AreaTriggers;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces;
using Forged.MapServer.Scripting.Interfaces.ISpell;
using Forged.MapServer.Spells;
using Framework.Constants;

namespace Scripts.Spells.DemonHunter;

[SpellScript(228477)]
public class SpellDhSoulCleave : SpellScript, IHasSpellEffects
{
    public List<ISpellEffect> SpellEffects { get; } = new();

    public override void Register()
    {
        SpellEffects.Add(new EffectHandler(HandleDummy, 0, SpellEffectName.Dummy, SpellScriptHookType.EffectHitTarget));
        SpellEffects.Add(new EffectHandler(HandleHeal, 3, SpellEffectName.Heal, SpellScriptHookType.EffectHitTarget));
    }

    private void HandleHeal(int effIndex)
    {
        var caster = Caster;

        if (caster == null)
            return;

        if (caster.TypeId != TypeId.Player)
            return;

        if (caster.HasAura(DemonHunterSpells.FEAST_OF_SOULS))
            caster.SpellFactory.CastSpell(caster, DemonHunterSpells.FEAST_OF_SOULS_HEAL, true);
    }

    private void HandleDummy(int effIndex)
    {
        var caster = Caster;

        if (caster == null)
            return;

        // Consume all soul fragments in 25 yards;
        var fragments = new List<List<AreaTrigger>>();
        fragments.Add(caster.GetAreaTriggers(ShatteredSoulsSpells.SHATTERED_SOULS));
        fragments.Add(caster.GetAreaTriggers(ShatteredSoulsSpells.SHATTERED_SOULS_DEMON));
        fragments.Add(caster.GetAreaTriggers(ShatteredSoulsSpells.LESSER_SOUL_SHARD));
        var range = (float)EffectInfo.BasePoints;

        foreach (var vec in fragments)
        {
            foreach (var at in vec)
            {
                if (!caster.IsWithinDist(at, range))
                    continue;

                var tempSumm = caster.SummonCreature(SharedConst.WorldTrigger, at.Location.X, at.Location.Y, at.Location.Z, 0, TempSummonType.TimedDespawn, TimeSpan.FromSeconds(100));

                if (tempSumm != null)
                {
                    tempSumm.Faction = caster.Faction;
                    tempSumm.SetSummonerGUID(caster.GUID);
                    var bp = 0;

                    switch (at.Template.Id.Id)
                    {
                        case 6007:
                        case 5997:
                            bp = (int)ShatteredSoulsSpells.SOUL_FRAGMENT_HEAL_VENGEANCE;

                            break;
                        case 6710:
                            bp = (int)ShatteredSoulsSpells.LESSER_SOUL_SHARD_HEAL;

                            break;
                    }

                    caster.SpellFactory.CastSpell(tempSumm, ShatteredSoulsSpells.CONSUME_SOUL_MISSILE, new CastSpellExtraArgs(TriggerCastFlags.FullMask).AddSpellMod(SpellValueMod.BasePoint0, (int)bp));

                    if (at.Template.Id.Id == 6007)
                        caster.SpellFactory.CastSpell(caster, ShatteredSoulsSpells.SOUL_FRAGMENT_DEMON_BONUS, true);

                    if (caster.HasAura(DemonHunterSpells.FEED_THE_DEMON))
                        caster.SpellHistory.ModifyCooldown(Global.SpellMgr.GetSpellInfo(DemonHunterSpells.DEMON_SPIKES, Difficulty.None).ChargeCategoryId, TimeSpan.FromMilliseconds(-1000));

                    if (caster.HasAura(ShatteredSoulsSpells.PAINBRINGER))
                        caster.SpellFactory.CastSpell(caster, ShatteredSoulsSpells.PAINBRINGER_BUFF, true);

                    var soulBarrier = caster.GetAuraEffect(DemonHunterSpells.SOUL_BARRIER, 0);

                    if (soulBarrier != null)
                    {
                        var amount = soulBarrier.Amount + ((double)(Global.SpellMgr.GetSpellInfo(DemonHunterSpells.SOUL_BARRIER, Difficulty.None).GetEffect(1).BasePoints) / 100.0f) * caster.GetTotalAttackPowerValue(WeaponAttackType.BaseAttack);
                        soulBarrier.SetAmount(amount);
                    }

                    at.SetDuration(0);
                }
            }
        }
    }
}