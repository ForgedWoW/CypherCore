// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using Forged.MapServer.DataStorage;
using Forged.MapServer.Entities.Players;
using Forged.MapServer.Entities.Units;
using Forged.MapServer.Scripting.Interfaces.IFormula;
using Framework.Constants;

namespace Forged.MapServer.Miscellaneous;

public class Formulas
{
    public static uint BaseGain(uint pl_level, uint mob_level)
    {
        uint baseGain;

        var xpPlayer = CliDB.XpGameTable.GetRow(pl_level);
        var xpMob = CliDB.XpGameTable.GetRow(mob_level);

        if (mob_level >= pl_level)
        {
            var nLevelDiff = mob_level - pl_level;

            if (nLevelDiff > 4)
                nLevelDiff = 4;

            baseGain = (uint)Math.Round(xpPlayer.PerKill * (1 + 0.05f * nLevelDiff));
        }
        else
        {
            var gray_level = GetGrayLevel(pl_level);

            if (mob_level > gray_level)
            {
                var ZD = GetZeroDifference(pl_level);
                baseGain = (uint)Math.Round(xpMob.PerKill * ((1 - (pl_level - mob_level) / ZD) * (xpMob.Divisor / xpPlayer.Divisor)));
            }
            else
            {
                baseGain = 0;
            }
        }

        if (GetDefaultValue("MinCreatureScaledXPRatio", 0) != 0 && pl_level != mob_level)
        {
            // Use mob level instead of player level to avoid overscaling on gain in a min is enforced
            var baseGainMin = BaseGain(pl_level, pl_level) * GetDefaultValue("MinCreatureScaledXPRatio", 0) / 100;
            baseGain = Math.Max(baseGainMin, baseGain);
        }

        ScriptManager.ForEach<IFormulaOnBaseGainCalculation>(p => p.OnBaseGainCalculation(baseGain, pl_level, mob_level));

        return baseGain;
    }

    public static uint BgConquestRatingCalculator(uint rate)
    {
        // WowWiki: Battlegroundratings receive a bonus of 22.2% to the cap they generate
        return (uint)(ConquestRatingCalculator(rate) * 1.222f + 0.5f);
    }

    public static uint ConquestRatingCalculator(uint rate)
    {
        if (rate <= 1500)
            return 1350; // Default conquest points
        else if (rate > 3000)
            rate = 3000;

        // http://www.arenajunkies.com/topic/179536-conquest-point-cap-vs-personal-rating-chart/page__st__60#entry3085246
        return (uint)(1.4326 * (1511.26 / (1 + 1639.28 / Math.Exp(0.00412 * rate)) + 850.15));
    }

    public static XPColorChar GetColorCode(uint pl_level, uint mob_level)
    {
        XPColorChar color;

        if (mob_level >= pl_level + 5)
            color = XPColorChar.Red;
        else if (mob_level >= pl_level + 3)
            color = XPColorChar.Orange;
        else if (mob_level >= pl_level - 2)
            color = XPColorChar.Yellow;
        else if (mob_level > GetGrayLevel(pl_level))
            color = XPColorChar.Green;
        else
            color = XPColorChar.Gray;

        ScriptManager.ForEach<IFormulaOnColorCodeCaclculation>(p => p.OnColorCodeCalculation(color, pl_level, mob_level));

        return color;
    }

    public static uint GetGrayLevel(uint pl_level)
    {
        uint level;

        if (pl_level < 7)
        {
            level = 0;
        }
        else if (pl_level < 35)
        {
            byte count = 0;

            for (var i = 15; i <= pl_level; ++i)
                if (i % 5 == 0)
                    ++count;

            level = (uint)(pl_level - 7 - (count - 1));
        }
        else
        {
            level = pl_level - 10;
        }

        ScriptManager.ForEach<IFormulaOnGrayLevelCalculation>(p => p.OnGrayLevelCalculation(level, pl_level));

        return level;
    }

    public static uint GetZeroDifference(uint pl_level)
    {
        uint diff = pl_level switch
        {
            < 4  => 5,
            < 10 => 6,
            < 12 => 7,
            < 16 => 8,
            < 20 => 9,
            < 30 => 11,
            < 40 => 12,
            < 45 => 13,
            < 50 => 14,
            < 55 => 15,
            < 60 => 16,
            _    => 17
        };

        ScriptManager.ForEach<IFormulaOnZeroDifference>(p => p.OnZeroDifferenceCalculation(diff, pl_level));

        return diff;
    }

    public static uint HKHonorAtLevel(uint level, float multiplier = 1.0f)
    {
        return (uint)Math.Ceiling(HKHonorAtLevelF(level, multiplier));
    }

    public static float HKHonorAtLevelF(uint level, float multiplier = 1.0f)
    {
        var honor = multiplier * level * 1.55f;
        ScriptManager.ForEach<IFormulaOnHonorCalculation>(p => p.OnHonorCalculation(honor, level, multiplier));

        return honor;
    }
    public static uint XPGain(Player player, Unit u, bool isBattleGround = false)
    {
        var creature = u.AsCreature;
        uint gain = 0;

        if (!creature || creature.CanGiveExperience)
        {
            var xpMod = 1.0f;

            gain = BaseGain(player.Level, u.GetLevelForTarget(player));

            if (gain != 0 && creature)
            {
                // Players get only 10% xp for killing creatures of lower expansion levels than himself
                if (ConfigMgr.GetDefaultValue("player.lowerExpInLowerExpansions", true) && creature.Template.GetHealthScalingExpansion() < (int)GetExpansionForLevel(player.Level))
                    gain = (uint)Math.Round(gain / 10.0f);

                if (creature.IsElite)
                {
                    // Elites in instances have a 2.75x XP bonus instead of the regular 2x world bonus.
                    if (u.Location.Map.IsDungeon)
                        xpMod *= 2.75f;
                    else
                        xpMod *= 2.0f;
                }

                xpMod *= creature.Template.ModExperience;
            }

            xpMod *= isBattleGround ? GetDefaultValue("Rate.XP.BattlegroundKill", 1.0f) : GetDefaultValue("Rate.XP.Kill", 1.0f);

            if (creature && creature.PlayerDamageReq != 0) // if players dealt less than 50% of the damage and were credited anyway (due to CREATURE_FLAG_EXTRA_NO_PLAYER_DAMAGE_REQ), scale XP gained appropriately (linear scaling)
                xpMod *= 1.0f - 2.0f * creature.PlayerDamageReq / creature.MaxHealth;

            gain = (uint)(gain * xpMod);
        }

        ScriptManager.ForEach<IFormulaOnGainCalculation>(p => p.OnGainCalculation(gain, player, u));

        return gain;
    }

    public static float XPInGroupRate(uint count, bool isRaid)
    {
        float rate;

        if (isRaid)
            // FIXME: Must apply decrease modifiers depending on raid size.
            // set to < 1 to, so client will display raid related strings
            rate = 0.99f;
        else
            rate = count switch
            {
                0 => 1.0f,
                1 => 1.0f,
                2 => 1.0f,
                3 => 1.166f,
                4 => 1.3f,
                5 => 1.4f,
                _ => 1.4f
            };

        ScriptManager.ForEach<IFormulaOnGroupRateCaclulation>(p => p.OnGroupRateCalculation(rate, count, isRaid));

        return rate;
    }
    private static Expansion GetExpansionForLevel(uint level)
    {
        return level switch
        {
            < 60  => Expansion.Classic,
            < 70  => Expansion.BurningCrusade,
            < 80  => Expansion.WrathOfTheLichKing,
            < 85  => Expansion.Cataclysm,
            < 90  => Expansion.MistsOfPandaria,
            < 100 => Expansion.WarlordsOfDraenor,
            _     => Expansion.Legion
        };
    }
}