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
    public string AIName;
    public uint BaseAttackTime;
    public float BaseVariance;
    public int CreatureDifficultyID;
    public CreatureType CreatureType;
    public uint[] DifficultyEntry = new uint[SharedConst.MaxCreatureDifficulties];
    public uint DmgSchool;
    public uint DynamicFlags;
    public uint Entry;
    public uint Faction;
    public CreatureFamily Family;
    public string FemaleName;
    public CreatureFlagsExtra FlagsExtra;
    public uint GossipMenuId;
    public int HealthScalingExpansion;
    public float HoverHeight;
    public string IconName;
    public uint[] KillCredit = new uint[SharedConst.MaxCreatureKillCredit];
    public uint LootId;
    public uint MaxGold;
    public short Maxlevel;
    public ulong MechanicImmuneMask;
    public uint MinGold;
    public short Minlevel;
    public float ModArmor;
    public float ModDamage;
    public List<CreatureModel> Models = new();
    public float ModExperience;
    public float ModHealth;
    public float ModHealthExtra;
    public float ModMana;
    public float ModManaExtra;
    public CreatureMovementData Movement;
    public uint MovementId;
    public uint MovementType;
    public string Name;
    public ulong Npcflag;
    public uint PickPocketId;
    public QueryCreatureResponse QueryData;
    public bool RacialLeader;
    public uint RangeAttackTime;
    public float RangeVariance;
    public CreatureEliteType Rank;
    public bool RegenHealth;
    public uint RequiredExpansion;
    public int[] Resistance = new int[7];
    public float Scale;
    public Dictionary<Difficulty, CreatureLevelScaling> scalingStorage = new();
    public uint ScriptID;
    public uint SkinLootId;
    public float SpeedRun;
    public float SpeedWalk;
    public uint[] Spells = new uint[8];
    public uint SpellSchoolImmuneMask;
    public string StringId;
    public string SubName;
    public string TitleAlt;
    public PlayerClass TrainerClass;
    public CreatureTypeFlags TypeFlags;
    public uint TypeFlags2;
    public uint UnitClass;
    public UnitFlags UnitFlags;
    public uint UnitFlags2;
    public uint UnitFlags3;
    public uint VehicleId;
    public uint VignetteID; // @todo Read Vignette.db2
    public int WidgetSetID;
    public int WidgetSetUnitConditionID;
    private readonly IConfiguration _configuration;
    private readonly GameObjectManager _objectManager;

    public CreatureTemplate(IConfiguration configuration, GameObjectManager objectManager)
    {
        _configuration = configuration;
        _objectManager = objectManager;
        Movement = new CreatureMovementData(_configuration);
    }

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
            var modelInfo = _objectManager.GetCreatureModelInfo(model.CreatureDisplayId);

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
            var modelInfo = _objectManager.GetCreatureModelInfo(model.CreatureDisplayId);

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
        var creatureLevelScaling = scalingStorage.LookupByKey(difficulty);

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