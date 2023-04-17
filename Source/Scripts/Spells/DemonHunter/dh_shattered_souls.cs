// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using Forged.MapServer.Entities.Creatures;
using Forged.MapServer.Entities.Players;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.IPlayer;
using Framework.Constants;

namespace Scripts.Spells.DemonHunter;

[Script]
public class DhShatteredSouls : ScriptObjectAutoAdd, IPlayerOnCreatureKill
{
    public DhShatteredSouls() : base("dh_shattered_souls") { }

    public void OnCreatureKill(Player player, Creature victim)
    {
        if (player.Class != PlayerClass.DemonHunter)
            return;

        var fragmentPos = victim.GetRandomNearPosition(5.0f);

        if (victim.CreatureType == CreatureType.Demon && RandomHelper.randChance(30))
        {
            player.SpellFactory.CastSpell(ShatteredSoulsSpells.SHATTERED_SOULS_MISSILE, true);
            victim.SpellFactory.CastSpell(ShatteredSoulsSpells.SHATTERED_SOULS_DEMON, true);     //at
            player.SpellFactory.CastSpell(ShatteredSoulsSpells.SOUL_FRAGMENT_DEMON_BONUS, true); //buff
        }

        if (victim.CreatureType != CreatureType.Demon && RandomHelper.randChance(30))
        {
            victim.SpellFactory.CastSpell(ShatteredSoulsSpells.SHATTERED_SOULS_MISSILE, true);
            player.SpellFactory.CastSpell(fragmentPos, ShatteredSoulsSpells.SHATTERED_SOULS, true); //10665
        }

        if (player.HasAura(DemonHunterSpells.FEED_THE_DEMON))
            player.SpellHistory.ModifyCooldown(Global.SpellMgr.GetSpellInfo(DemonHunterSpells.DEMON_SPIKES, Difficulty.None).ChargeCategoryId, TimeSpan.FromMilliseconds(-1000));

        if (player.HasAura(ShatteredSoulsSpells.PAINBRINGER))
            player.SpellFactory.CastSpell(player, ShatteredSoulsSpells.PAINBRINGER_BUFF, true);

        var soulBarrier = player.GetAuraEffect(DemonHunterSpells.SOUL_BARRIER, 0);

        if (soulBarrier != null)
        {
            var amount = soulBarrier.Amount + ((double)(Global.SpellMgr.GetSpellInfo(DemonHunterSpells.SOUL_BARRIER, Difficulty.None).GetEffect(1).BasePoints) / 100.0f) * player.GetTotalAttackPowerValue(WeaponAttackType.BaseAttack);
            soulBarrier.SetAmount(amount);
        }
    }
}