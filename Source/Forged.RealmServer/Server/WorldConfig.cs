// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using Framework.Constants;
using Forged.RealmServer.Scripting.Interfaces.IWorld;
using Microsoft.Extensions.Configuration;
using Framework.Util;
using Serilog;
using Forged.RealmServer.Scripting;

namespace Forged.RealmServer;

public class WorldConfig
{
	private readonly IConfiguration _configuration;
    private readonly ScriptManager _scriptManager;

    public readonly Dictionary<WorldCfg, object> Values = new();

    public WorldConfig(IConfiguration configuration, ScriptManager scriptManager)
	{
        _configuration = configuration;
        _scriptManager = scriptManager;
    }

    public void Load(bool reload = false)
	{
		// Read support system setting from the config file
		Values[WorldCfg.SupportEnabled] = _configuration.GetDefaultValue("Support.Enabled", true);
		Values[WorldCfg.SupportTicketsEnabled] = _configuration.GetDefaultValue("Support.TicketsEnabled", false);
		Values[WorldCfg.SupportBugsEnabled] = _configuration.GetDefaultValue("Support.BugsEnabled", false);
		Values[WorldCfg.SupportComplaintsEnabled] = _configuration.GetDefaultValue("Support.ComplaintsEnabled", false);
		Values[WorldCfg.SupportSuggestionsEnabled] = _configuration.GetDefaultValue("Support.SuggestionsEnabled", false);

		// Send server info on login?
		Values[WorldCfg.EnableSinfoLogin] = _configuration.GetDefaultValue("Server.LoginInfo", 0);

		// Read all rates from the config file
		void SetRegenRate(WorldCfg rate, string configKey)
		{
			Values[rate] = _configuration.GetDefaultValue(configKey, 1.0f);

			if ((float)Values[rate] < 0.0f)
			{
				Log.Logger.Error("{0} ({1}) must be > 0. Using 1 instead.", configKey, Values[rate]);
				Values[rate] = 1;
			}
		}

		SetRegenRate(WorldCfg.RateHealth, "Rate.Health");
		SetRegenRate(WorldCfg.RatePowerMana, "Rate.Mana");
		SetRegenRate(WorldCfg.RatePowerRageIncome, "Rate.Rage.Gain");
		SetRegenRate(WorldCfg.RatePowerRageLoss, "Rate.Rage.Loss");
		SetRegenRate(WorldCfg.RatePowerFocus, "Rate.Focus");
		SetRegenRate(WorldCfg.RatePowerEnergy, "Rate.Energy");
		SetRegenRate(WorldCfg.RatePowerComboPointsLoss, "Rate.ComboPoints.Loss");
		SetRegenRate(WorldCfg.RatePowerRunicPowerIncome, "Rate.RunicPower.Gain");
		SetRegenRate(WorldCfg.RatePowerRunicPowerLoss, "Rate.RunicPower.Loss");
		SetRegenRate(WorldCfg.RatePowerSoulShards, "Rate.SoulShards.Loss");
		SetRegenRate(WorldCfg.RatePowerLunarPower, "Rate.LunarPower.Loss");
		SetRegenRate(WorldCfg.RatePowerHolyPower, "Rate.HolyPower.Loss");
		SetRegenRate(WorldCfg.RatePowerMaelstrom, "Rate.Maelstrom.Loss");
		SetRegenRate(WorldCfg.RatePowerChi, "Rate.Chi.Loss");
		SetRegenRate(WorldCfg.RatePowerInsanity, "Rate.Insanity.Loss");
		SetRegenRate(WorldCfg.RatePowerArcaneCharges, "Rate.ArcaneCharges.Loss");
		SetRegenRate(WorldCfg.RatePowerFury, "Rate.Fury.Loss");
		SetRegenRate(WorldCfg.RatePowerPain, "Rate.Pain.Loss");

		Values[WorldCfg.RateSkillDiscovery] = _configuration.GetDefaultValue("Rate.Skill.Discovery", 1.0f);
		Values[WorldCfg.RateDropItemPoor] = _configuration.GetDefaultValue("Rate.Drop.Item.Poor", 1.0f);
		Values[WorldCfg.RateDropItemNormal] = _configuration.GetDefaultValue("Rate.Drop.Item.Normal", 1.0f);
		Values[WorldCfg.RateDropItemUncommon] = _configuration.GetDefaultValue("Rate.Drop.Item.Uncommon", 1.0f);
		Values[WorldCfg.RateDropItemRare] = _configuration.GetDefaultValue("Rate.Drop.Item.Rare", 1.0f);
		Values[WorldCfg.RateDropItemEpic] = _configuration.GetDefaultValue("Rate.Drop.Item.Epic", 1.0f);
		Values[WorldCfg.RateDropItemLegendary] = _configuration.GetDefaultValue("Rate.Drop.Item.Legendary", 1.0f);
		Values[WorldCfg.RateDropItemArtifact] = _configuration.GetDefaultValue("Rate.Drop.Item.Artifact", 1.0f);
		Values[WorldCfg.RateDropItemReferenced] = _configuration.GetDefaultValue("Rate.Drop.Item.Referenced", 1.0f);
		Values[WorldCfg.RateDropItemReferencedAmount] = _configuration.GetDefaultValue("Rate.Drop.Item.ReferencedAmount", 1.0f);
		Values[WorldCfg.RateDropMoney] = _configuration.GetDefaultValue("Rate.Drop.Money", 1.0f);
		Values[WorldCfg.RateXpKill] = _configuration.GetDefaultValue("Rate.XP.Kill", 1.0f);
		Values[WorldCfg.RateXpBgKill] = _configuration.GetDefaultValue("Rate.XP.BattlegroundKill", 1.0f);
		Values[WorldCfg.RateXpQuest] = _configuration.GetDefaultValue("Rate.XP.Quest", 1.0f);
		Values[WorldCfg.RateXpExplore] = _configuration.GetDefaultValue("Rate.XP.Explore", 1.0f);

		Values[WorldCfg.XpBoostDaymask] = _configuration.GetDefaultValue("XP.Boost.Daymask", 0);
		Values[WorldCfg.RateXpBoost] = _configuration.GetDefaultValue("XP.Boost.Rate", 2.0f);

		Values[WorldCfg.RateRepaircost] = _configuration.GetDefaultValue("Rate.RepairCost", 1.0f);

		if ((float)Values[WorldCfg.RateRepaircost] < 0.0f)
		{
			Log.Logger.Error("Rate.RepairCost ({0}) must be >=0. Using 0.0 instead.", Values[WorldCfg.RateRepaircost]);
			Values[WorldCfg.RateRepaircost] = 0.0f;
		}

		Values[WorldCfg.RateReputationGain] = _configuration.GetDefaultValue("Rate.Reputation.Gain", 1.0f);
		Values[WorldCfg.RateReputationLowLevelKill] = _configuration.GetDefaultValue("Rate.Reputation.LowLevel.Kill", 1.0f);
		Values[WorldCfg.RateReputationLowLevelQuest] = _configuration.GetDefaultValue("Rate.Reputation.LowLevel.Quest", 1.0f);
		Values[WorldCfg.RateReputationRecruitAFriendBonus] = _configuration.GetDefaultValue("Rate.Reputation.RecruitAFriendBonus", 0.1f);
		Values[WorldCfg.RateCreatureNormalDamage] = _configuration.GetDefaultValue("Rate.Creature.Normal.Damage", 1.0f);
		Values[WorldCfg.RateCreatureEliteEliteDamage] = _configuration.GetDefaultValue("Rate.Creature.Elite.Elite.Damage", 1.0f);
		Values[WorldCfg.RateCreatureEliteRareeliteDamage] = _configuration.GetDefaultValue("Rate.Creature.Elite.RAREELITE.Damage", 1.0f);
		Values[WorldCfg.RateCreatureEliteWorldbossDamage] = _configuration.GetDefaultValue("Rate.Creature.Elite.WORLDBOSS.Damage", 1.0f);
		Values[WorldCfg.RateCreatureEliteRareDamage] = _configuration.GetDefaultValue("Rate.Creature.Elite.RARE.Damage", 1.0f);
		Values[WorldCfg.RateCreatureNormalHp] = _configuration.GetDefaultValue("Rate.Creature.Normal.HP", 1.0f);
		Values[WorldCfg.RateCreatureEliteEliteHp] = _configuration.GetDefaultValue("Rate.Creature.Elite.Elite.HP", 1.0f);
		Values[WorldCfg.RateCreatureEliteRareeliteHp] = _configuration.GetDefaultValue("Rate.Creature.Elite.RAREELITE.HP", 1.0f);
		Values[WorldCfg.RateCreatureEliteWorldbossHp] = _configuration.GetDefaultValue("Rate.Creature.Elite.WORLDBOSS.HP", 1.0f);
		Values[WorldCfg.RateCreatureEliteRareHp] = _configuration.GetDefaultValue("Rate.Creature.Elite.RARE.HP", 1.0f);
		Values[WorldCfg.RateCreatureNormalSpelldamage] = _configuration.GetDefaultValue("Rate.Creature.Normal.SpellDamage", 1.0f);
		Values[WorldCfg.RateCreatureEliteEliteSpelldamage] = _configuration.GetDefaultValue("Rate.Creature.Elite.Elite.SpellDamage", 1.0f);
		Values[WorldCfg.RateCreatureEliteRareeliteSpelldamage] = _configuration.GetDefaultValue("Rate.Creature.Elite.RAREELITE.SpellDamage", 1.0f);
		Values[WorldCfg.RateCreatureEliteWorldbossSpelldamage] = _configuration.GetDefaultValue("Rate.Creature.Elite.WORLDBOSS.SpellDamage", 1.0f);
		Values[WorldCfg.RateCreatureEliteRareSpelldamage] = _configuration.GetDefaultValue("Rate.Creature.Elite.RARE.SpellDamage", 1.0f);
		Values[WorldCfg.RateCreatureAggro] = _configuration.GetDefaultValue("Rate.Creature.Aggro", 1.0f);
		Values[WorldCfg.RateRestIngame] = _configuration.GetDefaultValue("Rate.Rest.InGame", 1.0f);
		Values[WorldCfg.RateRestOfflineInTavernOrCity] = _configuration.GetDefaultValue("Rate.Rest.Offline.InTavernOrCity", 1.0f);
		Values[WorldCfg.RateRestOfflineInWilderness] = _configuration.GetDefaultValue("Rate.Rest.Offline.InWilderness", 1.0f);
		Values[WorldCfg.RateDamageFall] = _configuration.GetDefaultValue("Rate.Damage.Fall", 1.0f);
		Values[WorldCfg.RateAuctionTime] = _configuration.GetDefaultValue("Rate.Auction.Time", 1.0f);
		Values[WorldCfg.RateAuctionDeposit] = _configuration.GetDefaultValue("Rate.Auction.Deposit", 1.0f);
		Values[WorldCfg.RateAuctionCut] = _configuration.GetDefaultValue("Rate.Auction.Cut", 1.0f);
		Values[WorldCfg.RateHonor] = _configuration.GetDefaultValue("Rate.Honor", 1.0f);
		Values[WorldCfg.RateInstanceResetTime] = _configuration.GetDefaultValue("Rate.InstanceResetTime", 1.0f);
		Values[WorldCfg.RateTalent] = _configuration.GetDefaultValue("Rate.Talent", 1.0f);

		if ((float)Values[WorldCfg.RateTalent] < 0.0f)
		{
			Log.Logger.Error("Rate.Talent ({0}) must be > 0. Using 1 instead.", Values[WorldCfg.RateTalent]);
			Values[WorldCfg.RateTalent] = 1.0f;
		}

		Values[WorldCfg.RateMovespeed] = _configuration.GetDefaultValue("Rate.MoveSpeed", 1.0f);

		if ((float)Values[WorldCfg.RateMovespeed] < 0.0f)
		{
			Log.Logger.Error("Rate.MoveSpeed ({0}) must be > 0. Using 1 instead.", Values[WorldCfg.RateMovespeed]);
			Values[WorldCfg.RateMovespeed] = 1.0f;
		}

		Values[WorldCfg.RateCorpseDecayLooted] = _configuration.GetDefaultValue("Rate.Corpse.Decay.Looted", 0.5f);

		Values[WorldCfg.RateDurabilityLossOnDeath] = _configuration.GetDefaultValue("DurabilityLoss.OnDeath", 10.0f);

		if ((float)Values[WorldCfg.RateDurabilityLossOnDeath] < 0.0f)
		{
			Log.Logger.Error("DurabilityLoss.OnDeath ({0}) must be >=0. Using 0.0 instead.", Values[WorldCfg.RateDurabilityLossOnDeath]);
			Values[WorldCfg.RateDurabilityLossOnDeath] = 0.0f;
		}

		if ((float)Values[WorldCfg.RateDurabilityLossOnDeath] > 100.0f)
		{
			Log.Logger.Error("DurabilityLoss.OnDeath ({0}) must be <= 100. Using 100.0 instead.", Values[WorldCfg.RateDurabilityLossOnDeath]);
			Values[WorldCfg.RateDurabilityLossOnDeath] = 0.0f;
		}

		Values[WorldCfg.RateDurabilityLossOnDeath] = (float)Values[WorldCfg.RateDurabilityLossOnDeath] / 100.0f;

		Values[WorldCfg.RateDurabilityLossDamage] = _configuration.GetDefaultValue("DurabilityLossChance.Damage", 0.5f);

		if ((float)Values[WorldCfg.RateDurabilityLossDamage] < 0.0f)
		{
			Log.Logger.Error("DurabilityLossChance.Damage ({0}) must be >=0. Using 0.0 instead.", Values[WorldCfg.RateDurabilityLossDamage]);
			Values[WorldCfg.RateDurabilityLossDamage] = 0.0f;
		}

		Values[WorldCfg.RateDurabilityLossAbsorb] = _configuration.GetDefaultValue("DurabilityLossChance.Absorb", 0.5f);

		if ((float)Values[WorldCfg.RateDurabilityLossAbsorb] < 0.0f)
		{
			Log.Logger.Error("DurabilityLossChance.Absorb ({0}) must be >=0. Using 0.0 instead.", Values[WorldCfg.RateDurabilityLossAbsorb]);
			Values[WorldCfg.RateDurabilityLossAbsorb] = 0.0f;
		}

		Values[WorldCfg.RateDurabilityLossParry] = _configuration.GetDefaultValue("DurabilityLossChance.Parry", 0.05f);

		if ((float)Values[WorldCfg.RateDurabilityLossParry] < 0.0f)
		{
			Log.Logger.Error("DurabilityLossChance.Parry ({0}) must be >=0. Using 0.0 instead.", Values[WorldCfg.RateDurabilityLossParry]);
			Values[WorldCfg.RateDurabilityLossParry] = 0.0f;
		}

		Values[WorldCfg.RateDurabilityLossBlock] = _configuration.GetDefaultValue("DurabilityLossChance.Block", 0.05f);

		if ((float)Values[WorldCfg.RateDurabilityLossBlock] < 0.0f)
		{
			Log.Logger.Error("DurabilityLossChance.Block ({0}) must be >=0. Using 0.0 instead.", Values[WorldCfg.RateDurabilityLossBlock]);
			Values[WorldCfg.RateDurabilityLossBlock] = 0.0f;
		}

		Values[WorldCfg.RateMoneyQuest] = _configuration.GetDefaultValue("Rate.Quest.Money.Reward", 1.0f);

		if ((float)Values[WorldCfg.RateMoneyQuest] < 0.0f)
		{
			Log.Logger.Error("Rate.Quest.Money.Reward ({0}) must be >=0. Using 0 instead.", Values[WorldCfg.RateMoneyQuest]);
			Values[WorldCfg.RateMoneyQuest] = 0.0f;
		}

		Values[WorldCfg.RateMoneyMaxLevelQuest] = _configuration.GetDefaultValue("Rate.Quest.Money.Max.Level.Reward", 1.0f);

		if ((float)Values[WorldCfg.RateMoneyMaxLevelQuest] < 0.0f)
		{
			Log.Logger.Error("Rate.Quest.Money.Max.Level.Reward ({0}) must be >=0. Using 0 instead.", Values[WorldCfg.RateMoneyMaxLevelQuest]);
			Values[WorldCfg.RateMoneyMaxLevelQuest] = 0.0f;
		}

		// Read other configuration items from the config file
		Values[WorldCfg.DurabilityLossInPvp] = _configuration.GetDefaultValue("DurabilityLoss.InPvP", false);

		Values[WorldCfg.Compression] = _configuration.GetDefaultValue("Compression", 1);

		if ((int)Values[WorldCfg.Compression] < 1 || (int)Values[WorldCfg.Compression] > 9)
		{
			Log.Logger.Error("Compression Level ({0}) must be in range 1..9. Using default compression Level (1).", Values[WorldCfg.Compression]);
			Values[WorldCfg.Compression] = 1;
		}

		Values[WorldCfg.AddonChannel] = _configuration.GetDefaultValue("AddonChannel", true);
		Values[WorldCfg.CleanCharacterDb] = _configuration.GetDefaultValue("CleanCharacterDB", false);
		Values[WorldCfg.PersistentCharacterCleanFlags] = _configuration.GetDefaultValue("PersistentCharacterCleanFlags", 0);
		Values[WorldCfg.AuctionReplicateDelay] = _configuration.GetDefaultValue("Auction.ReplicateItemsCooldown", 900);
		Values[WorldCfg.AuctionSearchDelay] = _configuration.GetDefaultValue("Auction.SearchDelay", 300);

		if ((int)Values[WorldCfg.AuctionSearchDelay] < 100 || (int)Values[WorldCfg.AuctionSearchDelay] > 10000)
		{
			Log.Logger.Error("Auction.SearchDelay ({0}) must be between 100 and 10000. Using default of 300ms", Values[WorldCfg.AuctionSearchDelay]);
			Values[WorldCfg.AuctionSearchDelay] = 300;
		}

		Values[WorldCfg.AuctionTaintedSearchDelay] = _configuration.GetDefaultValue("Auction.TaintedSearchDelay", 3000);

		if ((int)Values[WorldCfg.AuctionTaintedSearchDelay] < 100 || (int)Values[WorldCfg.AuctionTaintedSearchDelay] > 10000)
		{
			Log.Logger.Error($"Auction.TaintedSearchDelay ({Values[WorldCfg.AuctionTaintedSearchDelay]}) must be between 100 and 10000. Using default of 3s");
			Values[WorldCfg.AuctionTaintedSearchDelay] = 3000;
		}

		Values[WorldCfg.ChatChannelLevelReq] = _configuration.GetDefaultValue("ChatLevelReq.Channel", 1);
		Values[WorldCfg.ChatWhisperLevelReq] = _configuration.GetDefaultValue("ChatLevelReq.Whisper", 1);
		Values[WorldCfg.ChatEmoteLevelReq] = _configuration.GetDefaultValue("ChatLevelReq.Emote", 1);
		Values[WorldCfg.ChatSayLevelReq] = _configuration.GetDefaultValue("ChatLevelReq.Say", 1);
		Values[WorldCfg.ChatYellLevelReq] = _configuration.GetDefaultValue("ChatLevelReq.Yell", 1);
		Values[WorldCfg.PartyLevelReq] = _configuration.GetDefaultValue("PartyLevelReq", 1);
		Values[WorldCfg.TradeLevelReq] = _configuration.GetDefaultValue("LevelReq.Trade", 1);
		Values[WorldCfg.AuctionLevelReq] = _configuration.GetDefaultValue("LevelReq.Auction", 1);
		Values[WorldCfg.MailLevelReq] = _configuration.GetDefaultValue("LevelReq.Mail", 1);
		Values[WorldCfg.PreserveCustomChannels] = _configuration.GetDefaultValue("PreserveCustomChannels", false);
		Values[WorldCfg.PreserveCustomChannelDuration] = _configuration.GetDefaultValue("PreserveCustomChannelDuration", 14);
		Values[WorldCfg.PreserveCustomChannelInterval] = _configuration.GetDefaultValue("PreserveCustomChannelInterval", 5);
		Values[WorldCfg.GridUnload] = _configuration.GetDefaultValue("GridUnload", true);
		Values[WorldCfg.BasemapLoadGrids] = _configuration.GetDefaultValue("BaseMapLoadAllGrids", false);

		if ((bool)Values[WorldCfg.BasemapLoadGrids] && (bool)Values[WorldCfg.GridUnload])
		{
			Log.Logger.Error("BaseMapLoadAllGrids enabled, but GridUnload also enabled. GridUnload must be disabled to enable base map pre-loading. Base map pre-loading disabled");
			Values[WorldCfg.BasemapLoadGrids] = false;
		}

		Values[WorldCfg.InstancemapLoadGrids] = _configuration.GetDefaultValue("InstanceMapLoadAllGrids", false);

		if ((bool)Values[WorldCfg.InstancemapLoadGrids] && (bool)Values[WorldCfg.GridUnload])
		{
			Log.Logger.Error("InstanceMapLoadAllGrids enabled, but GridUnload also enabled. GridUnload must be disabled to enable instance map pre-loading. Instance map pre-loading disabled");
			Values[WorldCfg.InstancemapLoadGrids] = false;
		}

		Values[WorldCfg.IntervalSave] = _configuration.GetDefaultValue("PlayerSaveInterval", 15 * Time.Minute * Time.InMilliseconds);
		Values[WorldCfg.IntervalDisconnectTolerance] = _configuration.GetDefaultValue("DisconnectToleranceInterval", 0);
		Values[WorldCfg.StatsSaveOnlyOnLogout] = _configuration.GetDefaultValue("PlayerSave.Stats.SaveOnlyOnLogout", true);

		Values[WorldCfg.MinLevelStatSave] = _configuration.GetDefaultValue("PlayerSave.Stats.MinLevel", 0);

		if ((int)Values[WorldCfg.MinLevelStatSave] > SharedConst.MaxLevel)
		{
			Log.Logger.Error("PlayerSave.Stats.MinLevel ({0}) must be in range 0..80. Using default, do not save character stats (0).", Values[WorldCfg.MinLevelStatSave]);
			Values[WorldCfg.MinLevelStatSave] = 0;
		}

		Values[WorldCfg.IntervalGridclean] = _configuration.GetDefaultValue("GridCleanUpDelay", 5 * Time.Minute * Time.InMilliseconds);

		if ((int)Values[WorldCfg.IntervalGridclean] < MapConst.MinGridDelay)
		{
			Log.Logger.Error("GridCleanUpDelay ({0}) must be greater {1} Use this minimal value.", Values[WorldCfg.IntervalGridclean], MapConst.MinGridDelay);
			Values[WorldCfg.IntervalGridclean] = MapConst.MinGridDelay;
		}

		Values[WorldCfg.IntervalMapupdate] = _configuration.GetDefaultValue("MapUpdateInterval", 10);

		if ((int)Values[WorldCfg.IntervalMapupdate] < MapConst.MinMapUpdateDelay)
		{
			Log.Logger.Error("MapUpdateInterval ({0}) must be greater {1}. Use this minimal value.", Values[WorldCfg.IntervalMapupdate], MapConst.MinMapUpdateDelay);
			Values[WorldCfg.IntervalMapupdate] = MapConst.MinMapUpdateDelay;
		}

		Values[WorldCfg.IntervalChangeweather] = _configuration.GetDefaultValue("ChangeWeatherInterval", 10 * Time.Minute * Time.InMilliseconds);

		if (reload)
		{
			var val = _configuration.GetDefaultValue("WorldServerPort", 8085);

			if (val != (int)Values[WorldCfg.PortWorld])
				Log.Logger.Error("WorldServerPort option can't be changed at worldserver.conf reload, using current value ({0}).", Values[WorldCfg.PortWorld]);

			val = _configuration.GetDefaultValue("InstanceServerPort", 8086);

			if (val != (int)Values[WorldCfg.PortInstance])
				Log.Logger.Error("InstanceServerPort option can't be changed at worldserver.conf reload, using current value ({0}).", Values[WorldCfg.PortInstance]);
		}
		else
		{
			Values[WorldCfg.PortWorld] = _configuration.GetDefaultValue("WorldServerPort", 8085);
			Values[WorldCfg.PortInstance] = _configuration.GetDefaultValue("InstanceServerPort", 8086);
		}

		// Config values are in "milliseconds" but we handle SocketTimeOut only as "seconds" so divide by 1000
		Values[WorldCfg.SocketTimeoutTime] = _configuration.GetDefaultValue("SocketTimeOutTime", 900000) / 1000;
		Values[WorldCfg.SocketTimeoutTimeActive] = _configuration.GetDefaultValue("SocketTimeOutTimeActive", 60000) / 1000;
		Values[WorldCfg.SessionAddDelay] = _configuration.GetDefaultValue("SessionAddDelay", 10000);

		Values[WorldCfg.GroupXpDistance] = _configuration.GetDefaultValue("MaxGroupXPDistance", 74.0f);
		Values[WorldCfg.MaxRecruitAFriendDistance] = _configuration.GetDefaultValue("MaxRecruitAFriendBonusDistance", 100.0f);
		Values[WorldCfg.MinQuestScaledXpRatio] = _configuration.GetDefaultValue("MinQuestScaledXPRatio", 0);

		if ((int)Values[WorldCfg.MinQuestScaledXpRatio] > 100)
		{
			Log.Logger.Error($"MinQuestScaledXPRatio ({Values[WorldCfg.MinQuestScaledXpRatio]}) must be in range 0..100. Set to 0.");
			Values[WorldCfg.MinQuestScaledXpRatio] = 0;
		}

		Values[WorldCfg.MinCreatureScaledXpRatio] = _configuration.GetDefaultValue("MinCreatureScaledXPRatio", 0);

		if ((int)Values[WorldCfg.MinCreatureScaledXpRatio] > 100)
		{
			Log.Logger.Error($"MinCreatureScaledXPRatio ({Values[WorldCfg.MinCreatureScaledXpRatio]}) must be in range 0..100. Set to 0.");
			Values[WorldCfg.MinCreatureScaledXpRatio] = 0;
		}

		Values[WorldCfg.MinDiscoveredScaledXpRatio] = _configuration.GetDefaultValue("MinDiscoveredScaledXPRatio", 0);

		if ((int)Values[WorldCfg.MinDiscoveredScaledXpRatio] > 100)
		{
			Log.Logger.Error($"MinDiscoveredScaledXPRatio ({Values[WorldCfg.MinDiscoveredScaledXpRatio]}) must be in range 0..100. Set to 0.");
			Values[WorldCfg.MinDiscoveredScaledXpRatio] = 0;
		}

		/// @todo Add MonsterSight (with meaning) in worldserver.conf or put them as define
		Values[WorldCfg.SightMonster] = _configuration.GetDefaultValue("MonsterSight", 50.0f);

		Values[WorldCfg.RegenHpCannotReachTargetInRaid] = _configuration.GetDefaultValue("Creature.RegenHPCannotReachTargetInRaid", true);

		if (reload)
		{
			var val = _configuration.GetDefaultValue("GameType", 0);

			if (val != (int)Values[WorldCfg.GameType])
				Log.Logger.Error("GameType option can't be changed at worldserver.conf reload, using current value ({0}).", Values[WorldCfg.GameType]);
		}
		else
		{
			Values[WorldCfg.GameType] = _configuration.GetDefaultValue("GameType", 0);
		}

		if (reload)
		{
			var val = (int)_configuration.GetDefaultValue("RealmZone", RealmZones.Development);

			if (val != (int)Values[WorldCfg.RealmZone])
				Log.Logger.Error("RealmZone option can't be changed at worldserver.conf reload, using current value ({0}).", Values[WorldCfg.RealmZone]);
		}
		else
		{
			Values[WorldCfg.RealmZone] = _configuration.GetDefaultValue("RealmZone", (int)RealmZones.Development);
		}

		Values[WorldCfg.AllowTwoSideInteractionCalendar] = _configuration.GetDefaultValue("AllowTwoSide.Interaction.Calendar", false);
		Values[WorldCfg.AllowTwoSideInteractionChannel] = _configuration.GetDefaultValue("AllowTwoSide.Interaction.Channel", false);
		Values[WorldCfg.AllowTwoSideInteractionGroup] = _configuration.GetDefaultValue("AllowTwoSide.Interaction.Group", false);
		Values[WorldCfg.AllowTwoSideInteractionGuild] = _configuration.GetDefaultValue("AllowTwoSide.Interaction.Guild", false);
		Values[WorldCfg.AllowTwoSideInteractionAuction] = _configuration.GetDefaultValue("AllowTwoSide.Interaction.Auction", true);
		Values[WorldCfg.AllowTwoSideTrade] = _configuration.GetDefaultValue("AllowTwoSide.Trade", false);
		Values[WorldCfg.StrictPlayerNames] = _configuration.GetDefaultValue("StrictPlayerNames", 0);
		Values[WorldCfg.StrictCharterNames] = _configuration.GetDefaultValue("StrictCharterNames", 0);
		Values[WorldCfg.StrictPetNames] = _configuration.GetDefaultValue("StrictPetNames", 0);

		Values[WorldCfg.MinPlayerName] = _configuration.GetDefaultValue("MinPlayerName", 2);

		if ((int)Values[WorldCfg.MinPlayerName] < 1 || (int)Values[WorldCfg.MinPlayerName] > 12)
		{
			Log.Logger.Error("MinPlayerName ({0}) must be in range 1..{1}. Set to 2.", Values[WorldCfg.MinPlayerName], 12);
			Values[WorldCfg.MinPlayerName] = 2;
		}

		Values[WorldCfg.MinCharterName] = _configuration.GetDefaultValue("MinCharterName", 2);

		if ((int)Values[WorldCfg.MinCharterName] < 1 || (int)Values[WorldCfg.MinCharterName] > 24)
		{
			Log.Logger.Error("MinCharterName ({0}) must be in range 1..{1}. Set to 2.", Values[WorldCfg.MinCharterName], 24);
			Values[WorldCfg.MinCharterName] = 2;
		}

		Values[WorldCfg.MinPetName] = _configuration.GetDefaultValue("MinPetName", 2);

		if ((int)Values[WorldCfg.MinPetName] < 1 || (int)Values[WorldCfg.MinPetName] > 12)
		{
			Log.Logger.Error("MinPetName ({0}) must be in range 1..{1}. Set to 2.", Values[WorldCfg.MinPetName], 12);
			Values[WorldCfg.MinPetName] = 2;
		}

		Values[WorldCfg.CharterCostGuild] = _configuration.GetDefaultValue("Guild.CharterCost", 1000);
		Values[WorldCfg.CharterCostArena2v2] = _configuration.GetDefaultValue("ArenaTeam.CharterCost.2v2", 800000);
		Values[WorldCfg.CharterCostArena3v3] = _configuration.GetDefaultValue("ArenaTeam.CharterCost.3v3", 1200000);
		Values[WorldCfg.CharterCostArena5v5] = _configuration.GetDefaultValue("ArenaTeam.CharterCost.5v5", 2000000);

		Values[WorldCfg.CharacterCreatingDisabled] = _configuration.GetDefaultValue("CharacterCreating.Disabled", 0);
		Values[WorldCfg.CharacterCreatingDisabledRacemask] = _configuration.GetDefaultValue("CharacterCreating.Disabled.RaceMask", 0);
		Values[WorldCfg.CharacterCreatingDisabledClassmask] = _configuration.GetDefaultValue("CharacterCreating.Disabled.ClassMask", 0);

		Values[WorldCfg.CharactersPerRealm] = _configuration.GetDefaultValue("CharactersPerRealm", 60);

		if ((int)Values[WorldCfg.CharactersPerRealm] < 1 || (int)Values[WorldCfg.CharactersPerRealm] > 200)
		{
			Log.Logger.Error("CharactersPerRealm ({0}) must be in range 1..200. Set to 200.", Values[WorldCfg.CharactersPerRealm]);
			Values[WorldCfg.CharactersPerRealm] = 200;
		}

		// must be after CharactersPerRealm
		Values[WorldCfg.CharactersPerAccount] = _configuration.GetDefaultValue("CharactersPerAccount", 60);

		if ((int)Values[WorldCfg.CharactersPerAccount] < (int)Values[WorldCfg.CharactersPerRealm])
		{
			Log.Logger.Error("CharactersPerAccount ({0}) can't be less than CharactersPerRealm ({1}).", Values[WorldCfg.CharactersPerAccount], Values[WorldCfg.CharactersPerRealm]);
			Values[WorldCfg.CharactersPerAccount] = Values[WorldCfg.CharactersPerRealm];
		}

		Values[WorldCfg.CharacterCreatingEvokersPerRealm] = _configuration.GetDefaultValue("CharacterCreating.EvokersPerRealm", 1);

		if ((int)Values[WorldCfg.CharacterCreatingEvokersPerRealm] < 0 || (int)Values[WorldCfg.CharacterCreatingEvokersPerRealm] > 10)
		{
			Log.Logger.Error($"CharacterCreating.EvokersPerRealm ({Values[WorldCfg.CharacterCreatingEvokersPerRealm]}) must be in range 0..10. Set to 1.");
			Values[WorldCfg.CharacterCreatingEvokersPerRealm] = 1;
		}

		Values[WorldCfg.CharacterCreatingMinLevelForDemonHunter] = _configuration.GetDefaultValue("CharacterCreating.MinLevelForDemonHunter", 0);
		Values[WorldCfg.CharacterCreatingMinLevelForEvoker] = _configuration.GetDefaultValue("CharacterCreating.MinLevelForEvoker", 50);
		Values[WorldCfg.CharacterCreatingDisableAlliedRaceAchievementRequirement] = _configuration.GetDefaultValue("CharacterCreating.DisableAlliedRaceAchievementRequirement", false);

		Values[WorldCfg.SkipCinematics] = _configuration.GetDefaultValue("SkipCinematics", 0);

		if ((int)Values[WorldCfg.SkipCinematics] < 0 || (int)Values[WorldCfg.SkipCinematics] > 2)
		{
			Log.Logger.Error("SkipCinematics ({0}) must be in range 0..2. Set to 0.", Values[WorldCfg.SkipCinematics]);
			Values[WorldCfg.SkipCinematics] = 0;
		}

		if (reload)
		{
			var val = _configuration.GetDefaultValue("MaxPlayerLevel", SharedConst.DefaultMaxLevel);

			if (val != (int)Values[WorldCfg.MaxPlayerLevel])
				Log.Logger.Error("MaxPlayerLevel option can't be changed at config reload, using current value ({0}).", Values[WorldCfg.MaxPlayerLevel]);
		}
		else
		{
			Values[WorldCfg.MaxPlayerLevel] = _configuration.GetDefaultValue("MaxPlayerLevel", SharedConst.DefaultMaxLevel);
		}

		if ((int)Values[WorldCfg.MaxPlayerLevel] > SharedConst.MaxLevel)
		{
			Log.Logger.Error("MaxPlayerLevel ({0}) must be in range 1..{1}. Set to {1}.", Values[WorldCfg.MaxPlayerLevel], SharedConst.MaxLevel);
			Values[WorldCfg.MaxPlayerLevel] = SharedConst.MaxLevel;
		}

		Values[WorldCfg.MinDualspecLevel] = _configuration.GetDefaultValue("MinDualSpecLevel", 40);

		Values[WorldCfg.StartPlayerLevel] = _configuration.GetDefaultValue("StartPlayerLevel", 1);

		if ((int)Values[WorldCfg.StartPlayerLevel] < 1)
		{
			Log.Logger.Error("StartPlayerLevel ({0}) must be in range 1..MaxPlayerLevel({1}). Set to 1.", Values[WorldCfg.StartPlayerLevel], Values[WorldCfg.MaxPlayerLevel]);
			Values[WorldCfg.StartPlayerLevel] = 1;
		}
		else if ((int)Values[WorldCfg.StartPlayerLevel] > (int)Values[WorldCfg.MaxPlayerLevel])
		{
			Log.Logger.Error("StartPlayerLevel ({0}) must be in range 1..MaxPlayerLevel({1}). Set to {2}.", Values[WorldCfg.StartPlayerLevel], Values[WorldCfg.MaxPlayerLevel], Values[WorldCfg.MaxPlayerLevel]);
			Values[WorldCfg.StartPlayerLevel] = Values[WorldCfg.MaxPlayerLevel];
		}

		Values[WorldCfg.StartDeathKnightPlayerLevel] = _configuration.GetDefaultValue("StartDeathKnightPlayerLevel", 8);

		if ((int)Values[WorldCfg.StartDeathKnightPlayerLevel] < 1)
		{
			Log.Logger.Error(
						"StartDeathKnightPlayerLevel ({0}) must be in range 1..MaxPlayerLevel({1}). Set to 1.",
						Values[WorldCfg.StartDeathKnightPlayerLevel],
						Values[WorldCfg.MaxPlayerLevel]);

			Values[WorldCfg.StartDeathKnightPlayerLevel] = 1;
		}
		else if ((int)Values[WorldCfg.StartDeathKnightPlayerLevel] > (int)Values[WorldCfg.MaxPlayerLevel])
		{
			Log.Logger.Error(
						"StartDeathKnightPlayerLevel ({0}) must be in range 1..MaxPlayerLevel({1}). Set to {2}.",
						Values[WorldCfg.StartDeathKnightPlayerLevel],
						Values[WorldCfg.MaxPlayerLevel],
						Values[WorldCfg.MaxPlayerLevel]);

			Values[WorldCfg.StartDeathKnightPlayerLevel] = Values[WorldCfg.MaxPlayerLevel];
		}

		Values[WorldCfg.StartDemonHunterPlayerLevel] = _configuration.GetDefaultValue("StartDemonHunterPlayerLevel", 8);

		if ((int)Values[WorldCfg.StartDemonHunterPlayerLevel] < 1)
		{
			Log.Logger.Error(
						"StartDemonHunterPlayerLevel ({0}) must be in range 1..MaxPlayerLevel({1}). Set to 1.",
						Values[WorldCfg.StartDemonHunterPlayerLevel],
						Values[WorldCfg.MaxPlayerLevel]);

			Values[WorldCfg.StartDemonHunterPlayerLevel] = 1;
		}
		else if ((int)Values[WorldCfg.StartDemonHunterPlayerLevel] > (int)Values[WorldCfg.MaxPlayerLevel])
		{
			Log.Logger.Error(
						"StartDemonHunterPlayerLevel ({0}) must be in range 1..MaxPlayerLevel({1}). Set to {2}.",
						Values[WorldCfg.StartDemonHunterPlayerLevel],
						Values[WorldCfg.MaxPlayerLevel],
						Values[WorldCfg.MaxPlayerLevel]);

			Values[WorldCfg.StartDemonHunterPlayerLevel] = Values[WorldCfg.MaxPlayerLevel];
		}

		Values[WorldCfg.StartEvokerPlayerLevel] = _configuration.GetDefaultValue("StartEvokerPlayerLevel", 58);

		if ((int)Values[WorldCfg.StartEvokerPlayerLevel] < 1)
		{
			Log.Logger.Error($"StartEvokerPlayerLevel ({Values[WorldCfg.StartEvokerPlayerLevel]}) must be in range 1..MaxPlayerLevel({Values[WorldCfg.MaxPlayerLevel]}). Set to 1.");
			Values[WorldCfg.StartEvokerPlayerLevel] = 1;
		}
		else if ((int)Values[WorldCfg.StartEvokerPlayerLevel] > (int)Values[WorldCfg.MaxPlayerLevel])
		{
			Log.Logger.Error($"StartEvokerPlayerLevel ({Values[WorldCfg.StartEvokerPlayerLevel]}) must be in range 1..MaxPlayerLevel({Values[WorldCfg.MaxPlayerLevel]}). Set to {Values[WorldCfg.MaxPlayerLevel]}.");
			Values[WorldCfg.StartEvokerPlayerLevel] = Values[WorldCfg.MaxPlayerLevel];
		}

		Values[WorldCfg.StartAlliedRaceLevel] = _configuration.GetDefaultValue("StartAlliedRacePlayerLevel", 10);

		if ((int)Values[WorldCfg.StartAlliedRaceLevel] < 1)
		{
			Log.Logger.Error($"StartAlliedRaceLevel ({Values[WorldCfg.StartAlliedRaceLevel]}) must be in range 1..MaxPlayerLevel({Values[WorldCfg.MaxPlayerLevel]}). Set to 1.");
			Values[WorldCfg.StartAlliedRaceLevel] = 1;
		}
		else if ((int)Values[WorldCfg.StartAlliedRaceLevel] > (int)Values[WorldCfg.MaxPlayerLevel])
		{
			Log.Logger.Error($"StartAlliedRaceLevel ({Values[WorldCfg.StartAlliedRaceLevel]}) must be in range 1..MaxPlayerLevel({Values[WorldCfg.MaxPlayerLevel]}). Set to {Values[WorldCfg.MaxPlayerLevel]}.");
			Values[WorldCfg.StartAlliedRaceLevel] = Values[WorldCfg.MaxPlayerLevel];
		}

		Values[WorldCfg.StartPlayerMoney] = _configuration.GetDefaultValue("StartPlayerMoney", 0);

		if ((int)Values[WorldCfg.StartPlayerMoney] < 0)
		{
			Log.Logger.Error("StartPlayerMoney ({0}) must be in range 0..{1}. Set to {2}.", Values[WorldCfg.StartPlayerMoney], PlayerConst.MaxMoneyAmount, 0);
			Values[WorldCfg.StartPlayerMoney] = 0;
		}
		else if ((int)Values[WorldCfg.StartPlayerMoney] > 0x7FFFFFFF - 1) // TODO: (See MaxMoneyAMOUNT)
		{
			Log.Logger.Error(
						"StartPlayerMoney ({0}) must be in range 0..{1}. Set to {2}.",
						Values[WorldCfg.StartPlayerMoney],
						0x7FFFFFFF - 1,
						0x7FFFFFFF - 1);

			Values[WorldCfg.StartPlayerMoney] = 0x7FFFFFFF - 1;
		}

		Values[WorldCfg.CurrencyResetHour] = _configuration.GetDefaultValue("Currency.ResetHour", 3);

		if ((int)Values[WorldCfg.CurrencyResetHour] > 23)
			Log.Logger.Error("StartPlayerMoney ({0}) must be in range 0..{1}. Set to {2}.", Values[WorldCfg.CurrencyResetHour] = 3);

		Values[WorldCfg.CurrencyResetDay] = _configuration.GetDefaultValue("Currency.ResetDay", 3);

		if ((int)Values[WorldCfg.CurrencyResetDay] > 6)
		{
			Log.Logger.Error("Currency.ResetDay ({0}) can't be load. Set to 3.", Values[WorldCfg.CurrencyResetDay]);
			Values[WorldCfg.CurrencyResetDay] = 3;
		}

		Values[WorldCfg.CurrencyResetInterval] = _configuration.GetDefaultValue("Currency.ResetInterval", 7);

		if ((int)Values[WorldCfg.CurrencyResetInterval] <= 0)
		{
			Log.Logger.Error("Currency.ResetInterval ({0}) must be > 0, set to default 7.", Values[WorldCfg.CurrencyResetInterval]);
			Values[WorldCfg.CurrencyResetInterval] = 7;
		}

		Values[WorldCfg.MaxRecruitAFriendBonusPlayerLevel] = _configuration.GetDefaultValue("RecruitAFriend.MaxLevel", 60);

		if ((int)Values[WorldCfg.MaxRecruitAFriendBonusPlayerLevel] > (int)Values[WorldCfg.MaxPlayerLevel])
		{
			Log.Logger.Error(
						"RecruitAFriend.MaxLevel ({0}) must be in the range 0..MaxLevel({1}). Set to {2}.",
						Values[WorldCfg.MaxRecruitAFriendBonusPlayerLevel],
						Values[WorldCfg.MaxPlayerLevel],
						60);

			Values[WorldCfg.MaxRecruitAFriendBonusPlayerLevel] = 60;
		}

		Values[WorldCfg.MaxRecruitAFriendBonusPlayerLevelDifference] = _configuration.GetDefaultValue("RecruitAFriend.MaxDifference", 4);
		Values[WorldCfg.AllTaxiPaths] = _configuration.GetDefaultValue("AllFlightPaths", false);
		Values[WorldCfg.InstantTaxi] = _configuration.GetDefaultValue("InstantFlightPaths", false);

		Values[WorldCfg.InstanceIgnoreLevel] = _configuration.GetDefaultValue("Instance.IgnoreLevel", false);
		Values[WorldCfg.InstanceIgnoreRaid] = _configuration.GetDefaultValue("Instance.IgnoreRaid", false);

		Values[WorldCfg.CastUnstuck] = _configuration.GetDefaultValue("CastUnstuck", true);
		Values[WorldCfg.ResetScheduleWeekDay] = _configuration.GetDefaultValue("ResetSchedule.WeekDay", 2);
		Values[WorldCfg.ResetScheduleHour] = _configuration.GetDefaultValue("ResetSchedule.Hour", 8);
		Values[WorldCfg.InstanceUnloadDelay] = _configuration.GetDefaultValue("Instance.UnloadDelay", 30 * Time.Minute * Time.InMilliseconds);
		Values[WorldCfg.DailyQuestResetTimeHour] = _configuration.GetDefaultValue("Quests.DailyResetTime", 3);

		if ((int)Values[WorldCfg.DailyQuestResetTimeHour] > 23)
		{
			Log.Logger.Error($"Quests.DailyResetTime ({Values[WorldCfg.DailyQuestResetTimeHour]}) must be in range 0..23. Set to 3.");
			Values[WorldCfg.DailyQuestResetTimeHour] = 3;
		}

		Values[WorldCfg.WeeklyQuestResetTimeWDay] = _configuration.GetDefaultValue("Quests.WeeklyResetWDay", 3);

		if ((int)Values[WorldCfg.WeeklyQuestResetTimeWDay] > 6)
		{
			Log.Logger.Error($"Quests.WeeklyResetDay ({Values[WorldCfg.WeeklyQuestResetTimeWDay]}) must be in range 0..6. Set to 3 (Wednesday).");
			Values[WorldCfg.WeeklyQuestResetTimeWDay] = 3;
		}

		Values[WorldCfg.MaxPrimaryTradeSkill] = _configuration.GetDefaultValue("MaxPrimaryTradeSkill", 2);
		Values[WorldCfg.MinPetitionSigns] = _configuration.GetDefaultValue("MinPetitionSigns", 4);

		if ((int)Values[WorldCfg.MinPetitionSigns] > 4)
		{
			Log.Logger.Error("MinPetitionSigns ({0}) must be in range 0..4. Set to 4.", Values[WorldCfg.MinPetitionSigns]);
			Values[WorldCfg.MinPetitionSigns] = 4;
		}

		Values[WorldCfg.GmLoginState] = _configuration.GetDefaultValue("GM.LoginState", 2);
		Values[WorldCfg.GmVisibleState] = _configuration.GetDefaultValue("GM.Visible", 2);
		Values[WorldCfg.GmChat] = _configuration.GetDefaultValue("GM.Chat", 2);
		Values[WorldCfg.GmWhisperingTo] = _configuration.GetDefaultValue("GM.WhisperingTo", 2);
		Values[WorldCfg.GmFreezeDuration] = _configuration.GetDefaultValue("GM.FreezeAuraDuration", 0);

		Values[WorldCfg.GmLevelInGmList] = _configuration.GetDefaultValue("GM.InGMList.Level", (int)AccountTypes.Administrator);
		Values[WorldCfg.GmLevelInWhoList] = _configuration.GetDefaultValue("GM.InWhoList.Level", (int)AccountTypes.Administrator);
		Values[WorldCfg.StartGmLevel] = _configuration.GetDefaultValue("GM.StartLevel", 1);

		if ((int)Values[WorldCfg.StartGmLevel] < (int)Values[WorldCfg.StartPlayerLevel])
		{
			Log.Logger.Error(
						"GM.StartLevel ({0}) must be in range StartPlayerLevel({1})..{2}. Set to {3}.",
						Values[WorldCfg.StartGmLevel],
						Values[WorldCfg.StartPlayerLevel],
						SharedConst.MaxLevel,
						Values[WorldCfg.StartPlayerLevel]);

			Values[WorldCfg.StartGmLevel] = Values[WorldCfg.StartPlayerLevel];
		}
		else if ((int)Values[WorldCfg.StartGmLevel] > SharedConst.MaxLevel)
		{
			Log.Logger.Error("GM.StartLevel ({0}) must be in range 1..{1}. Set to {1}.", Values[WorldCfg.StartGmLevel], SharedConst.MaxLevel);
			Values[WorldCfg.StartGmLevel] = SharedConst.MaxLevel;
		}

		Values[WorldCfg.AllowGmGroup] = _configuration.GetDefaultValue("GM.AllowInvite", false);
		Values[WorldCfg.GmLowerSecurity] = _configuration.GetDefaultValue("GM.LowerSecurity", false);
		Values[WorldCfg.ForceShutdownThreshold] = _configuration.GetDefaultValue("GM.ForceShutdownThreshold", 30);

		Values[WorldCfg.GroupVisibility] = _configuration.GetDefaultValue("Visibility.GroupMode", 1);

		Values[WorldCfg.MailDeliveryDelay] = _configuration.GetDefaultValue("MailDeliveryDelay", Time.Hour);
		Values[WorldCfg.CleanOldMailTime] = _configuration.GetDefaultValue("CleanOldMailTime", 4);

		if ((int)Values[WorldCfg.CleanOldMailTime] > 23)
		{
			Log.Logger.Error($"CleanOldMailTime ({Values[WorldCfg.CleanOldMailTime]}) must be an hour, between 0 and 23. Set to 4.");
			Values[WorldCfg.CleanOldMailTime] = 4;
		}

		Values[WorldCfg.UptimeUpdate] = _configuration.GetDefaultValue("UpdateUptimeInterval", 10);

		if ((int)Values[WorldCfg.UptimeUpdate] <= 0)
		{
			Log.Logger.Error("UpdateUptimeInterval ({0}) must be > 0, set to default 10.", Values[WorldCfg.UptimeUpdate]);
			Values[WorldCfg.UptimeUpdate] = 10;
		}

		// log db cleanup interval
		Values[WorldCfg.LogdbClearinterval] = _configuration.GetDefaultValue("LogDB.Opt.ClearInterval", 10);

		if ((int)Values[WorldCfg.LogdbClearinterval] <= 0)
		{
			Log.Logger.Error("LogDB.Opt.ClearInterval ({0}) must be > 0, set to default 10.", Values[WorldCfg.LogdbClearinterval]);
			Values[WorldCfg.LogdbClearinterval] = 10;
		}

		Values[WorldCfg.LogdbCleartime] = _configuration.GetDefaultValue("LogDB.Opt.ClearTime", 1209600); // 14 days default
		Log.Logger.Information("Will clear `logs` table of entries older than {0} seconds every {1} minutes.", Values[WorldCfg.LogdbCleartime], Values[WorldCfg.LogdbClearinterval]);

		Values[WorldCfg.SkillChanceOrange] = _configuration.GetDefaultValue("SkillChance.Orange", 100);
		Values[WorldCfg.SkillChanceYellow] = _configuration.GetDefaultValue("SkillChance.Yellow", 75);
		Values[WorldCfg.SkillChanceGreen] = _configuration.GetDefaultValue("SkillChance.Green", 25);
		Values[WorldCfg.SkillChanceGrey] = _configuration.GetDefaultValue("SkillChance.Grey", 0);

		Values[WorldCfg.SkillChanceMiningSteps] = _configuration.GetDefaultValue("SkillChance.MiningSteps", 75);
		Values[WorldCfg.SkillChanceSkinningSteps] = _configuration.GetDefaultValue("SkillChance.SkinningSteps", 75);

		Values[WorldCfg.SkillProspecting] = _configuration.GetDefaultValue("SkillChance.Prospecting", false);
		Values[WorldCfg.SkillMilling] = _configuration.GetDefaultValue("SkillChance.Milling", false);

		Values[WorldCfg.SkillGainCrafting] = _configuration.GetDefaultValue("SkillGain.Crafting", 1);

		Values[WorldCfg.SkillGainGathering] = _configuration.GetDefaultValue("SkillGain.Gathering", 1);

		Values[WorldCfg.MaxOverspeedPings] = _configuration.GetDefaultValue("MaxOverspeedPings", 2);

		if ((int)Values[WorldCfg.MaxOverspeedPings] != 0 && (int)Values[WorldCfg.MaxOverspeedPings] < 2)
		{
			Log.Logger.Error("MaxOverspeedPings ({0}) must be in range 2..infinity (or 0 to disable check). Set to 2.", Values[WorldCfg.MaxOverspeedPings]);
			Values[WorldCfg.MaxOverspeedPings] = 2;
		}

		Values[WorldCfg.Weather] = _configuration.GetDefaultValue("ActivateWeather", true);

		Values[WorldCfg.DisableBreathing] = _configuration.GetDefaultValue("DisableWaterBreath", (int)AccountTypes.Console);

		if (reload)
		{
			var val = _configuration.GetDefaultValue("Expansion", (int)Expansion.Dragonflight);

			if (val != (int)Values[WorldCfg.Expansion])
				Log.Logger.Error("Expansion option can't be changed at worldserver.conf reload, using current value ({0}).", Values[WorldCfg.Expansion]);
		}
		else
		{
			Values[WorldCfg.Expansion] = _configuration.GetDefaultValue("Expansion", Expansion.Dragonflight);
		}

		Values[WorldCfg.ChatFloodMessageCount] = _configuration.GetDefaultValue("ChatFlood.MessageCount", 10);
		Values[WorldCfg.ChatFloodMessageDelay] = _configuration.GetDefaultValue("ChatFlood.MessageDelay", 1);
		Values[WorldCfg.ChatFloodMuteTime] = _configuration.GetDefaultValue("ChatFlood.MuteTime", 10);

		Values[WorldCfg.EventAnnounce] = _configuration.GetDefaultValue("Event.Announce", false);

		Values[WorldCfg.CreatureFamilyFleeAssistanceRadius] = _configuration.GetDefaultValue("CreatureFamilyFleeAssistanceRadius", 30.0f);
		Values[WorldCfg.CreatureFamilyAssistanceRadius] = _configuration.GetDefaultValue("CreatureFamilyAssistanceRadius", 10.0f);
		Values[WorldCfg.CreatureFamilyAssistanceDelay] = _configuration.GetDefaultValue("CreatureFamilyAssistanceDelay", 1500);
		Values[WorldCfg.CreatureFamilyFleeDelay] = _configuration.GetDefaultValue("CreatureFamilyFleeDelay", 7000);

		Values[WorldCfg.WorldBossLevelDiff] = _configuration.GetDefaultValue("WorldBossLevelDiff", 3);

		Values[WorldCfg.QuestEnableQuestTracker] = _configuration.GetDefaultValue("Quests.EnableQuestTracker", false);

		// note: disable value (-1) will assigned as 0xFFFFFFF, to prevent overflow at calculations limit it to max possible player Level MaxLevel(100)
		Values[WorldCfg.QuestLowLevelHideDiff] = _configuration.GetDefaultValue("Quests.LowLevelHideDiff", 4);

		if ((int)Values[WorldCfg.QuestLowLevelHideDiff] > SharedConst.MaxLevel)
			Values[WorldCfg.QuestLowLevelHideDiff] = SharedConst.MaxLevel;

		Values[WorldCfg.QuestHighLevelHideDiff] = _configuration.GetDefaultValue("Quests.HighLevelHideDiff", 7);

		if ((int)Values[WorldCfg.QuestHighLevelHideDiff] > SharedConst.MaxLevel)
			Values[WorldCfg.QuestHighLevelHideDiff] = SharedConst.MaxLevel;

		Values[WorldCfg.QuestIgnoreRaid] = _configuration.GetDefaultValue("Quests.IgnoreRaid", false);
		Values[WorldCfg.QuestIgnoreAutoAccept] = _configuration.GetDefaultValue("Quests.IgnoreAutoAccept", false);
		Values[WorldCfg.QuestIgnoreAutoComplete] = _configuration.GetDefaultValue("Quests.IgnoreAutoComplete", false);

		Values[WorldCfg.RandomBgResetHour] = _configuration.GetDefaultValue("Battleground.Random.ResetHour", 6);

		if ((int)Values[WorldCfg.RandomBgResetHour] > 23)
		{
			Log.Logger.Error("Battleground.Random.ResetHour ({0}) can't be load. Set to 6.", Values[WorldCfg.RandomBgResetHour]);
			Values[WorldCfg.RandomBgResetHour] = 6;
		}

		Values[WorldCfg.CalendarDeleteOldEventsHour] = _configuration.GetDefaultValue("Calendar.DeleteOldEventsHour", 6);

		if ((int)Values[WorldCfg.CalendarDeleteOldEventsHour] > 23)
		{
			Log.Logger.Error($"Calendar.DeleteOldEventsHour ({Values[WorldCfg.CalendarDeleteOldEventsHour]}) can't be load. Set to 6.");
			Values[WorldCfg.CalendarDeleteOldEventsHour] = 6;
		}

		Values[WorldCfg.GuildResetHour] = _configuration.GetDefaultValue("Guild.ResetHour", 6);

		if ((int)Values[WorldCfg.GuildResetHour] > 23)
		{
			Log.Logger.Error("Guild.ResetHour ({0}) can't be load. Set to 6.", Values[WorldCfg.GuildResetHour]);
			Values[WorldCfg.GuildResetHour] = 6;
		}

		Values[WorldCfg.DetectPosCollision] = _configuration.GetDefaultValue("DetectPosCollision", true);

		Values[WorldCfg.RestrictedLfgChannel] = _configuration.GetDefaultValue("Channel.RestrictedLfg", true);
		Values[WorldCfg.TalentsInspecting] = _configuration.GetDefaultValue("TalentsInspecting", 1);
		Values[WorldCfg.ChatFakeMessagePreventing] = _configuration.GetDefaultValue("ChatFakeMessagePreventing", false);
		Values[WorldCfg.ChatStrictLinkCheckingSeverity] = _configuration.GetDefaultValue("ChatStrictLinkChecking.Severity", 0);
		Values[WorldCfg.ChatStrictLinkCheckingKick] = _configuration.GetDefaultValue("ChatStrictLinkChecking.Kick", 0);

		Values[WorldCfg.CorpseDecayNormal] = _configuration.GetDefaultValue("Corpse.Decay.NORMAL", 60);
		Values[WorldCfg.CorpseDecayRare] = _configuration.GetDefaultValue("Corpse.Decay.RARE", 300);
		Values[WorldCfg.CorpseDecayElite] = _configuration.GetDefaultValue("Corpse.Decay.ELITE", 300);
		Values[WorldCfg.CorpseDecayRareelite] = _configuration.GetDefaultValue("Corpse.Decay.RAREELITE", 300);
		Values[WorldCfg.CorpseDecayWorldboss] = _configuration.GetDefaultValue("Corpse.Decay.WORLDBOSS", 3600);

		Values[WorldCfg.DeathSicknessLevel] = _configuration.GetDefaultValue("Death.SicknessLevel", 11);
		Values[WorldCfg.DeathCorpseReclaimDelayPvp] = _configuration.GetDefaultValue("Death.CorpseReclaimDelay.PvP", true);
		Values[WorldCfg.DeathCorpseReclaimDelayPve] = _configuration.GetDefaultValue("Death.CorpseReclaimDelay.PvE", true);
		Values[WorldCfg.DeathBonesWorld] = _configuration.GetDefaultValue("Death.Bones.World", true);
		Values[WorldCfg.DeathBonesBgOrArena] = _configuration.GetDefaultValue("Death.Bones.BattlegroundOrArena", true);

		Values[WorldCfg.DieCommandMode] = _configuration.GetDefaultValue("Die.Command.Mode", true);

		Values[WorldCfg.ThreatRadius] = _configuration.GetDefaultValue("ThreatRadius", 60.0f);

		// always use declined names in the russian client
		Values[WorldCfg.DeclinedNamesUsed] = (RealmZones)Values[WorldCfg.RealmZone] == RealmZones.Russian || _configuration.GetDefaultValue("DeclinedNames", false);

		Values[WorldCfg.ListenRangeSay] = _configuration.GetDefaultValue("ListenRange.Say", 25.0f);
		Values[WorldCfg.ListenRangeTextemote] = _configuration.GetDefaultValue("ListenRange.TextEmote", 25.0f);
		Values[WorldCfg.ListenRangeYell] = _configuration.GetDefaultValue("ListenRange.Yell", 300.0f);

		Values[WorldCfg.BattlegroundCastDeserter] = _configuration.GetDefaultValue("Battleground.CastDeserter", true);
		Values[WorldCfg.BattlegroundQueueAnnouncerEnable] = _configuration.GetDefaultValue("Battleground.QueueAnnouncer.Enable", false);
		Values[WorldCfg.BattlegroundQueueAnnouncerPlayeronly] = _configuration.GetDefaultValue("Battleground.QueueAnnouncer.PlayerOnly", false);
		Values[WorldCfg.BattlegroundStoreStatisticsEnable] = _configuration.GetDefaultValue("Battleground.StoreStatistics.Enable", false);
		Values[WorldCfg.BattlegroundReportAfk] = _configuration.GetDefaultValue("Battleground.ReportAFK", 3);

		if ((int)Values[WorldCfg.BattlegroundReportAfk] < 1)
		{
			Log.Logger.Error("Battleground.ReportAFK ({0}) must be >0. Using 3 instead.", Values[WorldCfg.BattlegroundReportAfk]);
			Values[WorldCfg.BattlegroundReportAfk] = 3;
		}

		if ((int)Values[WorldCfg.BattlegroundReportAfk] > 9)
		{
			Log.Logger.Error("Battleground.ReportAFK ({0}) must be <10. Using 3 instead.", Values[WorldCfg.BattlegroundReportAfk]);
			Values[WorldCfg.BattlegroundReportAfk] = 3;
		}

		Values[WorldCfg.BattlegroundInvitationType] = _configuration.GetDefaultValue("Battleground.InvitationType", 0);
		Values[WorldCfg.BattlegroundPrematureFinishTimer] = _configuration.GetDefaultValue("Battleground.PrematureFinishTimer", 5 * Time.Minute * Time.InMilliseconds);
		Values[WorldCfg.BattlegroundPremadeGroupWaitForMatch] = _configuration.GetDefaultValue("Battleground.PremadeGroupWaitForMatch", 30 * Time.Minute * Time.InMilliseconds);
		Values[WorldCfg.BgXpForKill] = _configuration.GetDefaultValue("Battleground.GiveXPForKills", false);
		Values[WorldCfg.ArenaMaxRatingDifference] = _configuration.GetDefaultValue("Arena.MaxRatingDifference", 150);
		Values[WorldCfg.ArenaRatingDiscardTimer] = _configuration.GetDefaultValue("Arena.RatingDiscardTimer", 10 * Time.Minute * Time.InMilliseconds);
		Values[WorldCfg.ArenaRatedUpdateTimer] = _configuration.GetDefaultValue("Arena.RatedUpdateTimer", 5 * Time.InMilliseconds);
		Values[WorldCfg.ArenaQueueAnnouncerEnable] = _configuration.GetDefaultValue("Arena.QueueAnnouncer.Enable", false);
		Values[WorldCfg.ArenaSeasonId] = _configuration.GetDefaultValue("Arena.ArenaSeason.ID", 32);
		Values[WorldCfg.ArenaStartRating] = _configuration.GetDefaultValue("Arena.ArenaStartRating", 0);
		Values[WorldCfg.ArenaStartPersonalRating] = _configuration.GetDefaultValue("Arena.ArenaStartPersonalRating", 1000);
		Values[WorldCfg.ArenaStartMatchmakerRating] = _configuration.GetDefaultValue("Arena.ArenaStartMatchmakerRating", 1500);
		Values[WorldCfg.ArenaSeasonInProgress] = _configuration.GetDefaultValue("Arena.ArenaSeason.InProgress", false);
		Values[WorldCfg.ArenaLogExtendedInfo] = _configuration.GetDefaultValue("ArenaLog.ExtendedInfo", false);
		Values[WorldCfg.ArenaWinRatingModifier1] = _configuration.GetDefaultValue("Arena.ArenaWinRatingModifier1", 48.0f);
		Values[WorldCfg.ArenaWinRatingModifier2] = _configuration.GetDefaultValue("Arena.ArenaWinRatingModifier2", 24.0f);
		Values[WorldCfg.ArenaLoseRatingModifier] = _configuration.GetDefaultValue("Arena.ArenaLoseRatingModifier", 24.0f);
		Values[WorldCfg.ArenaMatchmakerRatingModifier] = _configuration.GetDefaultValue("Arena.ArenaMatchmakerRatingModifier", 24.0f);

		Values[WorldCfg.OffhandCheckAtSpellUnlearn] = _configuration.GetDefaultValue("OffhandCheckAtSpellUnlearn", true);

		Values[WorldCfg.CreaturePickpocketRefill] = _configuration.GetDefaultValue("Creature.PickPocketRefillDelay", 10 * Time.Minute);
		Values[WorldCfg.CreatureStopForPlayer] = _configuration.GetDefaultValue("Creature.MovingStopTimeForPlayer", 3 * Time.Minute * Time.InMilliseconds);

		var clientCacheId = _configuration.GetDefaultValue("ClientCacheVersion", 0);

		if (clientCacheId != 0)
		{
			// overwrite DB/old value
			if (clientCacheId > 0)
				Values[WorldCfg.ClientCacheVersion] = clientCacheId;
			else
				Log.Logger.Error("ClientCacheVersion can't be negative {0}, ignored.", clientCacheId);
		}

		Log.Logger.Information("Client cache version set to: {0}", clientCacheId);

		Values[WorldCfg.GuildNewsLogCount] = _configuration.GetDefaultValue("Guild.NewsLogRecordsCount", GuildConst.NewsLogMaxRecords);

		if ((int)Values[WorldCfg.GuildNewsLogCount] > GuildConst.NewsLogMaxRecords)
			Values[WorldCfg.GuildNewsLogCount] = GuildConst.NewsLogMaxRecords;

		Values[WorldCfg.GuildEventLogCount] = _configuration.GetDefaultValue("Guild.EventLogRecordsCount", GuildConst.EventLogMaxRecords);

		if ((int)Values[WorldCfg.GuildEventLogCount] > GuildConst.EventLogMaxRecords)
			Values[WorldCfg.GuildEventLogCount] = GuildConst.EventLogMaxRecords;

		Values[WorldCfg.GuildBankEventLogCount] = _configuration.GetDefaultValue("Guild.BankEventLogRecordsCount", GuildConst.BankLogMaxRecords);

		if ((int)Values[WorldCfg.GuildBankEventLogCount] > GuildConst.BankLogMaxRecords)
			Values[WorldCfg.GuildBankEventLogCount] = GuildConst.BankLogMaxRecords;

		// Load the CharDelete related config options
		Values[WorldCfg.ChardeleteMethod] = _configuration.GetDefaultValue("CharDelete.Method", 0);
		Values[WorldCfg.ChardeleteMinLevel] = _configuration.GetDefaultValue("CharDelete.MinLevel", 0);
		Values[WorldCfg.ChardeleteDeathKnightMinLevel] = _configuration.GetDefaultValue("CharDelete.DeathKnight.MinLevel", 0);
		Values[WorldCfg.ChardeleteDemonHunterMinLevel] = _configuration.GetDefaultValue("CharDelete.DemonHunter.MinLevel", 0);
		Values[WorldCfg.ChardeleteKeepDays] = _configuration.GetDefaultValue("CharDelete.KeepDays", 30);

		// No aggro from gray mobs
		Values[WorldCfg.NoGrayAggroAbove] = _configuration.GetDefaultValue("NoGrayAggro.Above", 0);
		Values[WorldCfg.NoGrayAggroBelow] = _configuration.GetDefaultValue("NoGrayAggro.Below", 0);

		if ((int)Values[WorldCfg.NoGrayAggroAbove] > (int)Values[WorldCfg.MaxPlayerLevel])
		{
			Log.Logger.Error("NoGrayAggro.Above ({0}) must be in range 0..{1}. Set to {1}.", Values[WorldCfg.NoGrayAggroAbove], Values[WorldCfg.MaxPlayerLevel]);
			Values[WorldCfg.NoGrayAggroAbove] = Values[WorldCfg.MaxPlayerLevel];
		}

		if ((int)Values[WorldCfg.NoGrayAggroBelow] > (int)Values[WorldCfg.MaxPlayerLevel])
		{
			Log.Logger.Error("NoGrayAggro.Below ({0}) must be in range 0..{1}. Set to {1}.", Values[WorldCfg.NoGrayAggroBelow], Values[WorldCfg.MaxPlayerLevel]);
			Values[WorldCfg.NoGrayAggroBelow] = Values[WorldCfg.MaxPlayerLevel];
		}

		if ((int)Values[WorldCfg.NoGrayAggroAbove] > 0 && (int)Values[WorldCfg.NoGrayAggroAbove] < (int)Values[WorldCfg.NoGrayAggroBelow])
		{
			Log.Logger.Error("NoGrayAggro.Below ({0}) cannot be greater than NoGrayAggro.Above ({1}). Set to {1}.", Values[WorldCfg.NoGrayAggroBelow], Values[WorldCfg.NoGrayAggroAbove]);
			Values[WorldCfg.NoGrayAggroBelow] = Values[WorldCfg.NoGrayAggroAbove];
		}

		// Respawn Settings
		Values[WorldCfg.RespawnMinCheckIntervalMs] = _configuration.GetDefaultValue("Respawn.MinCheckIntervalMS", 5000);
		Values[WorldCfg.RespawnDynamicMode] = _configuration.GetDefaultValue("Respawn.DynamicMode", 0);

		if ((int)Values[WorldCfg.RespawnDynamicMode] > 1)
		{
			Log.Logger.Error($"Invalid value for Respawn.DynamicMode ({Values[WorldCfg.RespawnDynamicMode]}). Set to 0.");
			Values[WorldCfg.RespawnDynamicMode] = 0;
		}

		Values[WorldCfg.RespawnDynamicEscortNpc] = _configuration.GetDefaultValue("Respawn.DynamicEscortNPC", false);
		Values[WorldCfg.RespawnGuidWarnLevel] = _configuration.GetDefaultValue("Respawn.GuidWarnLevel", 12000000);

		if ((int)Values[WorldCfg.RespawnGuidWarnLevel] > 16777215)
		{
			Log.Logger.Error($"Respawn.GuidWarnLevel ({Values[WorldCfg.RespawnGuidWarnLevel]}) cannot be greater than maximum GUID (16777215). Set to 12000000.");
			Values[WorldCfg.RespawnGuidWarnLevel] = 12000000;
		}

		Values[WorldCfg.RespawnGuidAlertLevel] = _configuration.GetDefaultValue("Respawn.GuidAlertLevel", 16000000);

		if ((int)Values[WorldCfg.RespawnGuidAlertLevel] > 16777215)
		{
			Log.Logger.Error($"Respawn.GuidWarnLevel ({Values[WorldCfg.RespawnGuidAlertLevel]}) cannot be greater than maximum GUID (16777215). Set to 16000000.");
			Values[WorldCfg.RespawnGuidAlertLevel] = 16000000;
		}

		Values[WorldCfg.RespawnRestartQuietTime] = _configuration.GetDefaultValue("Respawn.RestartQuietTime", 3);

		if ((int)Values[WorldCfg.RespawnRestartQuietTime] > 23)
		{
			Log.Logger.Error($"Respawn.RestartQuietTime ({Values[WorldCfg.RespawnRestartQuietTime]}) must be an hour, between 0 and 23. Set to 3.");
			Values[WorldCfg.RespawnRestartQuietTime] = 3;
		}

		Values[WorldCfg.RespawnDynamicRateCreature] = _configuration.GetDefaultValue("Respawn.DynamicRateCreature", 10.0f);

		if ((float)Values[WorldCfg.RespawnDynamicRateCreature] < 0.0f)
		{
			Log.Logger.Error($"Respawn.DynamicRateCreature ({Values[WorldCfg.RespawnDynamicRateCreature]}) must be positive. Set to 10.");
			Values[WorldCfg.RespawnDynamicRateCreature] = 10.0f;
		}

		Values[WorldCfg.RespawnDynamicMinimumCreature] = _configuration.GetDefaultValue("Respawn.DynamicMinimumCreature", 10);
		Values[WorldCfg.RespawnDynamicRateGameobject] = _configuration.GetDefaultValue("Respawn.DynamicRateGameObject", 10.0f);

		if ((float)Values[WorldCfg.RespawnDynamicRateGameobject] < 0.0f)
		{
			Log.Logger.Error($"Respawn.DynamicRateGameObject ({Values[WorldCfg.RespawnDynamicRateGameobject]}) must be positive. Set to 10.");
			Values[WorldCfg.RespawnDynamicRateGameobject] = 10.0f;
		}

		Values[WorldCfg.RespawnDynamicMinimumGameObject] = _configuration.GetDefaultValue("Respawn.DynamicMinimumGameObject", 10);
		Values[WorldCfg.RespawnGuidWarningFrequency] = _configuration.GetDefaultValue("Respawn.WarningFrequency", 1800);

		Values[WorldCfg.EnableMmaps] = _configuration.GetDefaultValue("mmap.EnablePathFinding", true);
		Values[WorldCfg.VmapIndoorCheck] = _configuration.GetDefaultValue("vmap.EnableIndoorCheck", false);

		Values[WorldCfg.MaxWho] = _configuration.GetDefaultValue("MaxWhoListReturns", 49);
		Values[WorldCfg.StartAllSpells] = _configuration.GetDefaultValue("PlayerStart.AllSpells", false);

		if ((bool)Values[WorldCfg.StartAllSpells])
			Log.Logger.Warning("PlayerStart.AllSpells Enabled - may not function as intended!");

		Values[WorldCfg.HonorAfterDuel] = _configuration.GetDefaultValue("HonorPointsAfterDuel", 0);
		Values[WorldCfg.ResetDuelCooldowns] = _configuration.GetDefaultValue("ResetDuelCooldowns", false);
		Values[WorldCfg.ResetDuelHealthMana] = _configuration.GetDefaultValue("ResetDuelHealthMana", false);
		Values[WorldCfg.StartAllExplored] = _configuration.GetDefaultValue("PlayerStart.MapsExplored", false);
		Values[WorldCfg.StartAllRep] = _configuration.GetDefaultValue("PlayerStart.AllReputation", false);
		Values[WorldCfg.PvpTokenEnable] = _configuration.GetDefaultValue("PvPToken.Enable", false);
		Values[WorldCfg.PvpTokenMapType] = _configuration.GetDefaultValue("PvPToken.MapAllowType", 4);
		Values[WorldCfg.PvpTokenId] = _configuration.GetDefaultValue("PvPToken.ItemID", 29434);
		Values[WorldCfg.PvpTokenCount] = _configuration.GetDefaultValue("PvPToken.ItemCount", 1);

		if ((int)Values[WorldCfg.PvpTokenCount] < 1)
			Values[WorldCfg.PvpTokenCount] = 1;

		Values[WorldCfg.NoResetTalentCost] = _configuration.GetDefaultValue("NoResetTalentsCost", false);
		Values[WorldCfg.ShowKickInWorld] = _configuration.GetDefaultValue("ShowKickInWorld", false);
		Values[WorldCfg.ShowMuteInWorld] = _configuration.GetDefaultValue("ShowMuteInWorld", false);
		Values[WorldCfg.ShowBanInWorld] = _configuration.GetDefaultValue("ShowBanInWorld", false);
		Values[WorldCfg.Numthreads] = _configuration.GetDefaultValue("MapUpdate.Threads", 10);
		Values[WorldCfg.MaxResultsLookupCommands] = _configuration.GetDefaultValue("Command.LookupMaxResults", 0);

		// Warden
		Values[WorldCfg.WardenEnabled] = _configuration.GetDefaultValue("Warden.Enabled", false);
		Values[WorldCfg.WardenNumInjectChecks] = _configuration.GetDefaultValue("Warden.NumInjectionChecks", 9);
		Values[WorldCfg.WardenNumLuaChecks] = _configuration.GetDefaultValue("Warden.NumLuaSandboxChecks", 1);
		Values[WorldCfg.WardenNumClientModChecks] = _configuration.GetDefaultValue("Warden.NumClientModChecks", 1);
		Values[WorldCfg.WardenClientBanDuration] = _configuration.GetDefaultValue("Warden.BanDuration", 86400);
		Values[WorldCfg.WardenClientCheckHoldoff] = _configuration.GetDefaultValue("Warden.ClientCheckHoldOff", 30);
		Values[WorldCfg.WardenClientFailAction] = _configuration.GetDefaultValue("Warden.ClientCheckFailAction", 0);
		Values[WorldCfg.WardenClientResponseDelay] = _configuration.GetDefaultValue("Warden.ClientResponseDelay", 600);

		// Feature System
		Values[WorldCfg.FeatureSystemBpayStoreEnabled] = _configuration.GetDefaultValue("FeatureSystem.BpayStore.Enabled", false);
		Values[WorldCfg.FeatureSystemCharacterUndeleteEnabled] = _configuration.GetDefaultValue("FeatureSystem.CharacterUndelete.Enabled", false);
		Values[WorldCfg.FeatureSystemCharacterUndeleteCooldown] = _configuration.GetDefaultValue("FeatureSystem.CharacterUndelete.Cooldown", 2592000);
		Values[WorldCfg.FeatureSystemWarModeEnabled] = _configuration.GetDefaultValue("FeatureSystem.WarMode.Enabled", false);

		// Dungeon finder
		Values[WorldCfg.LfgOptionsmask] = _configuration.GetDefaultValue("DungeonFinder.OptionsMask", 1);

		// DBC_ItemAttributes
		Values[WorldCfg.DbcEnforceItemAttributes] = _configuration.GetDefaultValue("DBC.EnforceItemAttributes", true);

		// Accountpassword Secruity
		Values[WorldCfg.AccPasschangesec] = _configuration.GetDefaultValue("Account.PasswordChangeSecurity", 0);

		// Random Battleground Rewards
		Values[WorldCfg.BgRewardWinnerHonorFirst] = _configuration.GetDefaultValue("Battleground.RewardWinnerHonorFirst", 27000);
		Values[WorldCfg.BgRewardWinnerConquestFirst] = _configuration.GetDefaultValue("Battleground.RewardWinnerConquestFirst", 10000);
		Values[WorldCfg.BgRewardWinnerHonorLast] = _configuration.GetDefaultValue("Battleground.RewardWinnerHonorLast", 13500);
		Values[WorldCfg.BgRewardWinnerConquestLast] = _configuration.GetDefaultValue("Battleground.RewardWinnerConquestLast", 5000);
		Values[WorldCfg.BgRewardLoserHonorFirst] = _configuration.GetDefaultValue("Battleground.RewardLoserHonorFirst", 4500);
		Values[WorldCfg.BgRewardLoserHonorLast] = _configuration.GetDefaultValue("Battleground.RewardLoserHonorLast", 3500);

		// Max instances per hour
		Values[WorldCfg.MaxInstancesPerHour] = _configuration.GetDefaultValue("AccountInstancesPerHour", 5);

		// Anounce reset of instance to whole party
		Values[WorldCfg.InstancesResetAnnounce] = _configuration.GetDefaultValue("InstancesResetAnnounce", false);

		// Autobroadcast
		//AutoBroadcast.On
		Values[WorldCfg.AutoBroadcast] = _configuration.GetDefaultValue("AutoBroadcast.On", false);
		Values[WorldCfg.AutoBroadcastCenter] = _configuration.GetDefaultValue("AutoBroadcast.Center", 0);
		Values[WorldCfg.AutoBroadcastInterval] = _configuration.GetDefaultValue("AutoBroadcast.Timer", 60000);

		// Guild save interval
		Values[WorldCfg.GuildSaveInterval] = _configuration.GetDefaultValue("Guild.SaveInterval", 15);

		// misc
		Values[WorldCfg.PdumpNoPaths] = _configuration.GetDefaultValue("PlayerDump.DisallowPaths", true);
		Values[WorldCfg.PdumpNoOverwrite] = _configuration.GetDefaultValue("PlayerDump.DisallowOverwrite", true);

		// Wintergrasp battlefield
		Values[WorldCfg.WintergraspEnable] = _configuration.GetDefaultValue("Wintergrasp.Enable", false);
		Values[WorldCfg.WintergraspPlrMax] = _configuration.GetDefaultValue("Wintergrasp.PlayerMax", 100);
		Values[WorldCfg.WintergraspPlrMin] = _configuration.GetDefaultValue("Wintergrasp.PlayerMin", 0);
		Values[WorldCfg.WintergraspPlrMinLvl] = _configuration.GetDefaultValue("Wintergrasp.PlayerMinLvl", 77);
		Values[WorldCfg.WintergraspBattletime] = _configuration.GetDefaultValue("Wintergrasp.BattleTimer", 30);
		Values[WorldCfg.WintergraspNobattletime] = _configuration.GetDefaultValue("Wintergrasp.NoBattleTimer", 150);
		Values[WorldCfg.WintergraspRestartAfterCrash] = _configuration.GetDefaultValue("Wintergrasp.CrashRestartTimer", 10);

		// Tol Barad battlefield
		Values[WorldCfg.TolbaradEnable] = _configuration.GetDefaultValue("TolBarad.Enable", true);
		Values[WorldCfg.TolbaradPlrMax] = _configuration.GetDefaultValue("TolBarad.PlayerMax", 100);
		Values[WorldCfg.TolbaradPlrMin] = _configuration.GetDefaultValue("TolBarad.PlayerMin", 0);
		Values[WorldCfg.TolbaradPlrMinLvl] = _configuration.GetDefaultValue("TolBarad.PlayerMinLvl", 85);
		Values[WorldCfg.TolbaradBattleTime] = _configuration.GetDefaultValue("TolBarad.BattleTimer", 15);
		Values[WorldCfg.TolbaradBonusTime] = _configuration.GetDefaultValue("TolBarad.BonusTime", 5);
		Values[WorldCfg.TolbaradNoBattleTime] = _configuration.GetDefaultValue("TolBarad.NoBattleTimer", 150);
		Values[WorldCfg.TolbaradRestartAfterCrash] = _configuration.GetDefaultValue("TolBarad.CrashRestartTimer", 10);

		// Stats limits
		Values[WorldCfg.StatsLimitsEnable] = _configuration.GetDefaultValue("Stats.Limits.Enable", false);
		Values[WorldCfg.StatsLimitsDodge] = _configuration.GetDefaultValue("Stats.Limits.Dodge", 95.0f);
		Values[WorldCfg.StatsLimitsParry] = _configuration.GetDefaultValue("Stats.Limits.Parry", 95.0f);
		Values[WorldCfg.StatsLimitsBlock] = _configuration.GetDefaultValue("Stats.Limits.Block", 95.0f);
		Values[WorldCfg.StatsLimitsCrit] = _configuration.GetDefaultValue("Stats.Limits.Crit", 95.0f);

		//packet spoof punishment
		Values[WorldCfg.PacketSpoofPolicy] = _configuration.GetDefaultValue("PacketSpoof.Policy", 1); //Kick
		Values[WorldCfg.PacketSpoofBanmode] = _configuration.GetDefaultValue("PacketSpoof.BanMode", (int)BanMode.Account);

		if ((int)Values[WorldCfg.PacketSpoofBanmode] == 1 || (int)Values[WorldCfg.PacketSpoofBanmode] > 2)
			Values[WorldCfg.PacketSpoofBanmode] = (int)BanMode.Account;

		Values[WorldCfg.PacketSpoofBanduration] = _configuration.GetDefaultValue("PacketSpoof.BanDuration", 86400);

		Values[WorldCfg.IpBasedActionLogging] = _configuration.GetDefaultValue("Allow.IP.Based.Action.Logging", false);

		// AHBot
		Values[WorldCfg.AhbotUpdateInterval] = _configuration.GetDefaultValue("AuctionHouseBot.Update.Interval", 20);

		Values[WorldCfg.CalculateCreatureZoneAreaData] = _configuration.GetDefaultValue("Calculate.Creature.Zone.Area.Data", false);
		Values[WorldCfg.CalculateGameobjectZoneAreaData] = _configuration.GetDefaultValue("Calculate.Gameoject.Zone.Area.Data", false);

		// Black Market
		Values[WorldCfg.BlackmarketEnabled] = _configuration.GetDefaultValue("BlackMarket.Enabled", true);

		Values[WorldCfg.BlackmarketMaxAuctions] = _configuration.GetDefaultValue("BlackMarket.MaxAuctions", 12);
		Values[WorldCfg.BlackmarketUpdatePeriod] = _configuration.GetDefaultValue("BlackMarket.UpdatePeriod", 24);

		// prevent character rename on character customization
		Values[WorldCfg.PreventRenameCustomization] = _configuration.GetDefaultValue("PreventRenameCharacterOnCustomization", false);

		// Allow 5-man parties to use raid warnings
		Values[WorldCfg.ChatPartyRaidWarnings] = _configuration.GetDefaultValue("PartyRaidWarnings", false);

		// Allow to cache data queries
		Values[WorldCfg.CacheDataQueries] = _configuration.GetDefaultValue("CacheDataQueries", true);

		// Check Invalid Position
		Values[WorldCfg.CreatureCheckInvalidPostion] = _configuration.GetDefaultValue("Creature.CheckInvalidPosition", false);
		Values[WorldCfg.GameobjectCheckInvalidPostion] = _configuration.GetDefaultValue("GameObject.CheckInvalidPosition", false);

		// Whether to use LoS from game objects
		Values[WorldCfg.CheckGobjectLos] = _configuration.GetDefaultValue("CheckGameObjectLoS", true);

		// FactionBalance
		Values[WorldCfg.FactionBalanceLevelCheckDiff] = _configuration.GetDefaultValue("Pvp.FactionBalance.LevelCheckDiff", 0);
		Values[WorldCfg.CallToArms5Pct] = _configuration.GetDefaultValue("Pvp.FactionBalance.Pct5", 0.6f);
		Values[WorldCfg.CallToArms10Pct] = _configuration.GetDefaultValue("Pvp.FactionBalance.Pct10", 0.7f);
		Values[WorldCfg.CallToArms20Pct] = _configuration.GetDefaultValue("Pvp.FactionBalance.Pct20", 0.8f);

		// Specifies if IP addresses can be logged to the database
		Values[WorldCfg.AllowLogginIpAddressesInDatabase] = _configuration.GetDefaultValue("AllowLoggingIPAddressesInDatabase", true);

		// call ScriptMgr if we're reloading the configuration
		if (reload)
            _scriptManager.ForEach<IWorldOnConfigLoad>(p => p.OnConfigLoad(reload));
	}

	public uint GetUIntValue(WorldCfg confi)
	{
		return Convert.ToUInt32(Values.LookupByKey(confi));
	}

	public int GetIntValue(WorldCfg confi)
	{
		return Convert.ToInt32(Values.LookupByKey(confi));
	}

	public ulong GetUInt64Value(WorldCfg confi)
	{
		return Convert.ToUInt64(Values.LookupByKey(confi));
	}

	public bool GetBoolValue(WorldCfg confi)
	{
		return Convert.ToBoolean(Values.LookupByKey(confi));
	}

	public float GetFloatValue(WorldCfg confi)
	{
		return Convert.ToSingle(Values.LookupByKey(confi));
	}

	public void SetValue(WorldCfg confi, object value)
	{
		Values[confi] = value;
	}
}