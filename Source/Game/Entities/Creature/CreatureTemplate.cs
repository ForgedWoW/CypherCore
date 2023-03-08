// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using Framework.Constants;
using Game.Networking.Packets;

namespace Game.Entities;

public class CreatureTemplate
{
	public uint Entry;
	public uint[] DifficultyEntry = new uint[SharedConst.MaxCreatureDifficulties];
	public uint[] KillCredit = new uint[SharedConst.MaxCreatureKillCredit];
	public List<CreatureModel> Models = new();
	public string Name;
	public string FemaleName;
	public string SubName;
	public string TitleAlt;
	public string IconName;
	public uint GossipMenuId;
	public short Minlevel;
	public Dictionary<Difficulty, CreatureLevelScaling> scalingStorage = new();
	public short Maxlevel;
	public int HealthScalingExpansion;
	public uint RequiredExpansion;
	public uint VignetteID; // @todo Read Vignette.db2
	public uint Faction;
	public ulong Npcflag;
	public float SpeedWalk;
	public float SpeedRun;
	public float Scale;
	public CreatureEliteType Rank;
	public uint DmgSchool;
	public uint BaseAttackTime;
	public uint RangeAttackTime;
	public float BaseVariance;
	public float RangeVariance;
	public uint UnitClass;
	public UnitFlags UnitFlags;
	public uint UnitFlags2;
	public uint UnitFlags3;
	public uint DynamicFlags;
	public CreatureFamily Family;
	public Class TrainerClass;
	public CreatureType CreatureType;
	public CreatureTypeFlags TypeFlags;
	public uint TypeFlags2;
	public uint LootId;
	public uint PickPocketId;
	public uint SkinLootId;
	public int[] Resistance = new int[7];
	public uint[] Spells = new uint[8];
	public uint VehicleId;
	public uint MinGold;
	public uint MaxGold;
	public string AIName;
	public uint MovementType;
	public CreatureMovementData Movement = new();
	public float HoverHeight;
	public float ModHealth;
	public float ModHealthExtra;
	public float ModMana;
	public float ModManaExtra;
	public float ModArmor;
	public float ModDamage;
	public float ModExperience;
	public bool RacialLeader;
	public uint MovementId;
	public int CreatureDifficultyID;
	public int WidgetSetID;
	public int WidgetSetUnitConditionID;
	public bool RegenHealth;
	public ulong MechanicImmuneMask;
	public uint SpellSchoolImmuneMask;
	public CreatureFlagsExtra FlagsExtra;
	public uint ScriptID;
	public string StringId;

	public QueryCreatureResponse QueryData;

	public CreatureModel GetModelByIdx(int idx)
	{
		return idx < Models.Count ? Models[idx] : null;
	}

	public CreatureModel GetRandomValidModel()
	{
		if (Models.Empty())
			return null;

		// If only one element, ignore the Probability (even if 0)
		if (Models.Count == 1)
			return Models[0];

		var selectedItr = Models.SelectRandomElementByWeight(model => { return model.Probability; });

		return selectedItr;
	}

	public CreatureModel GetFirstValidModel()
	{
		foreach (var model in Models)
			if (model.CreatureDisplayId != 0)
				return model;

		return null;
	}

	public CreatureModel GetModelWithDisplayId(uint displayId)
	{
		foreach (var model in Models)
			if (displayId == model.CreatureDisplayId)
				return model;

		return null;
	}

	public CreatureModel GetFirstInvisibleModel()
	{
		foreach (var model in Models)
		{
			var modelInfo = Global.ObjectMgr.GetCreatureModelInfo(model.CreatureDisplayId);

			if (modelInfo != null && modelInfo.IsTrigger)
				return model;
		}

		return CreatureModel.DefaultInvisibleModel;
	}

	public CreatureModel GetFirstVisibleModel()
	{
		foreach (var model in Models)
		{
			var modelInfo = Global.ObjectMgr.GetCreatureModelInfo(model.CreatureDisplayId);

			if (modelInfo != null && !modelInfo.IsTrigger)
				return model;
		}

		return CreatureModel.DefaultVisibleModel;
	}

	public int[] GetMinMaxLevel()
	{
		return new[]
		{
			HealthScalingExpansion != (int)Expansion.LevelCurrent ? Minlevel : Minlevel + SharedConst.MaxLevel, HealthScalingExpansion != (int)Expansion.LevelCurrent ? Maxlevel : Maxlevel + SharedConst.MaxLevel
		};
	}

	public int GetHealthScalingExpansion()
	{
		return HealthScalingExpansion == (int)Expansion.LevelCurrent ? WorldConfig.GetIntValue(WorldCfg.Expansion) : HealthScalingExpansion;
	}

	public SkillType GetRequiredLootSkill()
	{
		if (TypeFlags.HasAnyFlag(CreatureTypeFlags.SkinWithHerbalism))
			return SkillType.Herbalism;
		else if (TypeFlags.HasAnyFlag(CreatureTypeFlags.SkinWithMining))
			return SkillType.Mining;
		else if (TypeFlags.HasAnyFlag(CreatureTypeFlags.SkinWithEngineering))
			return SkillType.Engineering;
		else
			return SkillType.Skinning; // normal case
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

	public static int DifficultyIDToDifficultyEntryIndex(uint difficulty)
	{
		switch ((Difficulty)difficulty)
		{
			case Difficulty.None:
			case Difficulty.Normal:
			case Difficulty.Raid10N:
			case Difficulty.Raid40:
			case Difficulty.Scenario3ManN:
			case Difficulty.NormalRaid:
				return -1;
			case Difficulty.Heroic:
			case Difficulty.Raid25N:
			case Difficulty.Scenario3ManHC:
			case Difficulty.HeroicRaid:
				return 0;
			case Difficulty.Raid10HC:
			case Difficulty.MythicKeystone:
			case Difficulty.MythicRaid:
				return 1;
			case Difficulty.Raid25HC:
				return 2;
			case Difficulty.LFR:
			case Difficulty.LFRNew:
			case Difficulty.EventRaid:
			case Difficulty.EventDungeon:
			case Difficulty.EventScenario:
			default:
				return -1;
		}
	}

	public void InitializeQueryData()
	{
		QueryData = new QueryCreatureResponse();

		QueryData.CreatureID = Entry;
		QueryData.Allow = true;

		CreatureStats stats = new();
		stats.Leader = RacialLeader;

		stats.Name[0] = Name;
		stats.NameAlt[0] = FemaleName;

		stats.Flags[0] = (uint)TypeFlags;
		stats.Flags[1] = TypeFlags2;

		stats.CreatureType = (int)CreatureType;
		stats.CreatureFamily = (int)Family;
		stats.Classification = (int)Rank;

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

		var items = Global.ObjectMgr.GetCreatureQuestItemList(Entry);

		if (items != null)
			stats.QuestItems.AddRange(items);

		QueryData.Stats = stats;
	}

	public CreatureLevelScaling GetLevelScaling(Difficulty difficulty)
	{
		var creatureLevelScaling = scalingStorage.LookupByKey(difficulty);

		if (creatureLevelScaling != null)
			return creatureLevelScaling;

		return new CreatureLevelScaling();
	}
}