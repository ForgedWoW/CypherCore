// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using System.Linq;
using Forged.MapServer.Globals;
using Forged.MapServer.Networking.Packets.Query;
using Framework.Constants;
using Framework.Util;
using Microsoft.Extensions.Configuration;

namespace Forged.MapServer.Entities.Creatures;

public class CreatureTemplate
{
    private readonly IConfiguration _configuration;
    private readonly GameObjectManager _objectManager;

    public CreatureTemplate(IConfiguration configuration, GameObjectManager objectManager)
    {
        _configuration = configuration;
        _objectManager = objectManager;
        Movement = new CreatureMovementData(_configuration);
    }

    public string AIName { get; set; }
    public uint BaseAttackTime { get; set; }
    public float BaseVariance { get; set; }
    public int CreatureDifficultyID { get; set; }
    public CreatureType CreatureType { get; set; }
    public uint[] DifficultyEntry { get; set; } = new uint[SharedConst.MaxCreatureDifficulties];
    public uint DmgSchool { get; set; }
    public uint DynamicFlags { get; set; }
    public uint Entry { get; set; }
    public uint Faction { get; set; }
    public CreatureFamily Family { get; set; }
    public string FemaleName { get; set; }
    public CreatureFlagsExtra FlagsExtra { get; set; }
    public uint GossipMenuId { get; set; }
    public int HealthScalingExpansion { get; set; }
    public float HoverHeight { get; set; }
    public string IconName { get; set; }
    public uint[] KillCredit { get; set; } = new uint[SharedConst.MaxCreatureKillCredit];
    public uint LootId { get; set; }
    public uint MaxGold { get; set; }
    public short Maxlevel { get; set; }
    public ulong MechanicImmuneMask { get; set; }
    public uint MinGold { get; set; }
    public short Minlevel { get; set; }
    public float ModArmor { get; set; }
    public float ModDamage { get; set; }
    public List<CreatureModel> Models { get; set; } = new();
    public float ModExperience { get; set; }
    public float ModHealth { get; set; }
    public float ModHealthExtra { get; set; }
    public float ModMana { get; set; }
    public float ModManaExtra { get; set; }
    public CreatureMovementData Movement { get; set; }
    public uint MovementId { get; set; }
    public uint MovementType { get; set; }
    public string Name { get; set; }
    public ulong Npcflag { get; set; }
    public uint PickPocketId { get; set; }
    public QueryCreatureResponse QueryData { get; set; }
    public bool RacialLeader { get; set; }
    public uint RangeAttackTime { get; set; }
    public float RangeVariance { get; set; }
    public CreatureEliteType Rank { get; set; }
    public bool RegenHealth { get; set; }
    public uint RequiredExpansion { get; set; }
    public int[] Resistance { get; set; } = new int[7];
    public float Scale { get; set; }
    public Dictionary<Difficulty, CreatureLevelScaling> ScalingStorage { get; set; } = new();
    public uint ScriptID { get; set; }
    public uint SkinLootId { get; set; }
    public float SpeedRun { get; set; }
    public float SpeedWalk { get; set; }
    public uint[] Spells { get; set; } = new uint[8];
    public uint SpellSchoolImmuneMask { get; set; }
    public string StringId { get; set; }
    public string SubName { get; set; }
    public string TitleAlt { get; set; }
    public PlayerClass TrainerClass { get; set; }
    public CreatureTypeFlags TypeFlags { get; set; }
    public uint TypeFlags2 { get; set; }
    public uint UnitClass { get; set; }
    public UnitFlags UnitFlags { get; set; }
    public uint UnitFlags2 { get; set; }
    public uint UnitFlags3 { get; set; }
    public uint VehicleId { get; set; }
    public uint VignetteID { get; set; } // @todo Read Vignette.db2
    public int WidgetSetID { get; set; }
    public int WidgetSetUnitConditionID { get; set; }

    public static int DifficultyIDToDifficultyEntryIndex(uint difficulty)
    {
        return (Difficulty)difficulty switch
        {
            Difficulty.None           => -1,
            Difficulty.Normal         => -1,
            Difficulty.Raid10N        => -1,
            Difficulty.Raid40         => -1,
            Difficulty.Scenario3ManN  => -1,
            Difficulty.NormalRaid     => -1,
            Difficulty.Heroic         => 0,
            Difficulty.Raid25N        => 0,
            Difficulty.Scenario3ManHC => 0,
            Difficulty.HeroicRaid     => 0,
            Difficulty.Raid10HC       => 1,
            Difficulty.MythicKeystone => 1,
            Difficulty.MythicRaid     => 1,
            Difficulty.Raid25HC       => 2,
            Difficulty.LFR            => -1,
            Difficulty.LFRNew         => -1,
            Difficulty.EventRaid      => -1,
            Difficulty.EventDungeon   => -1,
            Difficulty.EventScenario  => -1,
            _                         => -1
        };
    }

    public CreatureModel GetFirstInvisibleModel()
    {
        foreach (var model in Models)
        {
            var modelInfo = _objectManager.CreatureModelCache.GetCreatureModelInfo(model.CreatureDisplayId);

            if (modelInfo is { IsTrigger: true })
                return model;
        }

        return CreatureModel.DefaultInvisibleModel;
    }

    public CreatureModel GetFirstValidModel()
    {
        return Models.FirstOrDefault(model => model.CreatureDisplayId != 0);
    }

    public CreatureModel GetFirstVisibleModel()
    {
        foreach (var model in Models)
        {
            var modelInfo = _objectManager.CreatureModelCache.GetCreatureModelInfo(model.CreatureDisplayId);

            if (modelInfo is { IsTrigger: false })
                return model;
        }

        return CreatureModel.DefaultVisibleModel;
    }

    public int GetHealthScalingExpansion()
    {
        return HealthScalingExpansion == (int)Expansion.LevelCurrent ? _configuration.GetDefaultValue("Expansion", (int)Expansion.Dragonflight) : HealthScalingExpansion;
    }

    public CreatureLevelScaling GetLevelScaling(Difficulty difficulty)
    {
        var creatureLevelScaling = ScalingStorage.LookupByKey(difficulty);

        return creatureLevelScaling ?? new CreatureLevelScaling();
    }

    public int[] GetMinMaxLevel()
    {
        return new[]
        {
            HealthScalingExpansion != (int)Expansion.LevelCurrent ? Minlevel : Minlevel + SharedConst.MaxLevel, HealthScalingExpansion != (int)Expansion.LevelCurrent ? Maxlevel : Maxlevel + SharedConst.MaxLevel
        };
    }

    public CreatureModel GetModelByIdx(int idx)
    {
        return idx < Models.Count ? Models[idx] : null;
    }

    public CreatureModel GetModelWithDisplayId(uint displayId)
    {
        return Models.FirstOrDefault(model => displayId == model.CreatureDisplayId);
    }

    public CreatureModel GetRandomValidModel()
    {
        if (Models.Empty())
            return null;

        // If only one element, ignore the Probability (even if 0)
        return Models.Count == 1 ? Models[0] : Models.SelectRandomElementByWeight(model => model.Probability);
    }

    public SkillType GetRequiredLootSkill()
    {
        if (TypeFlags.HasAnyFlag(CreatureTypeFlags.SkinWithHerbalism))
            return SkillType.Herbalism;

        if (TypeFlags.HasAnyFlag(CreatureTypeFlags.SkinWithMining))
            return SkillType.Mining;

        return TypeFlags.HasAnyFlag(CreatureTypeFlags.SkinWithEngineering) ? SkillType.Engineering : SkillType.Skinning; // normal case
    }

    public void InitializeQueryData()
    {
        QueryData = new QueryCreatureResponse
        {
            CreatureID = Entry,
            Allow = true
        };

        CreatureStats stats = new()
        {
            Leader = RacialLeader,
            Name =
            {
                [0] = Name
            },
            NameAlt =
            {
                [0] = FemaleName
            },
            Flags =
            {
                [0] = (uint)TypeFlags,
                [1] = TypeFlags2
            },
            CreatureType = (int)CreatureType,
            CreatureFamily = (int)Family,
            Classification = (int)Rank
        };

        for (uint i = 0; i < SharedConst.MaxCreatureKillCredit; ++i)
            stats.ProxyCreatureID[i] = KillCredit[i];

        foreach (var model in Models)
        {
            stats.Display.TotalProbability += model.Probability;
            stats.Display.CreatureDisplay.Add(new CreatureXDisplay(model.CreatureDisplayId, model.DisplayScale, model.Probability));
        }

        stats.HpMulti = ModHealth;
        stats.EnergyMulti = ModMana;

        stats.CreatureMovementInfoID = MovementId;
        stats.RequiredExpansion = RequiredExpansion;
        stats.HealthScalingExpansion = HealthScalingExpansion;
        stats.VignetteID = VignetteID;
        stats.Class = (int)UnitClass;
        stats.CreatureDifficultyID = CreatureDifficultyID;
        stats.WidgetSetID = WidgetSetID;
        stats.WidgetSetUnitConditionID = WidgetSetUnitConditionID;

        stats.Title = SubName;
        stats.TitleAlt = TitleAlt;
        stats.CursorName = IconName;

        var items = _objectManager.GetCreatureQuestItemList(Entry);

        if (items != null)
            stats.QuestItems.AddRange(items);

        QueryData.Stats = stats;
    }

    public bool IsExotic()
    {
        return (TypeFlags & CreatureTypeFlags.TameableExotic) != 0;
    }

    public bool IsTameable(bool canTameExotic)
    {
        if (CreatureType != CreatureType.Beast || Family == CreatureFamily.None || !TypeFlags.HasAnyFlag(CreatureTypeFlags.Tameable))
            return false;

        // if can tame exotic then can tame any tameable
        return canTameExotic || !IsExotic();
    }
}