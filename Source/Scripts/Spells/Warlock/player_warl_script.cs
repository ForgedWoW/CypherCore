// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using Forged.MapServer.Entities.Players;
using Forged.MapServer.Entities.Units;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.IPlayer;
using Forged.MapServer.Scripting.Interfaces.IUnit;
using Forged.MapServer.Spells;
using Framework.Constants;

namespace Scripts.Spells.Warlock;

[Script]
internal class PlayerWarlScript : ScriptObjectAutoAdd, IPlayerOnModifyPower, IPlayerOnDealDamage, IUnitOnDamage
{
    public PlayerWarlScript() : base("player_warl_script") { }
    public PlayerClass PlayerClass { get; } = PlayerClass.Warlock;

    public void OnDamage(Player caster, Unit target, ref double damage, SpellInfo spellProto)
    {
        if (spellProto == null ||
            spellProto.DmgClass != SpellDmgClass.Magic ||
            !caster.TryGetAura(WarlockSpells.MASTERY_CHAOTIC_ENERGIES, out var chaoticEnergies))
            return;

        var dmgPct = chaoticEnergies.GetEffect(0).Amount / 2;

        MathFunctions.AddPct(ref damage, dmgPct);

        if (caster.HasAura(WarlockSpells.MASTERY_CHAOTIC_ENERGIES) &&
            (spellProto.Id == WarlockSpells.CHAOS_BOLT ||
             spellProto.Id == WarlockSpells.SHADOWBURN ||
             spellProto.Id == WarlockSpells.RAIN_OF_FIRE_DAMAGE))
            MathFunctions.AddPct(ref damage, dmgPct);
        else
        {
            var rand = RandomHelper.DRand(0, dmgPct);

            if (rand != 0)
                MathFunctions.AddPct(ref damage, rand);
        }
    }

    public void OnModifyPower(Player player, PowerType power, int oldValue, ref int newValue, bool regen)
    {
        if (power != PowerType.SoulShards)
            return;

        var shardCost = oldValue - newValue;

        if (shardCost < 0)
        {
            var shardGain = shardCost * -1;

            var lastCount = player.VariableStorage.GetValue("filledSoulShard", 0) + shardGain;

            if (lastCount > 10)
            {
                lastCount = lastCount % 10; // buff only applies to the rounded save remainder for next proc.

                DemonicInsperation(player);
                WrathfulMnion(player);
            }

            player.VariableStorage.Set("filledSoulShard", lastCount);
        }

        if (!regen)
        {
            PowerOverwhelming(player, shardCost);
            RitualOfRuin(player, shardCost);
            RainOfChaos(player, shardCost);
            GrandWarlocksDesign(player, shardCost);
        }
    }

    public void OnDamage(Unit attacker, Unit victim, ref double damage)
    {
        if (!attacker.TryGetAura(WarlockSpells.FEL_ARMOR, out var felAura) || !felAura.TryGetEffect(3, out var auraEffect))
            return;

        damage -= MathFunctions.CalculatePct(damage, auraEffect.Amount / 10);
    }

    private static void DemonicInsperation(Player player)
    {
        if (player.HasAura(WarlockSpells.DEMONIC_INSPIRATION) && player.TryGetPet(out var pet))
            pet.AddAura(WarlockSpells.DEMONIC_INSPIRATION_PET_AURA);
    }

    private static void WrathfulMnion(Player player)
    {
        if (player.HasAura(WarlockSpells.WRATHFUL_MINION) && player.TryGetPet(out var pet))
            pet.AddAura(WarlockSpells.WRATHFUL_MINION_PET_AURA);
    }

    private void GrandWarlocksDesign(Player player, int shardCost)
    {
        if (shardCost > 0 && player.TryGetAura(WarlockSpells.GRAND_WARLOCKS_DESIGN, out var grandDesign))
            for (var i = 0; i < shardCost; i++)
                switch (player.GetPrimarySpecialization())
                {
                    case TalentSpecialization.WarlockAffliction:
                        player.SpellHistory.ModifyCooldown(WarlockSpells.SUMMON_DARKGLARE, TimeSpan.FromMilliseconds(-grandDesign.GetEffect(0).Amount));

                        break;

                    case TalentSpecialization.WarlockDemonology:
                        player.SpellHistory.ModifyCooldown(WarlockSpells.SUMMON_DEMONIC_TYRANT, TimeSpan.FromMilliseconds(-grandDesign.GetEffect(1).Amount));

                        break;

                    case TalentSpecialization.WarlockDestruction:
                        player.SpellHistory.ModifyCooldown(WarlockSpells.SUMMON_INFERNAL, TimeSpan.FromMilliseconds(-grandDesign.GetEffect(2).Amount));

                        break;
                }
    }

    private void RainOfChaos(Player player, int shardCost)
    {
        if (shardCost > 0 && player.TryGetAura(WarlockSpells.RAIN_OF_CHAOS, out var raidOfChaos))
            for (var i = 0; i < shardCost; i++)
                if (RandomHelper.randChance(raidOfChaos.GetEffect(0).Amount))
                    player.SpellFactory.CastSpell(WarlockSpells.RAIN_OF_CHAOS_INFERNAL, true);
    }

    private void PowerOverwhelming(Player player, int shardCost)
    {
        if (shardCost <= 0 || !player.HasAura(WarlockSpells.POWER_OVERWHELMING))
            return;

        var cost = shardCost / 10;

        for (var i = 0; i < cost; i++)
            player.AddAura(WarlockSpells.POWER_OVERWHELMING_AURA, player);
    }

    private void RitualOfRuin(Player player, int shardCost)
    {
        if (shardCost <= 0 || !player.HasAura(WarlockSpells.RITUAL_OF_RUIN))
            return;

        var soulShardsSpent = player.VariableStorage.GetValue(WarlockSpells.RITUAL_OF_RUIN.ToString(), 0) + shardCost;
        var needed = (int)Global.SpellMgr.GetSpellInfo(WarlockSpells.RITUAL_OF_RUIN).GetEffect(0).BasePoints * 10; // each soul shard is 10

        if (player.TryGetAura(WarlockSpells.MASTER_RITUALIST, out var masterRitualist))
            needed += masterRitualist.GetEffect(0).AmountAsInt; // note this number is negitive so we add it.

        if (soulShardsSpent > needed)
        {
            player.AddAura(WarlockSpells.RITUAL_OF_RUIN_FREE_CAST_AURA, player);
            soulShardsSpent -= needed;
        }

        player.VariableStorage.Set(WarlockSpells.RITUAL_OF_RUIN.ToString(), soulShardsSpent);
    }
}