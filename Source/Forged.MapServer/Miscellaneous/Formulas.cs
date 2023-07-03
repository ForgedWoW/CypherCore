// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using Forged.MapServer.DataStorage;
using Forged.MapServer.Entities.Players;
using Forged.MapServer.Entities.Units;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.IFormula;
using Framework.Constants;
using Framework.Util;
using Microsoft.Extensions.Configuration;

namespace Forged.MapServer.Miscellaneous;

public class Formulas
{
    private readonly CliDB _cliDB;
    private readonly IConfiguration _configuration;
    private readonly ScriptManager _scriptManager;

    public Formulas(CliDB cliDB, ScriptManager scriptManager, IConfiguration configuration)
    {
        _cliDB = cliDB;
        _scriptManager = scriptManager;
        _configuration = configuration;
    }

    public uint BaseGain(uint plLevel, uint mobLevel)
    {
        uint baseGain;

        var xpPlayer = _cliDB.XpGameTable.GetRow(plLevel);
        var xpMob = _cliDB.XpGameTable.GetRow(mobLevel);

        if (mobLevel >= plLevel)
        {
            var nLevelDiff = mobLevel - plLevel;

            if (nLevelDiff > 4)
                nLevelDiff = 4;

            baseGain = (uint)Math.Round(xpPlayer.PerKill * (1 + 0.05f * nLevelDiff));
        }
        else
        {
            var grayLevel = GetGrayLevel(plLevel);

            if (mobLevel > grayLevel)
            {
                var zd = GetZeroDifference(plLevel);
                baseGain = (uint)Math.Round(xpMob.PerKill * ((1 - (plLevel - mobLevel) / zd) * (xpMob.Divisor / xpPlayer.Divisor)));
            }
            else
                baseGain = 0;
        }

        if (_configuration.GetDefaultValue("MinCreatureScaledXPRatio", 0) != 0 && plLevel != mobLevel)
        {
            // Use mob level instead of player level to avoid overscaling on gain in a min is enforced
            var baseGainMin = BaseGain(plLevel, plLevel) * _configuration.GetDefaultValue("MinCreatureScaledXPRatio", 0u) / 100;
            baseGain = Math.Max(baseGainMin, baseGain);
        }

        _scriptManager.ForEach<IFormulaOnBaseGainCalculation>(p => p.OnBaseGainCalculation(baseGain, plLevel, mobLevel));

        return baseGain;
    }

    public uint BgConquestRatingCalculator(uint rate)
    {
        // WowWiki: Battlegroundratings receive a bonus of 22.2% to the cap they generate
        return (uint)(ConquestRatingCalculator(rate) * 1.222f + 0.5f);
    }

    public uint ConquestRatingCalculator(uint rate)
    {
        switch (rate)
        {
            case <= 1500:
                return 1350; // Default conquest points
            case > 3000:
                rate = 3000;

                break;
        }

        // http://www.arenajunkies.com/topic/179536-conquest-point-cap-vs-personal-rating-chart/page__st__60#entry3085246
        return (uint)(1.4326 * (1511.26 / (1 + 1639.28 / Math.Exp(0.00412 * rate)) + 850.15));
    }

    public XPColorChar GetColorCode(uint plLevel, uint mobLevel)
    {
        XPColorChar color;

        if (mobLevel >= plLevel + 5)
            color = XPColorChar.Red;
        else if (mobLevel >= plLevel + 3)
            color = XPColorChar.Orange;
        else if (mobLevel >= plLevel - 2)
            color = XPColorChar.Yellow;
        else if (mobLevel > GetGrayLevel(plLevel))
            color = XPColorChar.Green;
        else
            color = XPColorChar.Gray;

        _scriptManager.ForEach<IFormulaOnColorCodeCaclculation>(p => p.OnColorCodeCalculation(color, plLevel, mobLevel));

        return color;
    }

    public uint GetGrayLevel(uint plLevel)
    {
        uint level;

        switch (plLevel)
        {
            case < 7:
                level = 0;

                break;
            case < 35:
            {
                byte count = 0;

                for (var i = 15; i <= plLevel; ++i)
                    if (i % 5 == 0)
                        ++count;

                level = (uint)(plLevel - 7 - (count - 1));

                break;
            }
            default:
                level = plLevel - 10;

                break;
        }

        _scriptManager.ForEach<IFormulaOnGrayLevelCalculation>(p => p.OnGrayLevelCalculation(level, plLevel));

        return level;
    }

    public uint GetZeroDifference(uint plLevel)
    {
        uint diff = plLevel switch
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

        _scriptManager.ForEach<IFormulaOnZeroDifference>(p => p.OnZeroDifferenceCalculation(diff, plLevel));

        return diff;
    }

    public uint HkHonorAtLevel(uint level, float multiplier = 1.0f)
    {
        return (uint)Math.Ceiling(HkHonorAtLevelF(level, multiplier));
    }

    public float HkHonorAtLevelF(uint level, float multiplier = 1.0f)
    {
        var honor = multiplier * level * 1.55f;
        _scriptManager.ForEach<IFormulaOnHonorCalculation>(p => p.OnHonorCalculation(honor, level, multiplier));

        return honor;
    }

    public uint XPGain(Player player, Unit u, bool isBattleGround = false)
    {
        var creature = u.AsCreature;
        uint gain = 0;

        if (creature == null || creature.CanGiveExperience)
        {
            var xpMod = 1.0f;

            gain = BaseGain(player.Level, u.GetLevelForTarget(player));

            if (gain != 0 && creature != null)
            {
                // Players get only 10% xp for killing creatures of lower expansion levels than himself
                if (_configuration.GetDefaultValue("player:lowerExpInLowerExpansions", true) && creature.Template.GetHealthScalingExpansion() < (int)GetExpansionForLevel(player.Level))
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

            xpMod *= isBattleGround ? _configuration.GetDefaultValue("Rate:XP:BattlegroundKill", 1.0f) : _configuration.GetDefaultValue("Rate:XP:Kill", 1.0f);

            if (creature != null && creature.PlayerDamageReq != 0) // if players dealt less than 50% of the damage and were credited anyway (due to CREATURE_FLAG_EXTRA_NO_PLAYER_DAMAGE_REQ), scale XP gained appropriately (linear scaling)
                xpMod *= 1.0f - 2.0f * creature.PlayerDamageReq / creature.MaxHealth;

            gain = (uint)(gain * xpMod);
        }

        _scriptManager.ForEach<IFormulaOnGainCalculation>(p => p.OnGainCalculation(gain, player, u));

        return gain;
    }

    public float XPInGroupRate(uint count, bool isRaid)
    {
        float rate;

        if (isRaid)
            // TODO: Must apply decrease modifiers depending on raid size.
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

        _scriptManager.ForEach<IFormulaOnGroupRateCaclulation>(p => p.OnGroupRateCalculation(rate, count, isRaid));

        return rate;
    }

    private Expansion GetExpansionForLevel(uint level)
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